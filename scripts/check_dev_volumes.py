#!/usr/bin/env python3
"""Checks docker-compose.dev.yml for missing per-service obj/bin volume mounts.

Dev services bind-mount the repo at /src and overlay named volumes for project
obj/bin directories. If a project in a service's transitive dependency closure
is built without an isolated obj/bin mount, multiple containers share the same
bind-mount artifacts and dotnet watch/build race on them.

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


def load_text(path: str) -> str:
    with open(path, "r", encoding="utf-8-sig") as f:
        return f.read()


def parse_compose_services(compose_path: str):
    """Parse dev compose into service -> {command, volumes, build_target}.

    Simple line-based parser for the regular structure used in this repo.
    """
    lines = load_text(compose_path).splitlines()
    services = {}
    in_services = False
    current = None
    svc = None
    in_volumes = False
    in_build = False

    service_re = re.compile(r"^  ([a-z0-9][a-z0-9-]*):\s*$")
    top_key_re = re.compile(r"^[a-zA-Z]")

    for raw in lines:
        if raw == "services:":
            in_services = True
            continue
        if in_services and top_key_re.match(raw) and not raw.startswith(" "):
            # left services section
            if current and svc:
                services[current] = svc
            in_services = False
            current = None
            svc = None
            in_volumes = False
            in_build = False
            continue
        if not in_services:
            continue

        m = service_re.match(raw)
        if m:
            if current and svc:
                services[current] = svc
            current = m.group(1)
            svc = {"volumes": [], "command": "", "has_src_mount": False}
            in_volumes = False
            in_build = False
            continue

        if svc is None:
            continue

        if re.match(r"^    volumes:\s*$", raw):
            in_volumes = True
            in_build = False
            continue
        if re.match(r"^    build:\s*$", raw):
            in_build = True
            in_volumes = False
            continue

        cm = re.match(r"^    command:\s*(.+?)\s*$", raw)
        if cm:
            svc["command"] = cm.group(1).strip()
            in_volumes = False
            in_build = False
            continue

        if re.match(r"^    [a-zA-Z]", raw):
            # another service key
            if not raw.startswith("      "):
                in_volumes = raw.startswith("    volumes:")
                in_build = raw.startswith("    build:")
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

    if in_services and current and svc:
        services[current] = svc
    return services


def extract_project_from_command(command: str) -> str | None:
    # matches --project path/to.csproj or "--project", "path/to.csproj"
    m = re.search(r"--project[\s\"',]+([^\"'\s,\]]+\.csproj)", command)
    if m:
        return m.group(1).replace("\\", "/")
    # also handle dotnet build src/.../*.csproj
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
    for v in volumes:
        parts = v.split(":")
        if len(parts) >= 2:
            if v.startswith("./") or v.startswith(".:") or v.startswith("/"):
                target = parts[1] if len(parts) >= 2 else ""
            else:
                target = parts[1] if len(parts) >= 2 else ""
            targets.add(target.rstrip("/"))
    return targets


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=".")
    ap.add_argument("--compose", default="docker-compose.dev.yml")
    args = ap.parse_args()
    root = os.path.abspath(args.root)
    compose_path = os.path.join(root, args.compose)
    if not os.path.isfile(compose_path):
        fail(f"?? ?????? {args.compose}")

    services = parse_compose_services(compose_path)
    problems = []
    checked = 0

    for name in sorted(services.keys()):
        svc = services[name]
        if not svc["has_src_mount"]:
            continue
        project_rel = extract_project_from_command(svc["command"])
        if not project_rel:
            continue
        project_path = os.path.normpath(os.path.join(root, project_rel))
        if not os.path.isfile(project_path):
            problems.append((name, project_rel, "project not found", ""))
            continue

        checked += 1
        closure = transitive_closure(project_path, root)
        targets = volume_targets(svc["volumes"])

        for dep in closure:
            dep_rel, obj_t, bin_t = expected_mounts_for_project(dep, root)
            missing = []
            if obj_t not in targets:
                missing.append("obj")
            if bin_t not in targets:
                missing.append("bin")
            if missing:
                problems.append((name, dep_rel, ",".join(missing), obj_t.rsplit("/obj", 1)[0]))

    if not problems:
        print(f"    ok: {checked} dev-???????? ?????????, obj/bin volumes ????????? ????????? ProjectReference.")
        return 0

    print(f"    x ??????? ????????????? obj/bin volume mounts ? {args.compose}:")
    current = None
    for svc_name, dep_rel, missing, _ in problems:
        if svc_name != current:
            print(f"      - {svc_name}:")
            current = svc_name
        print(f"          {missing:>7}  {dep_rel}")
    print()
    print("    ???????: dev-?????????? ????????? ./:/src ? ???????? ????? ??????????? ? ????? bind-mount.")
    print("    ??? per-service obj/bin volume ??? ??????????? dotnet watch/build ? ?????? ???????????")
    print("    ???????? ?? ???? ? ?? ?? ????? (GenerateDepsFile/AssemblyAttributes/locked dll).")
    print("    ???????????: ???????? obj/bin named volumes ??? ??????? ?????????????? ??????? ? ??????")
    print("    ? ??????????????? ?????? ? top-level volumes:.")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
