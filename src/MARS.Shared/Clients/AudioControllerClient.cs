using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Configuration;
using MARS.Shared.Models.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Clients;

/// <summary>
/// Реализация <see cref="IAudioControllerClient"/> через HTTP.
/// </summary>
public class AudioControllerClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<AudioControllerConfiguration> configuration,
    ILogger<AudioControllerClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), IAudioControllerClient
{
    public override string ServiceEndpoint => configuration.Value.BaseUrl;

    public Task<MuteMixerResponse?> MuteAsync(
        IReadOnlyList<string>? excludedProcessNames = null,
        CancellationToken cancellationToken = default
    )
    {
        var request = new MuteMixerRequest { ExcludedProcessNames = excludedProcessNames };

        return PostAsync<MuteMixerRequest, MuteMixerResponse>(
            "api/Mixer/mute",
            request,
            cancellationToken
        );
    }

    public Task<UnmuteMixerResponse?> UnmuteAsync(CancellationToken cancellationToken = default)
    {
        return PostAsync<EmptyRequest, UnmuteMixerResponse>(
            "api/Mixer/unmute",
            new EmptyRequest(),
            cancellationToken
        );
    }

    public Task<MutedSessionsResponse?> GetMutedSessionsAsync(
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<MutedSessionsResponse>("api/Mixer/muted", cancellationToken);
    }

    public Task<MicrophoneVolumeResponse?> GetMicrophoneVolumeAsync(
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<MicrophoneVolumeResponse>("api/Microphone/volume", cancellationToken);
    }

    public Task<MicrophoneVolumeSetResponse?> SetMicrophoneVolumeAsync(
        float volume,
        CancellationToken cancellationToken = default
    )
    {
        return PostAsync<EmptyRequest, MicrophoneVolumeSetResponse>(
            "api/Microphone/volume/" + volume.ToString(CultureInfo.InvariantCulture),
            new EmptyRequest(),
            cancellationToken
        );
    }

    public Task<MicrophoneVolumeSetResponse?> SetMicrophoneVolumeToMaxAsync(
        CancellationToken cancellationToken = default
    )
    {
        return PostAsync<EmptyRequest, MicrophoneVolumeSetResponse>(
            "api/Microphone/volume/max",
            new EmptyRequest(),
            cancellationToken
        );
    }

    private sealed class EmptyRequest;
}
