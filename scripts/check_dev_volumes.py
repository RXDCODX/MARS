#!/usr/bin/env python3
"""Checks docker-compose.dev.yml for missing per-service obj/bin volume mounts.

Dev services bind-mount the repo at /src and overlay named volumes for project
obj/bin directories. If a project in a service's transitive dependency closure
is built without an isolated obj/bin mount, multiple containers share the same
bind-mount artifacts and dotnet watch/build race on them.

Additional hygiene checks (polling file watcher, SDK tag consistency,
.dockerignore, Properties overlay, top-level volume declarations) are reported
as warnings.

Usage:
  python3 scripts/check_dev_volumes.py --root .
  python3 scripts/check_dev_volumes.py --root . --compose docker-compose.dev.yml
"""

from __future__ import annotations

import argparse
import os
import posixpath
import re
import sys
import xml.etree.ElementTree as ET


def fail(msg: str) -> "None":
    sys.stderr.write(f"    x {msg}\n")
    sys.exit(1)


def note(msg: str) -> None:
    print(f"    - {msg}")


def warn(msg: str) -> None:
    print(f"    ! {msg}")


def load_text(path: str) -> str:
    with open(path, "r", encoding="utf-8") as f:
        return f.read()


VOLUME_NAME_RE = re.compile(r"^[a-zA-Z0-9_.-]+$")


def parse_compose_services(compose_path: str):
    """Parse dev compose into service -> metadata dict.

    Returns (services, top_volumes) where services: name -> {
        volumes: [str], has_src_mount: bool, command: str,
        env: [str], dockerfile: str|None, build_target: str|None,
    }
    """
    lines = load_text(compose_path).splitlines()
    services = {}
    top_volumes = []
    in_services = False
    in_top_volumes = False
    current = None
    svc = None
    in_volumes = False
    in_build = False
    in_env = False

    service_re = re.compile(r"^  ([a-z0-9][a-z0-9-]*):\s*$")
    top_key_re = re.compile(r"^[a-zA-Z]")

    def flush():
        nonlocal current, svc
        if current and svc:
            services[current] = svc
        current = None
        svc = None

    for raw in lines:
        if raw == "services:":
            in_services = True
            in_top_volumes = False
            continue
        if raw == "volumes:" and not raw.startswith(" "):
            flush()
            in_services = False
            in_top_volumes = True
            continue
        if in_top_volumes:
            vm = re.match(r"^  ([a-zA-Z0-9_.-]+):?\s*$", raw)
            if vm:
                top_volumes.append(vm.group(1))
            elif raw.strip() == "" or (top_key_re.match(raw) and not raw.startswith(" ")):
                in_top_volumes = False
            continue
        if in_services and top_key_re.match(raw) and not raw.startswith(" "):
            flush()
            in_services = False
            in_volumes = False
            in_build = False
            in_env = False
            continue
        if not in_services:
            continue

        m = service_re.match(raw)
        if m:
            flush()
            current = m.group(1)
            svc = {"volumes": [], "command": "", "has_src_mount": False,
                   "env": [], "dockerfile": None, "build_target": None}
            in_volumes = False
            in_build = False
            in_env = False
            continue

        if svc is None:
            continue

        if re.match(r"^    volumes:\s*$", raw):
            in_volumes = True
            in_build = False
            in_env = False
            continue
        if re.match(r"^    build:\s*$", raw):
            in_build = True
            in_volumes = False
            in_env = False
            continue
        if re.match(r"^    environment:\s*$", raw):
            in_env = True
            in_volumes = False
            in_build = False
            continue

        cm = re.match(r"^    command:\s*(.+?)\s*$", raw)
        if cm:
            svc["command"] = cm.group(1).strip()
            in_volumes = False
            in_build = False
            in_env = False
            continue

        if re.match(r"^    [a-zA-Z]", raw):
            if not raw.startswith("      "):
                in_volumes = raw.startswith("    volumes:")
                in_build = raw.startswith("    build:")
                in_env = raw.startswith("    environment:")
            continue

        if in_volumes:
            vm = re.match(r"^\s+-\s+(.+?)\s*$", raw)
            if vm:
                val = vm.group(1).strip('"').strip("'")
                svc["volumes"].append(val)
                if val == "./:/src" or val == ".:/src":
                    svc["has_src_mount"] = True
            elif re.match(r"^    [a-zA-Z]", raw):
                in_volumes = False

        if in_build:
            dm = re.match(r"^\s+dockerfile:\s*(.+?)\s*$", raw)
            if dm:
                svc["dockerfile"] = dm.group(1).strip().strip('"').strip("'")
            tm = re.match(r"^\s+target:\s*(.+?)\s*$", raw)
            if tm:
                svc["build_target"] = tm.group(1).strip().strip('"').strip("'")

        if in_env:
            em = re.match(r"^\s+-\s+(.+?)\s*$", raw)
            if em:
                svc["env"].append(em.group(1).strip().strip('"').strip("'"))
            else:
                km = re.match(r"^\s+([A-Za-z0-9_]+):\s*(.*)$", raw)
                if km:
                    svc["env"].append(km.group(1))

    flush()
    return services, top_volumes


