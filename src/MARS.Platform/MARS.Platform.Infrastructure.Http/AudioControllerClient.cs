using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Models.Audio;
using MARS.Shared.Kernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Http;

/// <summary>
/// Реализация <see cref="IAudioControllerClient"/> через HTTP.
/// </summary>
public sealed class AudioControllerClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<AudioControllerConfiguration> configuration,
    ILogger<AudioControllerClient> logger
) : IAudioControllerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly IOptions<ServiceAuthOptions> _authOptions = authOptions;
    private readonly IOptions<AudioControllerConfiguration> _configuration = configuration;
    private readonly ILogger<AudioControllerClient> _logger = logger;

    public string ServiceEndpoint => _configuration.Value.BaseUrl;

    public Task<MuteMixerResponse> MuteAsync(
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

    public Task<UnmuteMixerResponse> UnmuteAsync(CancellationToken cancellationToken = default) =>
        PostAsync<object, UnmuteMixerResponse>("api/Mixer/unmute", new { }, cancellationToken);

    public Task<MutedSessionsResponse> GetMutedSessionsAsync(
        CancellationToken cancellationToken = default
    ) => GetAsync<MutedSessionsResponse>("api/Mixer/muted", cancellationToken);

    public Task<MicrophoneVolumeResponse> GetMicrophoneVolumeAsync(
        CancellationToken cancellationToken = default
    ) => GetAsync<MicrophoneVolumeResponse>("api/Microphone/volume", cancellationToken);

    public Task<MicrophoneVolumeSetResponse> SetMicrophoneVolumeAsync(
        float volume,
        CancellationToken cancellationToken = default
    ) =>
        PostAsync<object, MicrophoneVolumeSetResponse>(
            $"api/Microphone/volume/{volume.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            new { },
            cancellationToken
        );

    public Task<MicrophoneVolumeSetResponse> SetMicrophoneVolumeToMaxAsync(
        CancellationToken cancellationToken = default
    ) =>
        PostAsync<object, MicrophoneVolumeSetResponse>(
            "api/Microphone/volume/max",
            new { },
            cancellationToken
        );

    private async Task<T> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        ApplyApiKey(request);

        return await SendAsync<T>(request, relativeUrl, cancellationToken);
    }

    private async Task<T> PostAsync<TRequest, T>(
        string relativeUrl,
        TRequest body,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        ApplyApiKey(request);

        return await SendAsync<T>(request, relativeUrl, cancellationToken);
    }

    private async Task<T> SendAsync<T>(
        HttpRequestMessage request,
        string relativeUrl,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using (request)
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new AudioControllerUnavailableException(
                        $"AudioController вернул {(int)response.StatusCode} на {relativeUrl}"
                    );
                }

                var envelope = await response.Content.ReadFromJsonAsync<OperationResult<T>>(
                    JsonOptions,
                    cancellationToken
                );

                if (envelope is not { Success: true } || envelope.Result is null)
                {
                    throw new AudioControllerUnavailableException(
                        envelope?.ErrorMessage
                            ?? $"AudioController не выполнил запрос {relativeUrl}"
                    );
                }

                return envelope.Result;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AudioControllerUnavailableException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new AudioControllerUnavailableException(
                $"Запрос к AudioController по адресу {relativeUrl} не удался",
                exception
            );
        }
    }

    private void ApplyApiKey(HttpRequestMessage request)
    {
        var options = _authOptions.Value;

        if (options.IsEnabled)
        {
            request.Headers.TryAddWithoutValidation(options.ApiKeyHeaderName, options.ApiKey);
        }
    }
}
