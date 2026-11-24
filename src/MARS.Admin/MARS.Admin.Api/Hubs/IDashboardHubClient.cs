using Microsoft.AspNetCore.SignalR;

namespace MARS.Admin.Api.Hubs;

using MARS.Admin.Api.Contracts;

public interface IDashboardHubClient
{
    Task ReceiveEvent(DashboardEventDto evt);
}
