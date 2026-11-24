using Microsoft.AspNetCore.SignalR;

namespace MARS.Admin.Api.Hubs;

using MARS.Admin.Api.Contracts;

public class DashboardHub : Hub<IDashboardHubClient> { }
