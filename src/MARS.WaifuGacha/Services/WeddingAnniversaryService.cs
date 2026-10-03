using System.Globalization;
using System.Text.RegularExpressions;
using MARS.WaifuGacha.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

/// <summary>
/// Порядковый список годовщин свадьбы. Портирован из монолита
/// (MARS.Server/Services/Twitch/WeddingAnniversary) — WaifuGacha владеет
/// супругами, поэтому и годовщины считаются здесь.
/// </summary>
public class WeddingAnniversaryService(
    IDbContextFactory<WaifuDbContext> dbContextFactory,
    ILogger<WeddingAnniversaryService> logger
)
{
    private static readonly (int Months, string Name)[] AnniversaryDefinitions =
    [
        (0, "Зелёная свадьба"),
        (12, "Ситцевая свадьба"),
        (24, "Бумажная свадьба"),
        (36, "Кожаная свадьба"),
        (48, "Льняная свадьба"),
        (60, "Деревянная свадьба"),
        (72, "Чугунная свадьба"),
        (78, "Цинковая свадьба"),
        (84, "Медная свадьба"),
        (96, "Жестяная свадьба"),
        (108, "Фаянсовая свадьба"),
        (120, "Оловянная свадьба (розовая)"),
        (132, "Стальная свадьба"),
        (150, "Никелевая свадьба"),
        (156, "Ландышевая (кружевная) свадьба"),
        (168, "Агатовая свадьба"),
        (180, "Стеклянная (хрустальная) свадьба"),
        (216, "Бирюзовая свадьба"),
        (240, "Фарфоровая свадьба"),
        (252, "Опаловая свадьба"),
        (264, "Бронзовая свадьба"),
        (276, "Берилловая свадьба"),
        (288, "Атласная свадьба"),
        (300, "Серебряная свадьба"),
        (360, "Жемчужная свадьба"),
        (420, "Коралловая свадьба"),
        (450, "Алюминиевая свадьба"),
        (456, "Ртутная свадьба"),
        (480, "Рубиновая свадьба"),
        (540, "Сапфировая свадьба"),
        (600, "Золотая свадьба"),
        (660, "Изумрудная свадьба"),
        (720, "Бриллиантовая свадьба"),
        (780, "Железная свадьба"),
        (810, "Кремниевая свадьба"),
        (840, "Благодатная свадьба"),
        (900, "Коронная свадьба"),
        (960, "Дубовая свадьба"),
        (1080, "Гранитная свадьба"),
        (1200, "Платиновая (красная) свадьба"),
    ];

    /// <summary>
    /// Следующая неотправленная годовщина пользователя, если её дата уже наступила.
    /// Сразу помечает её как отправленную, чтобы повторный вызов не продублировал поздравление.
    /// </summary>
    public virtual async Task<(int Months, string Name)?> GetNextUnsentAnniversaryAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        (int Months, string Name)? result = null;

        if (!string.IsNullOrWhiteSpace(twitchId))
        {
            try
            {
                await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                    cancellationToken
                );

                var host = await dbContext
                    .Husbands.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.TwitchId == twitchId, cancellationToken);

                if (host is { IsPrivated: true, WhenPrivated: not null })
                {
                    // Дата свадьбы приходит из базы с Kind=Utc, поэтому и «сегодня» считается в
                    // UTC. Сравнение календарной даты в UTC с локальной даёт верный
                    // ответ не всегда: при положительном смещении в первые часы после
                    // полуночи сегодняшняя годовщина отсеивалась, и сервис поздравлял
                    // с годовой. Тот же разбор в TwitchCore починен так же.
                    var today = DateTime.UtcNow.Date;
                    var lastMonths = host.LastWeddingCongratulatedMonths ?? -1;

                    foreach (var anniversary in AnniversaryDefinitions)
                    {
                        if (anniversary.Months <= lastMonths)
                        {
                            continue;
                        }

                        if (host.WhenPrivated.Value.AddMonths(anniversary.Months).Date <= today)
                        {
                            result = anniversary;
                            break;
                        }
                    }

                    if (result.HasValue)
                    {
                        await MarkAnniversaryAsSentAsync(
                            twitchId,
                            result.Value.Months,
                            cancellationToken
                        );
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Ошибка при поиске непосланной годовщины для пользователя {UserId}",
                    twitchId
                );
            }
        }

        return result;
    }

    /// <summary>
    /// Фиксирует, что годовщина заданной длительности уже поздравлена.
    /// </summary>
    public virtual async Task MarkAnniversaryAsSentAsync(
        string twitchId,
        int months,
        CancellationToken cancellationToken = default
    )
    {
        if (!string.IsNullOrWhiteSpace(twitchId))
        {
            try
            {
                await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                    cancellationToken
                );

                var affected = await dbContext
                    .Husbands.Where(e => e.TwitchId == twitchId)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(e => e.LastWeddingCongratulatedMonths, months),
                        cancellationToken
                    );

                if (affected > 0)
                {
                    logger.LogInformation(
                        "Отмечена годовщина {Months} месяцев для пользователя {UserId}",
                        months,
                        twitchId
                    );
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Ошибка при отметке годовщины для пользователя {UserId}",
                    twitchId
                );
            }
        }
    }

    public static string BuildCongratulationMessageFromSpouse(
        string displayName,
        string spouseName,
        (int Months, string Name) anniversary
    )
    {
        var yearsText = FormatYears(anniversary.Months);
        var declined = DeclineAnniversaryName(anniversary.Name);
        string result;

        if (anniversary.Months == 0)
        {
            result =
                $"@{displayName}, твой супруг, {spouseName}, поздравляет тебя с {declined}! Совет да любовь!";
        }
        else
        {
            result =
                $"@{displayName}, твой супруг, {spouseName}, поздравляет тебя с {declined} ({yearsText} лет)! Совет да любовь!";
        }

        return result;
    }

    private static string DeclineAnniversaryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var result = Regex.Replace(name, "\\bсвадьба\\b", "свадьбой", RegexOptions.IgnoreCase);

        result = Regex.Replace(
            result,
            "([А-Яа-яЁё]+)ная\\s+свадьбой",
            "$1ной свадьбой",
            RegexOptions.IgnoreCase
        );
        result = Regex.Replace(
            result,
            "([А-Яа-яЁё]+)ая\\s+свадьбой",
            "$1ой свадьбой",
            RegexOptions.IgnoreCase
        );
        result = Regex.Replace(
            result,
            "([А-Яа-яЁё]+)ная(?=\\s*\\()",
            "$1ной",
            RegexOptions.IgnoreCase
        );
        result = Regex.Replace(
            result,
            "([А-Яа-яЁё]+)ая(?=\\s*\\()",
            "$1ой",
            RegexOptions.IgnoreCase
        );
        result = Regex.Replace(
            result,
            "\\(\\s*([А-Яа-яЁё]+)ная\\b",
            "($1ной",
            RegexOptions.IgnoreCase
        );
        result = Regex.Replace(
            result,
            "\\(\\s*([А-Яа-яЁё]+)ая\\b",
            "($1ой",
            RegexOptions.IgnoreCase
        );

        return result;
    }

    private static string FormatYears(int months)
    {
        var years = months / 12m;
        string result;

        if (years % 1 == 0)
        {
            result = ((int)years).ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            result = years.ToString("0.#", CultureInfo.GetCultureInfo("ru-RU"));
        }

        return result;
    }
}
