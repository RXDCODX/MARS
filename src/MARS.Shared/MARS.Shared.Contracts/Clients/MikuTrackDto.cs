using System;

namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// DTO трека Miku Monday, возвращаемый сервисом <c>MARS.WaifuGacha</c>.
/// </summary>
public sealed record MikuTrackDto(
    Guid Id,
    int Number,
    string Artist,
    string Title,
    string Url,
    string? ThumbnailUrl
);
