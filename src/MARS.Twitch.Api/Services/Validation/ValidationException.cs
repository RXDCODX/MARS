namespace MARS.Twitch.Api.Services.Validation;

public sealed class ValidationException(string message) : Exception(message);
