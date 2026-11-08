using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Controllers;

public record AutoHelloRequest
{
    public string DisplayName { get; init; } = string.Empty;
}
