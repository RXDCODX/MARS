import { useEffect, useRef, useState } from "react";

import { ChatMessage } from "@/shared/api";
import {
  decodeJsonBranch,
  readStringField,
} from "@/shared/realtime/overlayPayload";
import { useOverlayEvent } from "@/shared/realtime/useOverlayEvent";
import Announce from "@/shared/Utils/Announce/Announce";

import { Message } from "./Message";

interface ChatHorizontalProperties {
  messages?: ChatMessage[];
  onRemoveMessage?: (id: string) => void;
}

export default function ChatHorizontal({
  messages: externalMessages,
  onRemoveMessage,
}: ChatHorizontalProperties) {
  const [internalMessages, setInternalMessages] = useState<ChatMessage[]>([]);
  const [announced, setAnnounced] = useState(false);

  // --- SLOT SYSTEM ---
  const SLOT_HEIGHT = 60; // px, можно скорректировать под среднюю высоту сообщения
  const [slots, setSlots] = useState<(string | null)[]>([]); // id сообщения или null
  const [queue, setQueue] = useState<ChatMessage[]>([]);
  const slotCountReference = useRef(0);

  // Вычисляем количество слотов при монтировании/изменении размера окна
  useEffect(() => {
    const updateSlots = () => {
      const count = Math.max(1, Math.floor(window.innerHeight / SLOT_HEIGHT));
      slotCountReference.current = count;
      setSlots(previous => {
        if (previous.length === count) return previous;
        // Если увеличилось — добавляем null, если уменьшилось — обрезаем
        if (previous.length < count)
          return [
            ...previous,
            ...Array.from({ length: count - previous.length }, () => null),
          ];
        return previous.slice(0, count);
      });
    };
    updateSlots();
    window.addEventListener("resize", updateSlots);
    return () => window.removeEventListener("resize", updateSlots);
  }, []);

  // Используем внешние сообщения или внутренние
  const messages =
    externalMessages === undefined ? internalMessages : externalMessages;
  const handleRemove =
    onRemoveMessage ||
    ((id: string) => {
      setInternalMessages(previous => previous.filter(m => m.id !== id));
    });

  // Подписки на хаб оверлея работают только если не переданы внешние сообщения
  useOverlayEvent("NewMessage", payload => {
    if (externalMessages) {
      return;
    }

    // Событие едет содержимым ветки — { id, messageJson }, где само
    // сообщение объявлено полем bytes — приходит массивом байт. Раньше
    // обработчик получал готовые аргументы от резолвера SignalR: он разбирал
    // протокол и отдавал (id, message). Теперь разбор виден в коде и
    // проверяется типами.
    const id = readStringField(payload, "id");
    const decoded = decodeJsonBranch(payload, "messageJson");

    if (id === undefined || decoded === undefined) {
      return;
    }

    const message = decoded as ChatMessage;
    message.id ??= id;

    setInternalMessages(previous => {
      while (previous.length >= 50) {
        previous.pop();
      }
      return previous.find(m => m.id === message.id)
        ? previous
        : [message, ...previous];
    });
  });

  useOverlayEvent("DeleteMessage", payload => {
    const id = readStringField(payload, "id");

    if (id === undefined) {
      return;
    }

    setInternalMessages(previous => previous.filter(m => m.id !== id));
  });

  // --- SLOT LOGIC ---
  // Сопоставление id сообщения -> индекс слота
  const [messageSlotMap, setMessageSlotMap] = useState<Record<string, number>>(
    {}
  );

  // Когда приходит новое сообщение — пытаемся положить в свободный слот или в очередь
  useEffect(() => {
    // Берем только новые сообщения, которых нет в слотах и очереди
    const activeIds = new Set([
      ...(slots.filter(Boolean) as string[]),
      ...queue.map(m => m.id!),
    ]);
    const newMessages = messages.filter(m => !activeIds.has(m.id!));
    if (newMessages.length === 0) return;
    setQueue(previous => [...previous, ...newMessages]);
  }, [messages, slots, queue]);

  // Когда есть свободные слоты и очередь — размещаем сообщения
  useEffect(() => {
    if (queue.length === 0) return;
    setSlots(previousSlots => {
      const newSlots = [...previousSlots];
      let isChanged = false;
      const newMessageSlotMap = { ...messageSlotMap };
      // Собираем индексы всех свободных слотов
      const freeIndices = newSlots
        .map((s, index) => (s === null ? index : null))
        .filter(v => v !== null) as number[];
      // Для каждого сообщения из очереди выбираем случайный свободный слот
      queue.forEach(message => {
        if (freeIndices.length === 0) return;
        const randIndex = Math.floor(Math.random() * freeIndices.length);
        const freeIndex = freeIndices[randIndex];
        if (freeIndex !== undefined) {
          newSlots[freeIndex] = message.id!;
          newMessageSlotMap[message.id!] = freeIndex;
          isChanged = true;
          // Удаляем этот индекс из списка свободных
          freeIndices.splice(randIndex, 1);
        }
      });
      if (isChanged) {
        setMessageSlotMap(newMessageSlotMap);
        // Удаляем из очереди те, что заняли слот
        setQueue(previousQueue =>
          previousQueue.filter(message => !newSlots.includes(message.id!))
        );
        return newSlots;
      }
      return previousSlots;
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [queue, slots]);

  // Когда сообщение завершает анимацию — освобождаем слот
  const removeFromSlot = (message: ChatMessage) => {
    const slotIndex = messageSlotMap[message.id!];
    setSlots(previous => {
      const newSlots = [...previous];
      if (slotIndex !== undefined) newSlots[slotIndex] = null;
      return newSlots;
    });
    setMessageSlotMap(previous => {
      const copy = { ...previous };
      delete copy[message.id!];
      return copy;
    });
    handleRemove(message.id!);
  };

  // ---
  return (
    <>
      {!announced && (
        <Announce
          title={"Chat Horizontal"}
          callback={() => setAnnounced(true)}
        />
      )}
      {slots.map((messageId, slotIndex) => {
        if (!messageId) return null;
        const message = messages.find(m => m.id === messageId);
        if (!message) return null;
        return (
          <Message
            key={message.id}
            message={message}
            callback={() => removeFromSlot(message)}
            slotTop={slotIndex * SLOT_HEIGHT}
          />
        );
      })}
    </>
  );
}
