using MARS.Shikimori.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShikimoriSharp;
using ShikimoriSharp.Bases;
using ShikimoriSharp.Classes;
using ShikimoriSharp.Enums;
using ShikimoriSharp.Settings;

namespace MARS.Shikimori.Services;

/// <summary>Манга в доменном виде.</summary>
public record MangaRef(long Id, string? Name, string? Russian, int? Year);
