using System.Diagnostics.CodeAnalysis;
﻿namespace MARS.MediaStorage.Application.Downloads;

public static class MediaDownloadPolicy
{
    public const long TargetBytes = 19_000_000;

    public static readonly int[] VideoProfiles = [1080, 720, 540, 480];

    public const int MinimumVideoBitrateKbps = 250;

    public static readonly int[] AudioBitratesKbps = [320, 256, 192, 128, 96, 64];

    public const int ArtifactCacheTtlMinutes = 15;

    public const int MaxTransientRetries = 1;
}
