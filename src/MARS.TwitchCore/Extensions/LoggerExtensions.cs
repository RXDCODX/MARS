using System.Text;
using Microsoft.Extensions.Logging;

namespace MARS.TwitchCore.Extensions;

public static class LoggerExtensions
{
    extension(ILogger logger)
    {
        public void LogException(Exception exception)
        {
            Exception? innerException = exception;

            while (innerException.InnerException != null)
            {
                innerException = innerException.InnerException;
            }

            logger.LogError("{Message} # {StackTrace}", innerException.Message, exception.StackTrace);
        }
    }

    extension<T>(ILogger<T> logger)
    {
        public void LogException(Exception exception)
        {
            Exception? innerException = exception;

            var sb = new StringBuilder(exception.Message);

            while (innerException.InnerException != null)
            {
                innerException = innerException.InnerException;
                sb.Append(" + " + innerException.Message);
            }

            logger.LogError(
                "({ClassName}): {Message} # {StackTrace}",
                typeof(T).Name,
                sb.ToString(),
                exception.StackTrace
            );
        }
    }
}
