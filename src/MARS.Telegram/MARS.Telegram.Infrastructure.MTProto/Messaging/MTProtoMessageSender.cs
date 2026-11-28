using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Application.Services;
using MARS.Telegram.Infrastructure.MTProto.BotService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TL;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Telegram.Infrastructure.MTProto.Messaging;

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
/// <para>
/// Администратор видит всё, что видит бот: входящие пересылаются ему в
/// <see cref="TelegramBotSession"/>, а исходящие дублируются копией. Если адресат
/// сам администратор, дубль не создаётся — иначе он получил бы каждое сообщение
/// дважды.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class MTProtoMessageSender(
    ITelegramBotSession session,
    IOptions<TelegramConfiguration> options,
    TelegramPeerResolver peerResolver,
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

        // Администратор видит весь исходящий поток: если среди адресатов его ещё
        // нет, шлём копию. Один раз на администратора, а не на каждый чат — иначе
        // одна запись журнала ушла бы админу трижды.
        if (result > 0)
        {
            var admins = options.Value.AdminIds;
            var copyTargets = admins.Where(admin => !chatIds.Contains(admin)).ToArray();

            foreach (var adminId in copyTargets)
            {
                await SendPlainAsync(
                    adminId,
                    $"[исходящее в чаты журнала]\n{message}",
                    cancellationToken
                );
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

            var peer = await peerResolver.ResolveAsync(chatId, connection, cancellationToken);
            if (peer is null)
            {
                logger.LogError(
                    "Запись журнала в Telegram не отправлена: не удалось определить адресат чата {ChatId}",
                    chatId
                );
                return false;
            }

            var chunks = MessageChunker.SplitPlain(message);
            if (chunks.SuppressedChars > 0)
            {
                logger.LogWarning(
                    "Запись журнала в Telegram обрезана: скрыто {Suppressed} из {Total} символов. Полный текст: {Message}",
                    chunks.SuppressedChars,
                    message.Length,
                    message
                );
            }
            else if (chunks.Chunks.Length > 1)
            {
                logger.LogDebug(
                    "Запись журнала в Telegram разбита на {Count} частей ({Total} символов)",
                    chunks.Chunks.Length,
                    message.Length
                );
            }

            foreach (var chunk in chunks.Chunks)
            {
                await connection.SendTextAsync(peer, chunk, null);
            }

            result = true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Запись журнала в Telegram не отправлена в чат {ChatId}", chatId);
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
            var chunks = MessageChunker.SplitHtml(message);
            if (chunks.SuppressedChars > 0)
            {
                logger.LogWarning(
                    "Сообщение в Telegram обрезано: скрыто {Suppressed} из {Total} символов. Полный текст: {Message}",
                    chunks.SuppressedChars,
                    message.Length,
                    message
                );
            }
            else if (chunks.Chunks.Length > 1)
            {
                logger.LogDebug(
                    "Сообщение в Telegram разбито на {Count} частей ({Total} символов)",
                    chunks.Chunks.Length,
                    message.Length
                );
            }

            var connection = await session.GetAsync(cancellationToken);

            var peer = await peerResolver.ResolveAsync(chatId, connection, cancellationToken);
            if (peer is null)
            {
                logger.LogError(
                    "Сообщение в Telegram не отправлено: не удалось определить адресат чата {ChatId}",
                    chatId
                );
                return false;
            }

            foreach (var chunk in chunks.Chunks)
            {
                var text = HtmlMessageEntities.Parse(chunk, out var entities);
                await connection.SendTextAsync(peer, text, entities);
            }

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

        // Администратор видит весь исходящий поток: если адресат не сам админ,
        // шлём ему копию. Копия идёт разметкой — разметка уже разобрана выше и
        // дойдёт до админа как есть. Отправка самому администратору копии не
        // требует: он уже получил сообщение как адресат.
        if (result && !options.Value.AdminIds.Contains(chatId))
        {
            var admins = options.Value.AdminIds;

            foreach (var adminId in admins)
            {
                try
                {
                    var copyMessage = $"[исходящее → чат {chatId}]\n{message}";
                    var copyChunks = MessageChunker.SplitHtml(copyMessage);
                    if (copyChunks.SuppressedChars > 0)
                    {
                        logger.LogWarning(
                            "Копия исходящего сообщения администратору обрезана: скрыто {Suppressed} из {Total} символов. Полный текст: {Message}",
                            copyChunks.SuppressedChars,
                            copyMessage.Length,
                            copyMessage
                        );
                    }

                    var connection = await session.GetAsync(cancellationToken);

                    var adminPeer = await peerResolver.ResolveAsync(
                        adminId,
                        connection,
                        cancellationToken
                    );
                    if (adminPeer is null)
                    {
                        logger.LogError(
                            "Копия исходящего сообщения администратору не отправлена: не удалось определить адресат чата {ChatId}",
                            adminId
                        );
                        continue;
                    }

                    foreach (var chunk in copyChunks.Chunks)
                    {
                        var copyText = HtmlMessageEntities.Parse(chunk, out var copyEntities);
                        await connection.SendTextAsync(adminPeer, copyText, copyEntities);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Копия исходящего сообщения администратору не отправлена");
                }
            }
        }

        return result;
    }
}
