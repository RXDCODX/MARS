import { type ComponentType, createElement, type ReactNode } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, test, vi } from "vitest";

vi.mock("@react-three/fiber", () => ({
  Canvas: ({ children }: { children?: ReactNode }) =>
    createElement("div", { "data-testid": "mock-r3f-canvas" }, children),
  useFrame: () => {},
  useThree: () => ({
    viewport: { width: 1, height: 1 },
    camera: {},
    scene: {},
    gl: {},
  }),
  extend: () => {},
  createPortal: (children: unknown) => children,
}));

vi.mock("@react-three/drei", () => {
  const Mock = ({ children }: { children?: ReactNode }) =>
    createElement("div", { "data-testid": "mock-drei" }, children);

  return {
    PerspectiveCamera: Mock,
    Environment: Mock,
    OrbitControls: Mock,
    Float: Mock,
    MeshTransmissionMaterial: Mock,
    Sphere: Mock,
  };
});

vi.mock("@react-three/postprocessing", () => {
  const Mock = ({ children }: { children?: ReactNode }) =>
    createElement("div", { "data-testid": "mock-postprocessing" }, children);

  return {
    EffectComposer: Mock,
    wrapEffect:
      () =>
      ({ children }: { children?: ReactNode }) =>
        createElement(
          "div",
          { "data-testid": "mock-wrapped-effect" },
          children
        ),
  };
});

import {
  allReactBitsBackgroundNames,
  type ReactBitsBackgroundName,
} from "./registry";

const moduleLoaders = import.meta.glob("./**/*.tsx");

const backgroundModuleEntries = Object.entries(moduleLoaders).filter(
  ([modulePath]) => {
    const normalizedPath = modulePath.replaceAll("\\", "/");
    const parts = normalizedPath.split("/");
    const folderName = parts.at(-2);
    const fileName = parts.at(-1)?.replace(".tsx", "");

    // Берем только основной компонент папки, например Aurora/Aurora.tsx
    return folderName === fileName;
  }
);

const renderPropertiesByBackground: Partial<
  Record<ReactBitsBackgroundName, object>
> = {
  GridDistortion: {
    imageSrc: "https://example.com/test-image.png",
  },
};

const toBackgroundName = (modulePath: string): ReactBitsBackgroundName => {
  const normalizedPath = modulePath.replaceAll("\\", "/");
  const parts = normalizedPath.split("/");
  const folderName = parts.at(-2) as ReactBitsBackgroundName;
  return folderName;
};

describe("ReactBitsBackgroundsLegacy SSR smoke", () => {
  it("contains entries for all vendored backgrounds", () => {
    expect(backgroundModuleEntries.length).toBe(
      allReactBitsBackgroundNames.length
    );
  });

  test.each(backgroundModuleEntries)(
    "imports and SSR-renders %s",
    async (modulePath, loader) => {
      const moduleExports = (await (
        loader as () => Promise<unknown>
      )()) as Record<string, unknown>;

      const componentName = toBackgroundName(modulePath);
      const component =
        (moduleExports.default as ComponentType<object> | undefined) ??
        (moduleExports[componentName] as ComponentType<object> | undefined);

      expect(component).toBeDefined();

      const properties = renderPropertiesByBackground[componentName] ?? {};
      const markup = renderToStaticMarkup(
        createElement(component!, properties)
      );

      expect(markup).toBeTruthy();
      expect(markup.length).toBeGreaterThan(0);
    }
  );
});
