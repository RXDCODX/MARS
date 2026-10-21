using MARS.Shared.Logging.Telegram;
using Microsoft.Extensions.Configuration;

namespace MARS.Shared.Tests.Logging;

/// <summary>
/// Настройки доставки журнала в Telegram читаются из секции <c>Telegram</c>.
///
/// Мусор в значении не должен ронять сервис: доставка журнала — дополнительный
/// канал, а не условие работы. Опечатка в конфигурации обязана приводить к
/// значению по умолчанию, а не к исключению на старте.
/// </summary>
public class TelegramLogOptionsTests
{
    /// <summary>
    /// Без настроек доставка включена, но порог — <c>Error</c>: предупреждения
    /// идут десятками, а чат для них не место.
    /// </summary>
    [Fact]
    public void DefaultsAreEnabledAndErrorLevel()
    {
        var options = TelegramLogOptions.FromConfiguration(Configuration());

        Assert.True(options.Enabled);
        Assert.Equal("Error", options.MinimumLevel);
        Assert.Equal(TelegramLogOptions.DefaultQueueCapacity, options.QueueCapacity);
        Assert.Equal(TelegramLogOptions.DefaultMaxAttempts, options.MaxAttempts);
    }

    [Fact]
    public void ValuesAreReadFromTelegramSection()
    {
        var options = TelegramLogOptions.FromConfiguration(
            Configuration(
                ("Telegram:LogEnabled", "false"),
                ("Telegram:LogMinimumLevel", "Warning"),
                ("Telegram:LogQueueCapacity", "10"),
                ("Telegram:LogMaxAttempts", "7")
            )
        );

        Assert.False(options.Enabled);
        Assert.Equal("Warning", options.MinimumLevel);
        Assert.Equal(10, options.QueueCapacity);
        Assert.Equal(7, options.MaxAttempts);
    }

    /// <summary>
    /// Неразобранный уровень не должен превращаться в «доставлять всё» или в
    /// «доставлять ничего»: остаётся значение по умолчанию.
    /// </summary>
    [Fact]
    public void UnparsableValuesFallBackToDefaults()
    {
        var options = TelegramLogOptions.FromConfiguration(
            Configuration(
                ("Telegram:LogEnabled", "да"),
                ("Telegram:LogQueueCapacity", "много"),
                ("Telegram:LogMaxAttempts", "")
            )
        );

        Assert.True(options.Enabled);
        Assert.Equal(TelegramLogOptions.DefaultQueueCapacity, options.QueueCapacity);
        Assert.Equal(TelegramLogOptions.DefaultMaxAttempts, options.MaxAttempts);
    }

    /// <summary>
    /// Ёмкость очереди и число попыток не могут быть нулевыми: нулевая ёмкость
    /// отбрасывала бы каждую запись, а нулевые попытки — не отправляли бы ни
    /// одной.
    /// </summary>
    [Fact]
    public void NonPositiveQueueAndAttemptsAreRaisedToOne()
    {
        var options = TelegramLogOptions.FromConfiguration(
            Configuration(("Telegram:LogQueueCapacity", "0"), ("Telegram:LogMaxAttempts", "-3"))
        );

        Assert.Equal(1, options.QueueCapacity);
        Assert.Equal(1, options.MaxAttempts);
    }

    private static IConfiguration Configuration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                values.Select(value => new KeyValuePair<string, string?>(value.Key, value.Value))
            )
            .Build();
}
