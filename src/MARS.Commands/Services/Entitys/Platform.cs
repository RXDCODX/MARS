using System;

namespace MARS.Commands.Services.Entitys;

[Flags]
public enum Platform
{
    // Значения обязаны быть степенями двойки. Без явных присваиваний компилятор
    // выдаёт 0,1,2,3,4,5, и тогда [Flags] становится ложью: Twitch | Discord даёт
    // 3 | 4 = 7, а Platform.All — тоже 7. Любая битовая проверка после этого
    // молча выдаёт мусор, поэтому IsAvailableOnPlatform сравнивал через
    // Enumerable.Contains, а агрегат платформ не означал «и там, и там».
    None = 0,
    Api = 1,
    Telegram = 2,
    Twitch = 4,
    Discord = 8,
    Vk = 16,
    All = Api | Telegram | Twitch | Discord | Vk,
}
