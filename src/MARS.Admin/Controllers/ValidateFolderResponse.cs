using MARS.Admin.Data;
using MARS.Admin.Entities;
using MARS.Admin.Services;
using MARS.Platform.Web.AspNetCore.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MARS.Admin.Controllers;

public class ValidateFolderResponse
{
    public bool Exists { get; set; }
    public bool Accessible { get; set; }
    public int VideoFilesCount { get; set; }
    public List<string> SampleFiles { get; set; } = [];
}
