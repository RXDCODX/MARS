namespace MARS.Platform.Infrastructure.Http;

/// <summary>
/// Исключение, которое бросает <see cref="AudioControllerClient"/>, если
/// MARS.AudioController недоступен или не выполнил запрос. Клиент не
/// проглатывает сбой сети, HTTP-ошибку или неуспешный конверт, чтобы
/// вызывающий код мог показать его как ошибку операции.
/// </summary>
public class AudioControllerUnavailableException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);
