using MARS.Admin.Data;
using MARS.Admin.Entities;
using MARS.Admin.Services;
using MARS.Platform.Web.AspNetCore.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MARS.Admin.Controllers;

public class ValidateFolderRequest
{
    public string FolderPath { get; set; } = null!;
}
