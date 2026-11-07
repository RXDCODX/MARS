using MARS.Shikimori.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShikimoriSharp;
using ShikimoriSharp.Bases;
using ShikimoriSharp.Classes;
using ShikimoriSharp.Enums;
using ShikimoriSharp.Settings;

namespace MARS.Shikimori.Services;

/// <summary>
/// Персонаж в доменном виде. Названия произведений приходят строками — выбор
/// самого короткого остаётся на стороне <see cref="ShikimoriService"/>, где это
/// правило и записано.
/// </summary>
public record CharacterDetails(
    long Id,
    string Name,
    string? Russian,
    string? Description,
    string? ImagePath,
    IReadOnlyList<string?> AnimeTitles,
    IReadOnlyList<string?> MangaTitles
);
