using MARS.Telegram.Configuration;
using MARS.Telegram.Services.BotService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TL;

namespace MARS.Telegram.Services.Messaging;

/// <summary>
/// Отправка сообщений ботом по MTProto.
/// </summary>
/// <remarks>
/// <para>
/// Разметка приходит готовой строкой с тегами — так её писали при Bot API, где
/// <c>ParseMode.Html</c> разбирал Telegram. Теперь разбор наш, и ошибка разметки
/// не должна уходить в чат сырым текстом: сообщение не отправляется вовсе, а
/// причина попадает в лог.
/// </para>
/// <para>
/// Адресата задаём по идентификатору чата с нулевым <c>access_hash</c> — для
/// бота Telegram такое допускает, иначе каждому администратору пришлось бы сначала
/// узнавать хеш через <c>messages.getFullChat</c>.
/// </para>
/// </remarks>
public sealed class MTProtoMessageSender(
    ITelegramBotSession session,
    IOptions<TelegramConfiguration> options,
    ILogger<MTProtoMessageSender> logger
) : ITelegramMessageSender
{
    /// <inheritdoc />
    public async Task<int> SendToAdminsAsync(string message, CancellationToken cancellationToken)
    {
        var result = 0;
        var admins = options.Value.AdminIds;

        if (admins.Length == 0)
        {
            logger.LogWarning(
                "Сообщение администраторам не отправлено: список пуст (Telegram__AdminIds__0)"
            );
        }

        foreach (var adminId in admins)
        {
            if (await SendAsync(adminId, message, cancellationToken))
            {
                result++;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<int> SendPlainToChatsAsync(
        IReadOnlyList<long> chatIds,
        string message,
        CancellationToken cancellationToken
    )
    {
        var result = 0;

        foreach (var chatId in chatIds)
        {
            if (await SendPlainAsync(chatId, message, cancellationToken))
            {
                result++;
            }
        }

        return result;
    }

    /// <summary>
    /// Отправка обычным текстом: сущностей нет, поэтому символы вроде «&lt;»
    /// уходят как есть и разбор разметки не может помешать доставке.
    /// </summary>
    private async Task<bool> SendPlainAsync(
        long chatId,
        string message,
        CancellationToken cancellationToken
    )
    {
        var result = false;

        try
        {
            var connection = await session.GetAsync(cancellationToken);

            await connection.SendTextAsync(new InputPeerUser(chatId, 0), message, null);

            result = true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Запись журнала в Telegram не отправлена");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> SendAsync(
        long chatId,
        string message,
        CancellationToken cancellationToken
    )
    {
        var result = false;

        try
        {
            var text = HtmlMessageEntities.Parse(message, out var entities);
            var connection = await session.GetAsync(cancellationToken);

            await connection.SendTextAsync(new InputPeerUser(chatId, 0), text, entities);

            result = true;
        }
        catch (FormatException e)
        {
            // Разметка не такая, какой её ждёт наш разбор: слать её «как есть»
            // нельзя, в чат уехал бы текст с тегами.
            logger.LogError(e, "Сообщение не отправлено: разметка не разобрана");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Сообщение в Telegram не отправлено");
        }

        return result;
    }
}
