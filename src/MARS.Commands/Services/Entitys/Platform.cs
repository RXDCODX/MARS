using System;

namespace MARS.Commands.Services.Entitys;

[Flags]
public enum Platform
{
    None,
    Api,
    Telegram,
    Twitch,
    Discord,
    Vk,
    All = Api | Telegram | Twitch | Discord | Vk,
}
