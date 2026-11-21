namespace MARS.Twitch.Infrastructure.Services.Validation;

public sealed class ValidationException(string message) : Exception(message);
