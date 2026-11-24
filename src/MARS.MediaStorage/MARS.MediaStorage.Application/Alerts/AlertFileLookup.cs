using MARS.Shared.Contracts.Models.Media;
using Microsoft.AspNetCore.Http;

namespace MARS.MediaStorage.Application.Alerts;

/// <summary>
/// Результат поиска файла алерта: либо готовое к отдаче описание, либо
/// человекочитаемая причина отказа.
/// </summary>
public sealed record AlertFileLookup(
    AlertFileLookupStatus Status,
    string? FullPath = null,
    string? ContentType = null,
    string? FileName = null,
    string? Message = null
);
