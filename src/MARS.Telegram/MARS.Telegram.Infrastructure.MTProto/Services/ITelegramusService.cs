using MARS.Telegram.Infrastructure.MTProto.BotService;

namespace MARS.Telegram.Infrastructure.MTProto.Services;

/// <summary>
/// Служба, которой нужны входящие сообщения бота.
///
/// <para>
/// Раньше контракт был шире и требовал <c>Update</c> Bot API вместе с клиентом
/// Bot API. Теперь сообщение нормализовано, а обработка объявляется одним
/// интерфейсом <see cref="ITelegramIncomingHandler"/> — так список обработчиков
/// один на всех и не зависит от того, кто именно их реализует.
/// </para>
/// </summary>
public interface ITelegramusService : ITelegramIncomingHandler;
