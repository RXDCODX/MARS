using MARS.Admin.Data;
using MARS.Admin.Services;
using MARS.Platform.Web.AspNetCore.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnvironmentVariableEntity = MARS.Admin.Entities.EnvironmentVariable;

namespace MARS.Admin.Controllers;

public class SetEnvironmentVariableRequest
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
}
