namespace MARS.TwitchCore.Services.Validation;

public interface IMessageValidationBuilder
{
    IMessageValidationBuilder RequireChannel(bool loud = false);
    IMessageValidationBuilder RequireBroadcasterId(bool loud = false);
    IMessageValidationBuilder SkipBlacklisted(bool loud = false);
    IMessageValidationBuilder RequireRewardId(bool loud = false);
    IMessageValidationBuilder RequireRewardGuid(Guid? expected, bool loud = false);
    IMessageValidationBuilder RequireServiceActive(bool isActive, bool loud = false);
    IMessageValidationBuilder RequireUserId(bool loud = false);
    Task<ValidationResult> ValidateAsync();
    Task<ValidationResult> ValidateWithResponseAsync(string userName);
}
