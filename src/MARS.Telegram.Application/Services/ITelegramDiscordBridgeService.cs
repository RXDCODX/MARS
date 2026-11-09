using MARS.Shared.Contracts.Models;
using MARS.Telegram.Application.Models;
using MARS.Telegram.Domain.Entities;

namespace MARS.Telegram.Application.Services;

public interface ITelegramDiscordBridgeService
{
    Task<OperationResult<List<TelegramDiscordBindingDto>>> GetBindingsAsync(
        CancellationToken cancellationToken = default
    );

    Task<OperationResult<TelegramDiscordBindingDto>> AddBindingAsync(
        TelegramDiscordBindingCreateRequest request,
        CancellationToken cancellationToken = default
    );

    Task<OperationResult> DeleteBindingAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<OperationResult<TelegramDiscordBindingDto>> SetBindingEnabledAsync(
        Guid id,
        bool isEnabled,
        CancellationToken cancellationToken = default
    );

    Task<OperationResult<List<TelegramDiscordChannelStateDto>>> GetStatesAsync(
        CancellationToken cancellationToken = default
    );

    Task<OperationResult<List<TelegramChannelOptionDto>>> GetTelegramChannelsAsync(
        CancellationToken cancellationToken = default
    );

    Task<OperationResult<List<DiscordChannelOptionDto>>> GetDiscordChannelsAsync(
        CancellationToken cancellationToken = default
    );
}
