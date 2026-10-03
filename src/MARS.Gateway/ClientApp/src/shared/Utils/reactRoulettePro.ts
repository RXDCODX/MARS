import RouletteProWrapped from "react-roulette-pro";

// Vite 8 (Rolldown) при CJS→ESM конвертации UMD-пакета react-roulette-pro
// не отдаёт компонент напрямую: дефолтный импорт может быть самим компонентом,
// обёрткой { default: Component } или loader-функцией, возвращающей module.exports.
// Резолвим все варианты на этапе инициализации модуля.
function unwrapRoulettePro(candidate: unknown): unknown {
  let value = candidate;

  while (value && typeof value === "object" && "default" in value) {
    value = (value as { default: unknown }).default;
  }

  if (typeof value === "function") {
    try {
      const probe = (value as () => unknown)();
      const probeDefault =
        probe && typeof probe === "object"
          ? (probe as { default?: unknown }).default
          : undefined;

      if (typeof probeDefault === "function") {
        return probeDefault;
      }
    } catch {
      // Это сам компонент — вызов вне React упал, используем его напрямую.
    }
  }

  return value;
}

export const RoulettePro = unwrapRoulettePro(
  RouletteProWrapped
) as typeof RouletteProWrapped;
