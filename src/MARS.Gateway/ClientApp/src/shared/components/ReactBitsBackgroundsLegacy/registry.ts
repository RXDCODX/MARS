import type { ComponentType, LazyExoticComponent } from "react";
import { lazy } from "react";

type AnyComponent = ComponentType<any>;

export const allReactBitsBackgroundNames = [
  "Aurora",
  "Balatro",
  "Ballpit",
  "Beams",
  "ColorBends",
  "DarkVeil",
  "Dither",
  "DotGrid",
  "EvilEye",
  "FaultyTerminal",
  "FloatingLines",
  "Galaxy",
  "GradientBlinds",
  "Grainient",
  "GridDistortion",
  "GridMotion",
  "GridScan",
  "Hyperspeed",
  "Iridescence",
  "LetterGlitch",
  "Lightning",
  "LightPillar",
  "LightRays",
  "LineWaves",
  "LiquidChrome",
  "LiquidEther",
  "Orb",
  "Particles",
  "PixelBlast",
  "PixelSnow",
  "Plasma",
  "Prism",
  "PrismaticBurst",
  "Radar",
  "RippleGrid",
  "ShapeGrid",
  "Silk",
  "SoftAurora",
  "Threads",
  "Waves",
] as const;

export type ReactBitsBackgroundName =
  (typeof allReactBitsBackgroundNames)[number];

export const reactBitsBackgroundComponentRegistry: Record<
  ReactBitsBackgroundName,
  LazyExoticComponent<AnyComponent>
> = {
  Aurora: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Aurora/Aurora").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Aurora"],
      })
    )
  ),
  Balatro: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Balatro/Balatro").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Balatro"],
      })
    )
  ),
  Ballpit: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Ballpit/Ballpit").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Ballpit"],
      })
    )
  ),
  Beams: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Beams/Beams").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Beams"],
      })
    )
  ),
  ColorBends: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/ColorBends/ColorBends").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["ColorBends"],
      })
    )
  ),
  DarkVeil: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/DarkVeil/DarkVeil").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["DarkVeil"],
      })
    )
  ),
  Dither: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Dither/Dither").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Dither"],
      })
    )
  ),
  DotGrid: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/DotGrid/DotGrid").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["DotGrid"],
      })
    )
  ),
  EvilEye: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/EvilEye/EvilEye").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["EvilEye"],
      })
    )
  ),
  FaultyTerminal: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/FaultyTerminal/FaultyTerminal").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["FaultyTerminal"],
      })
    )
  ),
  FloatingLines: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/FloatingLines/FloatingLines").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["FloatingLines"],
      })
    )
  ),
  Galaxy: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Galaxy/Galaxy").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Galaxy"],
      })
    )
  ),
  GradientBlinds: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/GradientBlinds/GradientBlinds").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["GradientBlinds"],
      })
    )
  ),
  Grainient: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Grainient/Grainient").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Grainient"],
      })
    )
  ),
  GridDistortion: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/GridDistortion/GridDistortion").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["GridDistortion"],
      })
    )
  ),
  GridMotion: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/GridMotion/GridMotion").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["GridMotion"],
      })
    )
  ),
  GridScan: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/GridScan/GridScan").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["GridScan"],
      })
    )
  ),
  Hyperspeed: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Hyperspeed/Hyperspeed").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Hyperspeed"],
      })
    )
  ),
  Iridescence: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Iridescence/Iridescence").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Iridescence"],
      })
    )
  ),
  LetterGlitch: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/LetterGlitch/LetterGlitch").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["LetterGlitch"],
      })
    )
  ),
  Lightning: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Lightning/Lightning").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Lightning"],
      })
    )
  ),
  LightPillar: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/LightPillar/LightPillar").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["LightPillar"],
      })
    )
  ),
  LightRays: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/LightRays/LightRays").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["LightRays"],
      })
    )
  ),
  LineWaves: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/LineWaves/LineWaves").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["LineWaves"],
      })
    )
  ),
  LiquidChrome: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/LiquidChrome/LiquidChrome").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["LiquidChrome"],
      })
    )
  ),
  LiquidEther: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/LiquidEther/LiquidEther").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["LiquidEther"],
      })
    )
  ),
  Orb: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Orb/Orb").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Orb"],
      })
    )
  ),
  Particles: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Particles/Particles").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Particles"],
      })
    )
  ),
  PixelBlast: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/PixelBlast/PixelBlast").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["PixelBlast"],
      })
    )
  ),
  PixelSnow: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/PixelSnow/PixelSnow").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["PixelSnow"],
      })
    )
  ),
  Plasma: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Plasma/Plasma").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Plasma"],
      })
    )
  ),
  Prism: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Prism/Prism").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Prism"],
      })
    )
  ),
  PrismaticBurst: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/PrismaticBurst/PrismaticBurst").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["PrismaticBurst"],
      })
    )
  ),
  Radar: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Radar/Radar").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Radar"],
      })
    )
  ),
  RippleGrid: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/RippleGrid/RippleGrid").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["RippleGrid"],
      })
    )
  ),
  ShapeGrid: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/ShapeGrid/ShapeGrid").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["ShapeGrid"],
      })
    )
  ),
  Silk: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Silk/Silk").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Silk"],
      })
    )
  ),
  SoftAurora: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/SoftAurora/SoftAurora").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["SoftAurora"],
      })
    )
  ),
  Threads: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Threads/Threads").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Threads"],
      })
    )
  ),
  Waves: lazy(() =>
    import("@/shared/components/ReactBitsBackgroundsLegacy/Waves/Waves").then(
      module => ({
        default:
          (module as Record<string, AnyComponent>).default ??
          (module as Record<string, AnyComponent>)["Waves"],
      })
    )
  ),
};
