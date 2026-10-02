using MARS.Shared.Models;
using MARS.Telegram.Data;
using MARS.Telegram.Entities;
using MARS.Telegram.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Связи каналов Telegram и Discord.
///
/// Это список, по которому сообщение из Telegram уходит в Discord. Проверяется,
/// что новая связь включается сразу, отключённая перестаёт работать, а удаление
/// не приводит к «успеху» для несуществующей связи — иначе администратор считал бы
/// правку выполненной, а канал продолжал бы пересылать.
/// </summary>
public class TelegramDiscordBridgeServiceTests
{
    private readonly ChatTestDbContextFactory _factory = new();
    private readonly TelegramDiscordBridgeService _service = new(
        new ChatTestDbContextFactory(),
        NullLogger<TelegramDiscordBridgeService>.Instance
    );

    public TelegramDiscordBridgeServiceTests() =>
        _service = new(_factory, NullLogger<TelegramDiscordBridgeService>.Instance);

    [Fact]
    public async Task AddedBindingIsReturned()
    {
        var result = await _service.AddBindingAsync(
            new TelegramDiscordBindingCreateRequest
            {
                TelegramChannelId = -100,
                DiscordChannelId = 42UL,
            },
            Token
        );

        Assert.True(result.Success);
        Assert.Equal(-100, result.Result!.TelegramChannelId);
        Assert.Equal(42UL, result.Result!.DiscordChannelId);
        Assert.True(result.Result!.IsEnabled);
    }

    [Fact]
    public async Task BindingsAreListed()
    {
        await AddAsync(telegramChannelId: -100, discordChannelId: 42UL);

        var result = await _service.GetBindingsAsync(Token);

        Assert.True(result.Success);
        Assert.Equal(42UL, Assert.Single(result.Result!).DiscordChannelId);
    }

    /// <summary>
    /// Отключённая связь остаётся в списке, но помечена: её можно включить обратно
    /// без пересоздания.
    /// </summary>
    [Fact]
    public async Task BindingCanBeDisabled()
    {
        var id = await AddAsync(telegramChannelId: -100, discordChannelId: 42UL);

        var disabled = await _service.SetBindingEnabledAsync(id, false, Token);

        Assert.True(disabled.Success);
        Assert.False(disabled.Result!.IsEnabled);

        var listed = await _service.GetBindingsAsync(Token);
        Assert.False(Assert.Single(listed.Result!).IsEnabled);
    }

    [Fact]
    public async Task BindingIsDeleted()
    {
        var id = await AddAsync(telegramChannelId: -100, discordChannelId: 42UL);

        var deleted = await _service.DeleteBindingAsync(id, Token);

        Assert.True(deleted.Success);
        Assert.Empty((await _service.GetBindingsAsync(Token)).Result!);
    }

    /// <summary>
    /// Удаление отсутствующей связи сообщает об отказе: администратор должен увидеть,
    /// что правка ничего не сделала.
    /// </summary>
    [Fact]
    public async Task DeletingMissingBindingFails()
    {
        var deleted = await _service.DeleteBindingAsync(Guid.CreateVersion7(), Token);

        Assert.False(deleted.Success);
    }

    /// <summary>
    /// Включение отсутствующей связи тоже отказ, а не «успех» с пустым телом.
    /// </summary>
    [Fact]
    public async Task EnablingMissingBindingFails()
    {
        var enabled = await _service.SetBindingEnabledAsync(Guid.CreateVersion7(), true, Token);

        Assert.False(enabled.Success);
        Assert.Null(enabled.Result);
    }

    /// <summary>
    /// Состояние каналов пустое до первой пересылки: фронтенд показывает пустой
    /// список, а не ошибку.
    /// </summary>
    [Fact]
    public async Task StatesAreEmptyByDefault()
    {
        var result = await _service.GetStatesAsync(Token);

        Assert.True(result.Success);
        Assert.Empty(result.Result!);
    }

    /// <summary>
    /// Список каналов Telegram пуст: их перечисляет WTelegram-клиент, а в
    /// микросервисе его нет. Пустой список — не ошибка.
    /// </summary>
    [Fact]
    public async Task ChannelListsAreEmptyWithoutClients()
    {
        Assert.True((await _service.GetTelegramChannelsAsync(Token)).Success);
        Assert.Empty((await _service.GetTelegramChannelsAsync(Token)).Result!);
        Assert.Empty((await _service.GetDiscordChannelsAsync(Token)).Result!);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<Guid> AddAsync(long telegramChannelId, ulong discordChannelId)
    {
        var added = await _service.AddBindingAsync(
            new TelegramDiscordBindingCreateRequest
            {
                TelegramChannelId = telegramChannelId,
                DiscordChannelId = discordChannelId,
            },
            Token
        );

        return added.Result!.Id;
    }
}
