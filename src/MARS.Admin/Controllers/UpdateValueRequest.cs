using MARS.Admin.Data;
using MARS.Admin.Entities;
using MARS.Admin.Services;
using MARS.Platform.Web.AspNetCore.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MARS.Admin.Controllers;

public record UpdateValueRequest
{
    public required string Value { get; init; }
}
