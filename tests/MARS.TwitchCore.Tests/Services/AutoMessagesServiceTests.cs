using MARS.TwitchCore.Data;
using MARS.TwitchCore.DTOs;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services.AutoMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Автоматические сообщения: набор фраз, которые бот сам публикует в чат.
///
/// Проверяется CRUD и то, что выборка отсортирована: сообщения берутся случайно,
/// но список должен быть предсказуемым, иначе повтор одного и того же текста
/// заметен зрителям.
/// </summary>
public class AutoMessagesServiceTests
{
    /// <summary>
    /// Фабрика одна на тестовый класс: у каждого экземпляра своя база в памяти,
    /// поэтому отдельная фабрика для сервиса получила бы пустую базу и проверки
    /// на запись проходили бы вхолостую.
    /// </summary>
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly AutoMessagesService _service;

    public AutoMessagesServiceTests()
    {
        _service = new(_factory, NullLogger<AutoMessagesService>.Instance);
    }

    /// <summary>
    /// Обновление отсутствующего сообщения возвращает null, а не создаёт запись
    /// из воздуха: иначе опечатка в id тихо добавила бы фразу в чат.
    /// </summary>
    [Fact]
    public async Task UpdateOfMissingMessageYieldsNull()
    {
        var updated = await _service.UpdateAutoMessageAsync(
            Guid.NewGuid(),
            new UpdateAutoMessageRequest { Message = "новое" },
            TestContext.Current.CancellationToken
        );

        Assert.Null(updated);
    }

    [Fact]
    public async Task DeletionOfMissingMessageReportsFalse()
    {
        var deleted = await _service.DeleteAutoMessageAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );

        Assert.False(deleted);
    }

    [Fact]
    public async Task MessageIsCreatedAndRead()
    {
        var created = await _service.CreateAutoMessageAsync(
            new CreateAutoMessageRequest { Message = "привет" },
            TestContext.Current.CancellationToken
        );

        var read = await _service.GetAutoMessageByIdAsync(
            created.Id,
            TestContext.Current.CancellationToken
        );

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("привет", read!.Message);
    }

    [Fact]
    public async Task MissingMessageByIdYieldsNull()
    {
        Assert.Null(
            await _service.GetAutoMessageByIdAsync(
                Guid.NewGuid(),
                TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>
    /// Пустой текст при обновлении не затирает прежний: без такой проверки один
    /// запрос с пустым телом стёр бы фразу у всех.
    /// </summary>
    [Fact]
    public async Task EmptyUpdateKeepsPreviousText()
    {
        var created = await _service.CreateAutoMessageAsync(
            new CreateAutoMessageRequest { Message = "старая фраза" },
            TestContext.Current.CancellationToken
        );

        var updated = await _service.UpdateAutoMessageAsync(
            created.Id,
            new UpdateAutoMessageRequest(),
            TestContext.Current.CancellationToken
        );

        Assert.Equal("старая фраза", updated!.Message);
    }

    [Fact]
    public async Task MessageTextIsReplacedOnUpdate()
    {
        var created = await _service.CreateAutoMessageAsync(
            new CreateAutoMessageRequest { Message = "старая фраза" },
            TestContext.Current.CancellationToken
        );

        var updated = await _service.UpdateAutoMessageAsync(
            created.Id,
            new UpdateAutoMessageRequest { Message = "новая фраза" },
            TestContext.Current.CancellationToken
        );

        Assert.Equal("новая фраза", updated!.Message);
    }

    [Fact]
    public async Task MessageIsDeleted()
    {
        var created = await _service.CreateAutoMessageAsync(
            new CreateAutoMessageRequest { Message = "прощание" },
            TestContext.Current.CancellationToken
        );

        var deleted = await _service.DeleteAutoMessageAsync(
            created.Id,
            TestContext.Current.CancellationToken
        );

        Assert.True(deleted);
        Assert.Empty(await AllAsync());
    }

    /// <summary>
    /// Список отдаётся отсортированным по тексту: сообщения выбираются случайно,
    /// и без сортировки повтор одного текста заметен зрителям.
    /// </summary>
    [Fact]
    public async Task AllMessagesAreSortedByText()
    {
        await SeedAsync("третья", "вторая", "первая");

        var messages = await AllAsync();

        Assert.Equal(["вторая", "первая", "третья"], messages.Select(m => m.Message));
    }

    [Fact]
    public async Task NoMessagesYieldsEmptyList()
    {
        Assert.Empty(await AllAsync());
    }

    private async Task<IEnumerable<AutoMessageDto>> AllAsync() =>
        await _service.GetAllAutoMessagesAsync(TestContext.Current.CancellationToken);

    private async Task SeedAsync(params string[] texts)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.AutoMessages.AddRange(texts.Select(text => new AutoMessage { Message = text }));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
