using MARS.Admin.Data;
using MARS.Admin.Entities;
using MARS.Admin.Services.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Admin.Tests.Services;

/// <summary>
/// Автозаполнение ключей конфигурации при старте админки: проверяется, что
/// известные ключи RootState появляются, а секреты из окружения в БД не
/// попадают — таблица читается неаутентифицированным GET.
/// </summary>
public class ConfigurationKeysBootstrapHostedServiceTests
{
    [Fact]
    public async Task RootStateKeysAreCreatedOnFirstRun()
    {
        var factory = new AdminDbContextFactory();
        var service = new ConfigurationKeysBootstrapHostedService(
            factory,
            NullLogger<ConfigurationKeysBootstrapHostedService>.Instance
        );

        await service.StartAsync(TestContext.Current.CancellationToken);

        await using var dbContext = await factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var names = await dbContext
            .RootState.Select(state => state.Name)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Contains(names, name => name == RootStateKeys.RandomMemeOnlineIsStop);
        Assert.Contains(names, name => name == RootStateKeys.TtsFilterEnabled);
        Assert.All(names, name => Assert.False(string.IsNullOrWhiteSpace(name)));
    }

    /// <summary>
    /// Повторный запуск не должен ни дублировать ключи, ни перетирать значения,
    /// которые администратор поменял вручную.
    /// </summary>
    [Fact]
    public async Task SecondRunKeepsExistingKeysAndValues()
    {
        var factory = new AdminDbContextFactory();
        await using (
            var seed = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
        )
        {
            seed.RootState.Add(
                new RootState
                {
                    Name = "waifuRollCooldownMinutes",
                    Value = "999",
                    Description = "изменено администратором",
                    TypeDescription = "long",
                }
            );
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var service = new ConfigurationKeysBootstrapHostedService(
            factory,
            NullLogger<ConfigurationKeysBootstrapHostedService>.Instance
        );

        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StartAsync(TestContext.Current.CancellationToken);

        await using var dbContext = await factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var names = await dbContext
            .RootState.Select(state => state.Name)
            .ToListAsync(TestContext.Current.CancellationToken);
        var edited = await dbContext.RootState.SingleAsync(
            state => state.Name == "waifuRollCooldownMinutes",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("999", edited.Value);
    }

    /// <summary>
    /// Имена ключей известны заранее, и их набор должен совпадать с
    /// <see cref="Shared.Security.RootStateKeys"/>: расхождение означало бы, что
    /// ключ читается в коде, но не создаётся при старте.
    /// </summary>
    [Fact]
    public async Task EveryKnownRootStateKeyExistsAfterBootstrap()
    {
        var factory = new AdminDbContextFactory();
        var service = new ConfigurationKeysBootstrapHostedService(
            factory,
            NullLogger<ConfigurationKeysBootstrapHostedService>.Instance
        );

        await service.StartAsync(TestContext.Current.CancellationToken);

        await using var dbContext = await factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var names = await dbContext
            .RootState.Select(state => state.Name)
            .ToListAsync(TestContext.Current.CancellationToken);
        var known = typeof(RootStateKeys)
            .GetFields(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
            )
            .Where(field =>
                field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string)
            )
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

        Assert.NotEmpty(known);
        Assert.All(known, key => Assert.Contains(key, names));
    }

    [Fact]
    public async Task EnvironmentVariablesAreCopiedWithoutSecrets()
    {
        Environment.SetEnvironmentVariable("MARS_ADMIN_BOOTSTRAP_TEST_VALUE", "открытое значение");
        Environment.SetEnvironmentVariable(
            "MARS_ADMIN_BOOTSTRAP_TEST_SECRET",
            "секретное значение"
        );

        try
        {
            var factory = new AdminDbContextFactory();
            var service = new ConfigurationKeysBootstrapHostedService(
                factory,
                NullLogger<ConfigurationKeysBootstrapHostedService>.Instance
            );

            await service.StartAsync(TestContext.Current.CancellationToken);

            await using var dbContext = await factory.CreateDbContextAsync(
                TestContext.Current.CancellationToken
            );
            var variables = await dbContext.EnvironmentVariables.ToListAsync(
                TestContext.Current.CancellationToken
            );
            var plain = variables.Single(variable =>
                variable.Key == "MARS_ADMIN_BOOTSTRAP_TEST_VALUE"
            );

            Assert.Equal("открытое значение", plain.Value);
            Assert.DoesNotContain(
                variables,
                variable => variable.Key == "MARS_ADMIN_BOOTSTRAP_TEST_SECRET"
            );
        }
        finally
        {
            Environment.SetEnvironmentVariable("MARS_ADMIN_BOOTSTRAP_TEST_VALUE", null);
            Environment.SetEnvironmentVariable("MARS_ADMIN_BOOTSTRAP_TEST_SECRET", null);
        }
    }

    [Fact]
    public async Task StopIsNoOp()
    {
        var service = new ConfigurationKeysBootstrapHostedService(
            new AdminDbContextFactory(),
            NullLogger<ConfigurationKeysBootstrapHostedService>.Instance
        );

        await service.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Упавшая база не должна ронять старт сервиса: иначе админка не поднялась
    /// бы из-за необязательного автозаполнения ключей.
    /// </summary>
    [Fact]
    public async Task DatabaseFailureIsSwallowed()
    {
        var factory = new Mock<IDbContextFactory<AdminDbContext>>();
        factory
            .Setup(instance => instance.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        var logger =
            new Mock<Microsoft.Extensions.Logging.ILogger<ConfigurationKeysBootstrapHostedService>>();
        var service = new ConfigurationKeysBootstrapHostedService(factory.Object, logger.Object);

        await service.StartAsync(TestContext.Current.CancellationToken);

        logger.Verify(
            instance =>
                instance.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }
}
