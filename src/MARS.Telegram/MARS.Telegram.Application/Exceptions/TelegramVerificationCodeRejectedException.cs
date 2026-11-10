namespace MARS.Telegram.Application.Exceptions;

/// <summary>Telegram отклонил код подтверждения пользователя.</summary>
public sealed class TelegramVerificationCodeRejectedException(
    string message,
    Exception innerException
) : Exception(message, innerException);
