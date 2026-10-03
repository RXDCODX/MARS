import { useCallback, useState } from "react";

import { TelegramusHubSignalRContext as SignalRContext } from "@/shared/api";
import { ChatMessage } from "@/shared/api";
import { TelegramusMakeScreenParticlesCreateParamsParticlesEnum } from "@/shared/api/";

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

  SignalRContext.useSignalREffect(
    "MakeScreenParticles",
    (type: TelegramusMakeScreenParticlesCreateParamsParticlesEnum) => {
      const newMessage = { type: type, id: count };
      setCount(count + 1);
      setMessages(previous => [...previous, newMessage]);
    },
    []
  );

  SignalRContext.useSignalREffect(
    "MakeScreenEmojisParticles",
    (mediaDto: ChatMessage) => {
      const newMessage = { input: mediaDto, id: count };
      setCount(count + 1);
      setEmojis(previous => [...previous, newMessage]);
    },
    []
  );

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
