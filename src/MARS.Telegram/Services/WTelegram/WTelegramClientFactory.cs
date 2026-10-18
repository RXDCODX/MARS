using MARS.Telegram.Configuration;
using MARS.Telegram.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Telegram.Services.WTelegram;

/// <summary>
/// Сборка клиента WTelegram: учётные данные приложения, прокси и сессия в БД.
/// </summary>
public interface IWTelegramClientFactory
{
    /// <summary>
    /// Ждёт ли вход код подтверждения: клиент упёрся в него и без него не пойдёт
    /// дальше.
    /// </summary>
    /// <remarks>
    /// Признак ставится по ошибке входа, а не по факту отправки кода: присланный
    /// код может оказаться не тем, и клиент попросит новый.
    /// </remarks>
    bool AwaitingCode { get; }

    /// <summary>
    /// Сообщение последней неудачи входа — его показывает страница статуса.
    /// </summary>
    /// <remarks>
    /// Без него администратор видел бы только крутилки и не смог бы отличить
    /// «неверный код» от «прокси недоступен».
    /// </remarks>
    string? LastLoginError { get; }

    /// <summary>
    /// Адрес прокси в виде ссылки <c>tg://proxy</c> либо <c>null</c>, когда прокси
    /// не задан и клиент идёт в Telegram напрямую.
    /// </summary>
    string? ResolveProxyUrl();

    /// <summary>
    /// Код подтверждения, присланный снаружи (с веб-страницы). Уходит в клиент при
    /// следующей попытке входа и главнее значения из конфигурации.
    /// </summary>
    void SubmitLoginCode(string code);

    /// <summary>Запоминает, чем кончилась попытка входа.</summary>
    void NoteLoginFailed(Exception error);

    /// <summary>Снимает ожидание кода: вход состоялся.</summary>
    void NoteLoginSucceeded();

    /// <summary>
    /// Клиент с сессией из указанного потока, без входа в аккаунт.
    /// </summary>
    WTelegramClient Create(Stream sessionStore);

    /// <summary>Клиент, вошедший как бот по токену.</summary>
    Task<WTelegramClient> LoginAsBotAsync(string botToken, CancellationToken cancellationToken);

