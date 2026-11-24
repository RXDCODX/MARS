using System.Text.Json;
using System.Text.Json.Serialization;

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Платформа, с которой инициирована команда.
/// </summary>
public enum CommandPlatform
{
    Api = 0,
    Telegram = 1,
    Twitch = 2,
    Discord = 3,
}