def extract_project_from_command(command: str) -> str | None:
    m = re.search(r"--project[\s\"',]+([^\"'\s,\]]+\.csproj)", command)
    if m:
        return m.group(1).replace("\\", "/")
    m = re.search(r"dotnet\s+build[\s\"',]+([^\"'\s,\]]+\.csproj)", command)
    if m:
        return m.group(1).replace("\\", "/")
    return None


def normalize_rel(path: str) -> str:
    return posixpath.normpath(path.replace("\\", "/")).lstrip("./")


def project_refs(csproj_path: str, root: str):
    refs = []
    try:
        tree = ET.parse(csproj_path)
    except Exception:
        return refs
    base = os.path.dirname(csproj_path)
    for elem in tree.iter():
        if elem.tag.split("}")[-1] != "ProjectReference":
            continue
        inc = elem.attrib.get("Include")
        if not inc:
            continue
        inc = inc.replace("\\", "/")
        ref_path = os.path.normpath(os.path.join(base, inc))
        if os.path.isfile(ref_path):
            refs.append(ref_path)
    return refs


def transitive_closure(entry_csproj: str, root: str):
    seen = set()
    order = []
    stack = [os.path.normpath(entry_csproj)]
    while stack:
        cur = stack.pop()
        key = os.path.normcase(cur)
        if key in seen:
            continue
        seen.add(key)
        order.append(cur)
        for dep in project_refs(cur, root):
            dkey = os.path.normcase(dep)
            if dkey not in seen:
                stack.append(dep)
    return order


def expected_mounts_for_project(csproj_path: str, root: str):
    rel = os.path.relpath(csproj_path, root).replace("\\", "/")
    proj_dir = posixpath.dirname(rel)
    obj_target = f"/src/{proj_dir}/obj"
    bin_target = f"/src/{proj_dir}/bin"
    return rel, obj_target, bin_target


def volume_targets(volumes):
    targets = set()
    anon = set()
    for v in volumes:
        parts = v.split(":")
        if len(parts) >= 2 and VOLUME_NAME_RE.match(parts[0]):
            targets.add(parts[1].rstrip("/"))
        else:
            anon.add(v.strip().rstrip("/"))
    return targets, anon


def is_watch_service(svc) -> bool:
    cmd = svc.get("command", "")
    return bool(re.search(r'["\']watch["\']', cmd)) or bool(re.search(r'["\']run["\']', cmd))


