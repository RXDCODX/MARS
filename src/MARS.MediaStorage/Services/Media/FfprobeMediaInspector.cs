using FFMpegCore;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Services.Media;

public class FfprobeMediaInspector(ILogger<FfprobeMediaInspector> logger) : IMediaInspector
{
    public async Task<MediaProbeResult> ProbeAsync(
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        var result = new MediaProbeResult();

        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            try
            {
                var analysis = await FFProbe.AnalyseAsync(
                    filePath,
                    cancellationToken: cancellationToken
                );
                var primaryVideoStream = analysis.PrimaryVideoStream;
                var primaryAudioStream = analysis.PrimaryAudioStream;

                if (primaryVideoStream is not null)
                {
                    result = new MediaProbeResult(
                        BitrateKbps: primaryVideoStream.BitRate > 0
                            ? primaryVideoStream.BitRate / 1000
                            : null,
                        AverageFrameRate: primaryVideoStream.AverageFrameRate is > 0
                            ? primaryVideoStream.AverageFrameRate
                            : null,
                        RawFrameRate: primaryVideoStream.FrameRate is > 0
                            ? primaryVideoStream.FrameRate
                            : null,
                        VideoCodecName: NormalizeCodecName(primaryVideoStream.CodecName),
                        AudioCodecName: NormalizeCodecName(primaryAudioStream?.CodecName)
                    );
                }
                else if (primaryAudioStream is not null)
                {
                    result = new MediaProbeResult(
                        BitrateKbps: primaryAudioStream.BitRate > 0
                            ? primaryAudioStream.BitRate / 1000
                            : null,
                        AudioCodecName: NormalizeCodecName(primaryAudioStream.CodecName)
                    );
                }
                else if (analysis.Format.BitRate > 0)
                {
                    result = new MediaProbeResult(
                        BitrateKbps: (long)Math.Round(analysis.Format.BitRate / 1000d)
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "ffprobe analysis failed for file {FilePath}", filePath);
            }
        }

        return result;
    }

    private static string? NormalizeCodecName(string? codecName)
    {
        return string.IsNullOrWhiteSpace(codecName) ? null : codecName.Trim().ToLowerInvariant();
    }
}
