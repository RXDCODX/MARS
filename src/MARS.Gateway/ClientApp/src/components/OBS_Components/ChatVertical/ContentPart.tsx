import { CSSProperties } from "react";

import { ChatMessage } from "@/shared/api";
import type { ContentPart } from "@/shared/Utils";

interface Properties {
  className?: string;
  style?: CSSProperties;
  part: ContentPart;
  message: ChatMessage;
}

// Функция для замены ссылок в тексте на 'ссылка' с нужным стилем
function replaceLinksWithStub(text: string) {
  // Простейший паттерн для ссылок (http/https/ftp)
  const urlRegex = /(https?:\/\/[^\s]+|ftp:\/\/[^\s]+)/gi;
  return text.split(urlRegex).map((part, index) => {
    if (urlRegex.test(part)) {
      return (
        <span key={index} className="linkStub">
          ссылка
        </span>
      );
    }
    return part;
  });
}

export default function ContentPart({ part, style, className }: Properties) {
  if (part.type === "text") {
    return (
      <div className={className} style={style}>
        {replaceLinksWithStub(
          typeof part.content === "string" ? part.content : ""
        )}
      </div>
    );
  }
  if (part.type === "link") {
    return (
      <span className={`linkStub ${className ?? ""}`.trim()} style={style}>
        ссылка
      </span>
    );
  }
  // Всё остальное не рендерим
  return null;
}
