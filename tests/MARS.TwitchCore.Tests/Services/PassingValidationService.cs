using MARS.TwitchCore.Services.Validation;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.EventSub.Core.EventArgs.Channel;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Валидация, которая всегда пропускает.
///
/// Настоящий построитель проверок пишет в чат и ходит в Twitch; в тестах
/// менеджеров интересует их собственное решение, а не текст ошибки.
/// Отдельная заглушка вместо Mock по интерфейсу сделана потому, что цепочка
/// вызовов длинная (ForRedemption → RequireServiceActive → ValidateWithResponse)
/// и на Moq её пришлось бы повторять в каждом тесте.
/// </summary>
internal sealed class PassingValidationService : ITwitchEventValidationService
{
    public static PassingValidationService Instance { get; } = new();

    /// <summary>Проверка, которая отвергает событие с указанной причиной.</summary>
    public string? RejectReason { get; init; }

    public IMessageValidationBuilder ForMessageReceived(OnMessageReceivedArgs args) =>
        new PassingMessageValidationBuilder(RejectReason);

    public IRedemptionValidationBuilder ForRedemption(
        ChannelPointsCustomRewardRedemptionArgs args
    ) => new PassingRedemptionValidationBuilder(RejectReason);

    public static PassingValidationService Rejecting(string reason) =>
        new() { RejectReason = reason };

    public static OnMessageReceivedArgs Message(string message) =>
        new(
            new ChatMessage(
                botUsername: "mars-bot",
                userId: "123456789",
                userName: "pyro",
                displayName: "Pyro",
                hexColor: "#FFFFFF",
                emoteSet: null!,
                message: message,
                userType: UserType.Viewer,
                channel: "канал",
                id: "message-1",
                subscribedMonthCount: 0,
                roomId: "room",
                isMe: false,
                isBroadcaster: false,
                noisy: default(Noisy),
                rawIrcMessage: string.Empty,
                emoteReplacedMessage: string.Empty,
                badges: [],
                cheerBadge: null!,
                bits: 0,
                bitsInDollars: 0,
                userDetail: default
            )
        );

    private sealed class PassingRedemptionValidationBuilder(string? rejectReason)
        : IRedemptionValidationBuilder
    {
        public IRedemptionValidationBuilder RequireBroadcasterUserId(bool loud = false) => this;

        public IRedemptionValidationBuilder RequireBroadcasterUserLogin(bool loud = false) => this;

        public IRedemptionValidationBuilder RequireCost(int cost, bool loud = false) => this;

        public IRedemptionValidationBuilder RequireServiceActive(
            bool isActive,
            bool loud = false
        ) => this;

        public IRedemptionValidationBuilder RequireRewardEnabled(
            Func<bool> isEnabled,
            bool loud = false
        ) => this;

        public IRedemptionValidationBuilder RequireRewardGuid(Guid? expected, bool loud = false) =>
            this;

        public Task<ValidationResult> ValidateAsync() => Task.FromResult(Result(rejectReason));

        public Task<ValidationResult> ValidateWithResponseAsync(string userName) =>
            Task.FromResult(Result(rejectReason));

        private static ValidationResult Result(string? rejectReason)
        {
            var result = new ValidationResult();

            if (rejectReason is not null)
            {
                result.AddError(rejectReason);
            }

            return result;
        }
    }

    private sealed class PassingMessageValidationBuilder(string? rejectReason)
        : IMessageValidationBuilder
    {
        public IMessageValidationBuilder RequireChannel(bool loud = false) => this;

        public IMessageValidationBuilder RequireBroadcasterId(bool loud = false) => this;

        public IMessageValidationBuilder SkipBlacklisted(bool loud = false) => this;

        public IMessageValidationBuilder RequireRewardId(bool loud = false) => this;

        public IMessageValidationBuilder RequireRewardGuid(Guid? expected, bool loud = false) =>
            this;

        public IMessageValidationBuilder RequireServiceActive(bool isActive, bool loud = false) =>
            this;

        public IMessageValidationBuilder RequireUserId(bool loud = false) => this;

        public Task<ValidationResult> ValidateAsync() => Task.FromResult(Result(rejectReason));

        public Task<ValidationResult> ValidateWithResponseAsync(string userName) =>
            Task.FromResult(Result(rejectReason));

        private static ValidationResult Result(string? rejectReason)
        {
            var result = new ValidationResult();

            if (rejectReason is not null)
            {
                result.AddError(rejectReason);
            }

            return result;
        }
    }
}
