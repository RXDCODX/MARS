import { useCallback, useState } from "react";

import { ChatMessage } from "@/shared/api";
import { TelegramusMakeScreenParticlesCreateParamsParticlesEnum } from "@/shared/api/";
import { decodeJsonBranch } from "@/shared/realtime/overlayPayload";
import { useOverlayEvent } from "@/shared/realtime/useOverlayEvent";

import { Confettyv2 } from "./Confetty";
import EmojiParticles from "./EmojiParticles";
import Firework from "./Firework";

interface base {
  id: number;
}

interface particles extends base {
  type: TelegramusMakeScreenParticlesCreateParamsParticlesEnum;
}

interface emojis extends base {
  input: string | ChatMessage;
}

export default function Manager() {
  const [count, setCount] = useState<number>(0);
  const [messages, setMessages] = useState<particles[]>([]);
  const [emojis, setEmojis] = useState<emojis[]>([]);

  useOverlayEvent("MakeScreenParticles", payload => {
    // Событие едет веткой { makeScreenParticles: { particlesJson } }, где
    // полезная нагрузка — поле bytes, то есть массив байт. Раньше обработчик
    // получал готовый тип частиц от резолвера SignalR.
    const decoded = decodeJsonBranch(payload, "particlesJson") as
      | TelegramusMakeScreenParticlesCreateParamsParticlesEnum
      | undefined;

    if (decoded === undefined) {
      return;
    }

    const newMessage = { type: decoded, id: count };
    setCount(count + 1);
    setMessages(previous => [...previous, newMessage]);
  });

  useOverlayEvent("MakeScreenEmojisParticles", payload => {
    const decoded = decodeJsonBranch(payload, "messageJson") as
      | ChatMessage
      | undefined;

    if (decoded === undefined) {
      return;
    }

    const newMessage = { input: decoded, id: count };
    setCount(count + 1);
    setEmojis(previous => [...previous, newMessage]);
  });

  const removeMessage = useCallback((id: number) => {
    setMessages(previous => previous.filter(message => message.id !== id));
  }, []);

  return (
    <>
      {messages.length > 0 &&
        messages.map(message => {
          switch (message.type) {
            case TelegramusMakeScreenParticlesCreateParamsParticlesEnum.Confetty: {
              return (
                <Confettyv2
                  key={message.id}
                  callback={() => removeMessage(message.id)}
                />
              );
            }
            case TelegramusMakeScreenParticlesCreateParamsParticlesEnum.Fireworks: {
              return (
                <Firework
                  key={message.id}
                  callback={() => removeMessage(message.id)}
                />
              );
            }
          }
        })}
      {emojis.length > 0 &&
        emojis.map(message => (
          <EmojiParticles key={message.id} input={message.input} />
        ))}
    </>
  );
}