    /// <summary>Клиент, вошедший как пользователь по номеру телефона.</summary>
    Task<WTelegramClient> LoginAsUserAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Клиент WTelegram для бота и пользовательского аккаунта.
/// </summary>
/// <remarks>
/// <para>
/// Обмен идёт по MTProto, а не через Bot API: прямой доступ к
/// <c>api.telegram.org</c> с машины владельца закрыт, и весь трафик заворачивается
/// в локальный прокси. Тот же протокол у прокси на входе, поэтому подключение
/// выглядит как обычный MTProto и настраивается адресом прокси.
/// </para>
/// <para>
/// Сессия хранится в БД, а не в файле рядом с сервисом: контейнер пересоздаётся
/// при каждом обновлении образа, и файловая сессия заставила бы входить заново.
/// Бот и пользователь пишут разные строки.
/// </para>
/// </remarks>
public sealed class WTelegramClientFactory(
    IOptions<TelegramConfiguration> options,
    IConfiguration configuration,
    IDbContextFactory<ChatDbContext> factory,
    ILogger<WTelegramClientFactory> logger
) : IWTelegramClientFactory
{
    /// <summary>Имя сессии бота: у пользовательского аккаунта своя строка.</summary>
    public const string BotSessionName = "bot";

    /// <summary>Имя сессии пользовательского аккаунта.</summary>
    public const string UserSessionName = "user";

    /// <summary>
    /// Признаки того, что входу нужен код. Их два, и оба взяты с живого запуска:
    /// клиент без кода падает с «You must provide a config value for
    /// verification_code», а неверный код — с «400 PHONE_CODE_INVALID». Только по
    /// первому признаку форма пряталась ровно тогда, когда код был нужен снова.
    /// </summary>
    /// <remarks>
    /// Сравнение по тексту — единственное, чем клиент об этом сообщает: отдельного
    /// типа ошибки у него нет, а трактовать любую ошибку входа как «нужен код»
    /// означало бы показывать форму при недоступном прокси.
    /// </remarks>
    private static readonly string[] CodeRequiredMarkers =
    [
        VerificationCodeKey,
        "PHONE_CODE_INVALID",
        "PHONE_CODE_EXPIRED",
    ];

    private volatile string? _loginCode;
    private volatile string? _lastLoginError;

    /// <summary>Ключ, под которым секрет прокси лежит в переменных окружения.</summary>
    private const string ProxySecretEnvironmentKey = "TG_WS_PROXY_SECRET";

    /// <summary>
    /// Ключ кода подтверждения входа. Именно <c>verification_code</c>: клиент
    /// спрашивает его под этим именем и на <c>code</c> не смотрит.
    /// </summary>
    public const string VerificationCodeKey = "verification_code";

    /// <inheritdoc />
    public bool AwaitingCode => _lastLoginError is not null && NeedsCode(_lastLoginError);

    /// <inheritdoc />
    public string? LastLoginError => _lastLoginError;

    /// <inheritdoc />
    public void SubmitLoginCode(string code) => _loginCode = code;

    /// <inheritdoc />
    public void NoteLoginFailed(Exception error) => _lastLoginError = error.Message;

    /// <inheritdoc />
    public void NoteLoginSucceeded() => _lastLoginError = null;

    /// <summary>Просит ли вход код подтверждения.</summary>
    private static bool NeedsCode(string error) =>
        CodeRequiredMarkers.Any(marker => error.Contains(marker, StringComparison.Ordinal));

    /// <inheritdoc />
    public string? ResolveProxyUrl()
    {
        string? result = null;

        var telegram = options.Value;
        var secret = ResolveSecret();

        if (!string.IsNullOrWhiteSpace(telegram.ProxyServer) && telegram.ProxyPort > 0)
        {
            result = string.IsNullOrWhiteSpace(secret)
                ? $"tg://proxy?server={telegram.ProxyServer}&port={telegram.ProxyPort}"
                : $"tg://proxy?server={telegram.ProxyServer}&port={telegram.ProxyPort}&secret={secret}";
        }

        return result;
    }

    /// <inheritdoc />
    public WTelegramClient Create(Stream sessionStore)
    {
        var config = BuildConfig();
        var client = new WTelegramClient(key => config.GetValueOrDefault(key), sessionStore);
        var proxy = ResolveProxyUrl();

        if (proxy is not null)
        {
            client.MTProxyUrl = proxy;

            logger.LogInformation(
                "WTelegram: клиент собран через прокси {ProxyUrl}",
                $"{options.Value.ProxyServer}:{options.Value.ProxyPort}"
            );
        }
        else
        {
            logger.LogInformation("WTelegram: прокси не задан, клиент идёт напрямую");
        }

        return client;
    }

    /// <inheritdoc />
    public async Task<WTelegramClient> LoginAsBotAsync(
        string botToken,
        CancellationToken cancellationToken
    )
    {
        var client = Create(NewSession(BotSessionName));

        await client.LoginBotIfNeeded(botToken);

        logger.LogInformation("WTelegram: бот авторизован как @{Username}", Username(client.User));

        return client;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Результат попытки запоминается: без кода вход падает с сообщением про
    /// <c>verification_code</c>, и именно по нему веб-страница показывает форму
    /// ввода. Раньше признак ожидания всегда был ложью, и код было некуда вводить.
    /// </remarks>
    public async Task<WTelegramClient> LoginAsUserAsync(CancellationToken cancellationToken)
    {
        var client = Create(NewSession(UserSessionName));

        try
        {
            await client.LoginUserIfNeeded();
        }
        catch (Exception e)
        {
            NoteLoginFailed(e);
            throw;
        }

        NoteLoginSucceeded();

        logger.LogInformation(
            "WTelegram: пользователь авторизован как @{Username}",
            Username(client.User)
        );

        return client;
    }

    /// <summary>
    /// Конфигурация приложения в том виде, как её ждёт клиент.
    /// </summary>
    /// <remarks>
    /// Имена ключей — из библиотеки, и опечатка в них стоит дорого: ключа
    /// <c>verification_code</c> клиент не знает и падает с «You must provide a
    /// config value for verification_code» — то есть требует именно то, чего не
    /// передали, а не то, что опечаталось в коде.
    /// </remarks>
    /// <remarks>
    /// <para>
    /// Пустые необязательные значения не добавляются: клиент сам спросит
    /// недостающее. А вот <c>api_id</c> и <c>api_hash</c> обязательны и без них
    /// клиент не создаётся вовсе — причём падает он внутри библиотеки, сообщением
    /// про «config value», от которого до настройки далеко. Поэтому проверка
    /// здесь: ошибка должна называть ключ, который надо задать.
    /// </para>
    /// <para>
    /// <c>api_hash</c> обязан быть полным — 32 шестнадцатеричных символа, каким
    /// его выдаёт my.telegram.org. Ключ шифрования сессии выводится из него
    /// разбором hex, и обрезанный хеш даёт «Specified key is not a valid size for
    /// this algorithm» внутри конструктора: сообщение про размер ключа AES и про
    /// Telegram не имеет ничего общего.
    /// </para>
    /// </remarks>
    public Dictionary<string, string?> BuildConfig()
    {
        var config = new Dictionary<string, string?>();
        var telegram = options.Value;
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(telegram.ApiId))
        {
            missing.Add("Telegram__ApiId");
        }
        else
        {
            config["api_id"] = telegram.ApiId;
        }

        if (string.IsNullOrWhiteSpace(telegram.ApiHash))
        {
            missing.Add("Telegram__ApiHash");
        }
        else
        {
            config["api_hash"] = telegram.ApiHash;
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "WTelegram: не заданы учётные данные приложения "
                    + string.Join(", ", missing)
                    + " (my.telegram.org → приложение). Без них клиент не создастся."
            );
        }

        if (!string.IsNullOrWhiteSpace(telegram.PhoneNumber))
        {
            config["phone_number"] = telegram.PhoneNumber;
        }

        if (!string.IsNullOrWhiteSpace(telegram.Password))
        {
            config["password"] = telegram.Password;
        }

        // Код подтверждения нужен только при первом входе: пока сессии нет, Telegram
        // присылает код в приложение, и без него вход не состоится. Дальше клиент
        // входит сам по сохранённой сессии.
        //
        // Присланный с веб-страницы код главнее значения из конфигурации: страница
        // и есть способ прислать свежий код, и она обязана перебивать устаревший.
        var code = _loginCode ?? telegram.LoginCode;

        if (!string.IsNullOrWhiteSpace(code))
        {
            config[VerificationCodeKey] = code;
        }

        return config;
    }

    /// <summary>
    /// Секрет прокси берётся из своей настройки, а если она пустая — из переменной
    /// окружения того же контейнера прокси. Дублировать секрет в двух местах
    /// файла окружения нельзя: расходятсятся молча, и прокси перестаёт пускать.
    /// </summary>
    private string? ResolveSecret()
    {
        var secret = options.Value.ProxySecret;

        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = configuration[ProxySecretEnvironmentKey];
        }

        return string.IsNullOrWhiteSpace(secret) ? null : secret.Trim();
    }

    /// <summary>
    /// Имя пользователя для журнала: у аккаунта набор имён, а не одно поле, и
    /// пустое имя в логе выглядело бы как «авторизовался неизвестно кем».
    /// </summary>
    private static string? Username(TL.User? user)
    {
        var names = user?.ActiveUsernames;

        return names is null ? null : string.Join(", ", names);
    }

    private WTelegramDbSessionStore NewSession(string name) => new(factory, name, logger);
}
