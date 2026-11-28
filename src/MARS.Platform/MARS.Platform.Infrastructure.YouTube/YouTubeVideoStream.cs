using System.Diagnostics.CodeAnalysis;
namespace MARS.Platform.Infrastructure.YouTube;

public sealed record YouTubeVideoStream(
    string Container,
    int Width,
    int Height,
    long BitsPerSecond
);