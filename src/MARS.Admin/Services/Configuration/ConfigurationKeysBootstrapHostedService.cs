using System.Collections;
using System.Reflection;
using MARS.Admin.Data;
using MARS.Admin.Entities;
using MARS.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace MARS.Admin.Services.Configuration;

public sealed class ConfigurationKeysBootstrapHostedService(
    IDbContextFactory<AdminDbContext> dbContextFactory,
    ILogger<ConfigurationKeysBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );

            await EnsureRootStateKeysAsync(dbContext, cancellationToken);
            await EnsureEnvironmentVariableKeysAsync(dbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при автоинициализации ключей конфигурации");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        var result = Task.CompletedTask;
        return result;
    }

    private static async Task EnsureRootStateKeysAsync(
        AdminDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var knownKeys = GetRootStateKeys();
        var existingKeys = await dbContext
            .RootState.AsNoTracking()
            .Select(s => s.Name)
            .ToListAsync(cancellationToken);
        var existingKeysHash = existingKeys.ToHashSet(StringComparer.Ordinal);

        var missingStates = knownKeys
            .Where(key => !existingKeysHash.Contains(key))
            .Select(CreateDefaultRootState)
            .ToList();

        if (missingStates.Count > 0)
        {
            await dbContext.RootState.AddRangeAsync(missingStates, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureEnvironmentVariableKeysAsync(
        AdminDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var existingKeys = await dbContext
            .EnvironmentVariables.AsNoTracking()
            .Select(e => e.Key)
            .ToListAsync(cancellationToken);
        var existingKeysHash = existingKeys.ToHashSet(StringComparer.Ordinal);

        var allVariables = Environment
            .GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .Select(entry => new KeyValuePair<string, string?>(
                entry.Key?.ToString() ?? string.Empty,
                entry.Value?.ToString()
            ))
            .ToList();

        // Секреты намеренно не копируются в БД: таблица читается
        // неаутентифицированным GET /api/EnvironmentVariable (блокер №6).
        var environmentVariables = SecretValueFilter.Filter(allVariables);
        var skippedSecretCount = allVariables.Count - environmentVariables.Count();
        var missingVariables = new List<EnvironmentVariable>();

        foreach (var environmentVariable in environmentVariables)
        {
            var key = environmentVariable.Key;
            var value = environmentVariable.Value;

            if (string.IsNullOrWhiteSpace(key) || existingKeysHash.Contains(key))
            {
                continue;
            }

            missingVariables.Add(
                new EnvironmentVariable
                {
                    Key = key,
                    Value = value,
                    Description = "Автосоздано из переменных окружения",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                }
            );

            existingKeysHash.Add(key);
        }

        if (skippedSecretCount > 0)
        {
            logger.LogInformation(
                "Пропущено {Count} секретных переменных окружения при автозаполнении БД",
                skippedSecretCount
            );
        }

        if (missingVariables.Count > 0)
        {
            await dbContext.EnvironmentVariables.AddRangeAsync(missingVariables, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static IReadOnlyList<string> GetRootStateKeys()
    {
        var result = typeof(RootStateKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field =>
                field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string)
            )
            .Select(field => field.GetRawConstantValue()?.ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return result;
    }

    private static RootState CreateDefaultRootState(string key)
    {
        var result = key switch
        {
            RootStateKeys.RandomMemeOnlineIsStop => new RootState
            {
                Name = key,
                Value = false.ToString(),
                Description = "Флаг остановки сервиса RandomMemeOnline",
                TypeDescription = "bool",
            },
            RootStateKeys.PuntoSwitcherFilterEnabled => new RootState
            {
                Name = key,
                Value = true.ToString(),
                Description = "Флаг включения фильтра PuntoSwitcher",
                TypeDescription = "bool",
            },
            RootStateKeys.TtsFilterEnabled => new RootState
            {
                Name = key,
                Value = true.ToString(),
                Description = "Флаг включения фильтра дубликатов TTS сообщений",
                TypeDescription = "bool",
            },
            RootStateKeys.WaifuRollCooldownMinutes => new RootState
            {
                Name = key,
                Value = 20L.ToString(),
                Description = "Кулдаун ролла вайфу в минутах",
                TypeDescription = "long",
            },
            RootStateKeys.TwitchFumoFridayNightVideoPath => new RootState
            {
                Name = key,
                Value = "wwwroot/Alerts/fumoFridayNight.webm",
                Description = "Путь до видео для Fumo Friday Night",
                TypeDescription = "string",
            },
            RootStateKeys.RandomRewardCooldownSeconds => new RootState
            {
                Name = key,
                Value = 60L.ToString(),
                Description = "Кулдаун награды RandomReward для одного пользователя в секундах",
                TypeDescription = "long",
            },
            RootStateKeys.SoundRequestProvider => new RootState
            {
                Name = key,
                Value = "YouTube",
                Description = "Активный провайдер SoundRequest (YouTube/Spotify)",
                TypeDescription = "enum: SoundRequestProvider",
            },
            _ => new RootState
            {
                Name = key,
                Value = string.Empty,
                Description = "Автосозданный ключ RootState",
                TypeDescription = "string",
            },
        };

        return result;
    }
}
