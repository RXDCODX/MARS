using MARS.TwitchCore.Data;
using MARS.TwitchCore.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MARS.TwitchCore.Services.WeddingAnniversary;

public class WeddingAnniversaryService(
    IDbContextFactory<TwitchDbContext> dbContextFactory,
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

    public virtual async Task<NearestAnniversaryDto?> GetNearestAnniversaryAsync(
        CancellationToken cancellationToken = default
    )
    {
        NearestAnniversaryDto? result = null;

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );

            var marriedUsers = await dbContext
                .Husbands.AsNoTracking()
                .Include(h => h.TwitchUser)
                .Where(h => h.IsPrivated && h.WhenPrivated != null)
                .ToListAsync(cancellationToken);

            // Дата свадьбы приходит из базы с Kind=Utc, поэтому и «сегодня» считается в
            // UTC: сравнение календарной даты в UTC с календарной датой в
            // локальном времени даёт верный ответ не всегда.
            //
            // При положительном смещении в первые часы после полуночи локальное
            // сегодня на день позже прочитанного значения, и годовщина «сегодня»
            // отсеивалась условием `anniversaryDate >= today` — сервис поздравлял
            // с годовщиной вместо сегодняшней. Тест на календарь падал четыре
            // часа в сутки в зависимости от времени запуска.
            var today = DateTime.UtcNow.Date;
            NearestAnniversaryDto? nearest = null;
            DateTime? nearestDate = null;

            foreach (var user in marriedUsers)
            {
                if (user.WhenPrivated is null)
                {
                    continue;
                }

                var weddingDate = user.WhenPrivated.Value;
                var lastMonths = user.LastWeddingCongratulatedMonths ?? -1;

                foreach (var anniversary in AnniversaryDefinitions.OrderBy(d => d.Months))
                {
                    if (anniversary.Months <= lastMonths)
                    {
                        continue;
                    }

                    var anniversaryDate = weddingDate.AddMonths(anniversary.Months);
                    if (anniversaryDate.Date >= today)
                    {
                        if (nearestDate is null || anniversaryDate < nearestDate)
                        {
                            nearestDate = anniversaryDate;
                            nearest = new NearestAnniversaryDto
                            {
                                TwitchId = user.TwitchId,
                                DisplayName = user.TwitchUser?.DisplayName ?? user.TwitchId,
                                AnniversaryName = anniversary.Name,
                                AnniversaryDate = anniversaryDate,
                                Months = anniversary.Months,
                            };
                        }

                        break;
                    }
                }
            }

            result = nearest;

            if (result != null)
            {
                logger.LogInformation(
                    "Найдена ближайшая годовщина: {User} - {Name} ({Date})",
                    result.DisplayName,
                    result.AnniversaryName,
                    result.AnniversaryDate
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка при поиске ближайшей годовщины среди всех пользователей");
        }

        return result;
    }
}
