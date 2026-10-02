using MARS.MediaStorage.Extensions;
using Microsoft.Extensions.Logging;
using Moq;

namespace MARS.MediaStorage.Tests.Extensions;

/// <summary>
/// Запись исключения в журнал.
///
/// Сервисы хранилища зовут это на каждой ошибке, и проверяется главное: в журнал
/// попадает сообщение корневой причины, а не внешней обёртки. Без разбора вложенных
/// исключений в логах оставалось «The operation failed» вместо настоящей причины.
/// </summary>
public class LoggerExtensionsTests
{
    [Fact]
    public void RootCauseMessageIsLogged()
    {
        var logger = new Mock<ILogger>();
        var exception = new InvalidOperationException(
            "внешняя ошибка",
            new ArgumentNullException("inner")
        );

        logger.Object.LogException(exception);

        logger.Verify(
            instance =>
                instance.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Обобщённая версия добавляет имя класса: в общем журнале без него непонятно,
    /// кто упал.
    /// </summary>
    [Fact]
    public void GenericVersionMentionsTheClass()
    {
        var logger = new Mock<ILogger<SampleService>>();

        logger.Object.LogException(new InvalidOperationException("ошибка"));

        logger.Verify(
            instance =>
                instance.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Цепочка из трёх исключений не должна обрываться на первом звене.
    /// </summary>
    [Fact]
    public void DeepChainIsWalkedToTheEnd()
    {
        var logger = new Mock<ILogger>();
        var exception = new InvalidOperationException(
            "первое",
            new ArgumentException("второе", new FormatException("третье"))
        );

        logger.Object.LogException(exception);

        logger.Verify(
            instance =>
                instance.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Тип для обобщённого журнала: имя класса попадает в текст ошибки.
    /// </summary>
    public sealed class SampleService;
}
