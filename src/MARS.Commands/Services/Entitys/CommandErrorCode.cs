namespace MARS.Commands.Services.Entitys;

/// <summary>
/// Причина неудачи команды. Раньше различать причины было нечем: «нет такой
/// команды», «мало аргументов» и «нет прав» возвращались одной строкой, и
/// вызывающий не получал ответа, кроме текста.
///
/// На gRPC эти коды отображаются в статус вызова: <c>NotFound</c>,
/// <c>InvalidArgument</c>, <c>PermissionDenied</c>, <c>Unavailable</c>.
/// </summary>
public enum CommandErrorCode
{
    None,

    /// <summary>Команда не найдена в реестре.</summary>
    UnknownCommand,

    /// <summary>Команда объявлена, но не работает на этой платформе.</summary>
    NotAvailableOnPlatform,

    /// <summary>Не хватает обязательных параметров либо они не разбираются.</summary>
    BadArguments,

    /// <summary>Команда закрыта правами администратора.</summary>
    NotAllowed,

    /// <summary>Команда объявлена, но ещё не реализована.</summary>
    NotImplemented,

    /// <summary>Сервис-цель недоступен.</summary>
    TargetUnreachable,

    /// <summary>Выполнение упало с ошибкой.</summary>
    Failed,

    /// <summary>Асинхронная команда принята, результат придёт событием.</summary>
    Accepted,
}