def is_dotnet_service(svc) -> bool:
    return "dotnet" in svc.get("command", "")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=".")
    ap.add_argument("--compose", default="docker-compose.dev.yml")
    args = ap.parse_args()
    root = os.path.abspath(args.root)
    compose_path = os.path.join(root, args.compose)
    if not os.path.isfile(compose_path):
        fail(f"не найден {args.compose}")

    services, top_volumes = parse_compose_services(compose_path)
    top_volume_set = set(top_volumes)

    problems = []
    warnings = []
    checked = 0
    unchecked = []

    for name in sorted(services.keys()):
        svc = services[name]
        if not svc["has_src_mount"]:
            continue
        project_rel = extract_project_from_command(svc["command"])
        if not project_rel:
            unchecked.append(name)
            continue
        project_path = os.path.normpath(os.path.join(root, project_rel))
        if not os.path.isfile(project_path):
            problems.append((name, project_rel, "проект не найден", ""))
            continue

        checked += 1
        closure = transitive_closure(project_path, root)
        targets, anon = volume_targets(svc["volumes"])

        for dep in closure:
            dep_rel, obj_t, bin_t = expected_mounts_for_project(dep, root)
            missing = []
            if obj_t not in targets:
                missing.append("obj")
            if bin_t not in targets:
                missing.append("bin")
            if missing:
                problems.append((name, dep_rel, ",".join(missing), ""))

        if is_watch_service(svc):
            proj_dir = posixpath.dirname(project_rel)
            props_target = f"/src/{proj_dir}/Properties"
            if props_target not in anon:
                warnings.append(f"{name}: нет анонимного overlay Properties ({props_target})")
            if "DOTNET_USE_POLLING_FILE_WATCHER" not in svc.get("env", []):
                warnings.append(f"{name}: нет DOTNET_USE_POLLING_FILE_WATCHER=1")

    if unchecked:
        warnings.append("не проверены (нет парсируемого .csproj в command): " + ", ".join(unchecked))

    # Top-level volume declarations
    used_named = set()
    for svc in services.values():
        for v in svc["volumes"]:
            parts = v.split(":")
            if len(parts) >= 2 and VOLUME_NAME_RE.match(parts[0]) and "/" not in parts[0]:
                used_named.add(parts[0])
    undeclared = sorted(used_named - top_volume_set)
    if undeclared:
        problems.append(("(compose)", "top-level volumes", "не объявлены", ", ".join(undeclared[:8])))

    # .dockerignore
    dockerignore_path = os.path.join(root, ".dockerignore")
    if not os.path.isfile(dockerignore_path):
        problems.append(("(repo)", ".dockerignore", "отсутствует", ""))
    else:
        content = load_text(dockerignore_path).lower()
        for needle in ("obj", "bin", "node_modules"):
            if needle not in content:
                warnings.append(f".dockerignore: нет исключения для {needle}")

    # SDK tag consistency across dev Dockerfiles
    sdk_tags = set()
    dockerfiles_checked = 0
    for svc in services.values():
        df = svc.get("dockerfile")
        if not df:
            continue
        df_path = os.path.normpath(os.path.join(root, df))
        if not os.path.isfile(df_path):
            continue
        dockerfiles_checked += 1
        try:
            text = load_text(df_path)
        except Exception:
            continue
        for line in text.splitlines():
            m = re.match(r"^\s*FROM\s+\S*dotnet/sdk:(\S+)\s+AS\s+dev\s*$", line, re.IGNORECASE)
            if m:
                sdk_tags.add(m.group(1))
    if dockerfiles_checked and len(sdk_tags) > 1:
        warnings.append("разные теги SDK в dev-стейджах: " + ", ".join(sorted(sdk_tags)))
    elif dockerfiles_checked and sdk_tags:
        note(f"SDK dev-стейджи: {', '.join(sorted(sdk_tags))}")

    # Report
    if not problems and not warnings:
        print(f"    ok: {checked} dev-сервисов проверено, obj/bin volumes совпадают с ProjectReference.")
        return 0

    if problems:
        print(f"    x несоответствия в {args.compose}:")
        current = None
        for svc_name, dep_rel, missing, detail in problems:
            if svc_name != current:
                print(f"      - {svc_name}:")
                current = svc_name
            line = f"          {missing:>14}  {dep_rel}"
            if detail:
                line += f"  ({detail})"
            print(line)
        print()

    if warnings:
        print("    Предупреждения:")
        for w in warnings:
            print(f"      ! {w}")
        print()

    if problems:
        print("    Причина: dev-сервисы используют ./:/src и сборка без изолированного")
        print("    obj/bin volume приводит к гонкам dotnet watch/build на общих артефактах.")
        print("    Исправление: добавить obj/bin named volumes для каждого проекта")
        print("    из транзитивного замыкания ProjectReference.")
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
