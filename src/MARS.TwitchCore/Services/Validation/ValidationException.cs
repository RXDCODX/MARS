namespace MARS.TwitchCore.Services.Validation;

public sealed class ValidationException(string message) : Exception(message);
