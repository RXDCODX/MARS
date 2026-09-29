using MARS.Admin.Hubs.Models.LoggerHub;

namespace MARS.Admin.Hubs.Interfaces;

public interface ILoggerHub
{
    Task Log(LogMessageDto logMessage);
}
