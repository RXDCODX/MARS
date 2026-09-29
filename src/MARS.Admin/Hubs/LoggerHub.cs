using MARS.Admin.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Admin.Hubs;

public class LoggerHub() : Hub<ILoggerHub>;
