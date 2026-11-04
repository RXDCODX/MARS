import { getRandomColor } from "../../Utils";

interface Properties {
  keyWordedString: string;
  keySymbol: string;
  classNameForKeyWordedSpan: string;
  isQuouted?: boolean;
  keyWordColor?: string;
  textColor?: string;
}

export function KeyWordedText({
  keySymbol = "#",
  keyWordedString,
  classNameForKeyWordedSpan = "key_word",
  keyWordColor = getRandomColor(),
  textColor = "white",
  isQuouted,
}: Properties) {
  if (keySymbol.length !== 1) {
    return;
  }

  if (!keyWordedString) {
    return;
  }

  const regex: RegExp = isQuouted
    ? new RegExp(`(${keySymbol}.*?${keySymbol})`, "g")
    : new RegExp(String.raw`(${keySymbol}\S+)`, "g");

  const parts = keyWordedString.split(regex);

  return (
    <div style={{ color: textColor, width: "100%" }}>
      <div style={{ width: "100%", textAlign: "center" }}>
        {parts.map((part, index) => {
          if (part.startsWith(keySymbol) && part.endsWith(keySymbol)) {
            const content = part.slice(1, -1);
            return (
              <span
                key={index}
                style={{ color: keyWordColor }}
                className={classNameForKeyWordedSpan}
              >
                {content}
              </span>
            );
          }
          return <span key={index}>{part}</span>;
        })}
      </div>
    </div>
  );
}
