namespace MARS.MediaStorage.Services.Media;

/// <summary>
/// Что ffprobe рассказал о файле. Имена кодеков добавлены ради
/// <see cref="MemeMediaTranscodePolicy"/>: монолит перекодировал видео не только
/// по битрейту, но и по кодекам и переменной частоте кадров.
/// </summary>
public sealed record MediaProbeResult(
    long? BitrateKbps = null,
    double? AverageFrameRate = null,
    double? RawFrameRate = null,
    string? VideoCodecName = null,
    string? AudioCodecName = null
);
