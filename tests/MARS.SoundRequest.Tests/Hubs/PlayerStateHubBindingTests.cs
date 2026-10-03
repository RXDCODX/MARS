using System.Text.Json;
using System.Text.Json.Serialization;
using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Grpc;
using MARS.SoundRequest.Hubs.Dtos;
using Xunit;

namespace MARS.SoundRequest.Tests.Hubs;

/// <summary>
/// Разбор состояния плеера, пришедшего от браузера.
/// </summary>
/// <remarks>
/// <para>
/// Тест скормит настоящий JSON, который шлёт клиент, а не собственный DTO. Пока
/// DTO объявлял перечисления на два и на одно значение меньше, разбор падал с
/// <c>JsonException</c> на <c>"SwitchingTrack"</c>, <c>"WaitingForTrack"</c>,
/// <c>"NoVideo"</c> и <c>"AudioOnly"</c>.
/// </para>
/// <para>
/// Эти значения приходят не от рук: <c>syncPlaybackState</c> берёт состояние из
/// хаба и шлёт его же обратно, а <c>StateManager</c> ставит
/// <c>WaitingForTrack</c> при каждом переключении трека. То есть после первого
/// же трека mute, громкость, play и переключение видео падали все разом, пока
/// страница не перезагрузится.
/// </para>
/// <para>
/// Отдельная проверка идёт в обратную сторону: значения обязаны доезжать до
/// домена без схлопывания, иначе gRPC-путь превращал <c>SwitchingTrack</c> в
/// <c>Stopped</c>.
/// </para>
/// </remarks>
public class PlayerStateHubBindingTests
{
    /// <summary>Настройки, которыми хаб пишет на провод.</summary>
    private static readonly JsonSerializerOptions WireFormat = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Настоящий payload, который уходит из useSoundRequestPlayer.</summary>
    private static string ClientJson(string state, string videoState) =>
        $$"""
            {
              "id": "11111111-2222-3333-4444-555555555555",
              "state": "{{state}}",
              "videoState": "{{videoState}}",
              "isMuted": false,
              "pausedByMute": false,
              "volume": 80,
              "currentTrackProgress": "00:01:30"
            }
            """;

    [Theory]
    [InlineData("Stopped")]
    [InlineData("Playing")]
    [InlineData("Paused")]
    [InlineData("SwitchingTrack")]
    [InlineData("WaitingForTrack")]
    public void Every_client_playback_state_is_accepted(string state)
    {
        var parsed = JsonSerializer.Deserialize<PlayerStateHubDto>(
            ClientJson(state, "Video"),
            WireFormat
        );

        Assert.NotNull(parsed);
        Assert.Equal(state, parsed!.State.ToString());
    }

    [Theory]
    [InlineData("Video")]
    [InlineData("NoVideo")]
    [InlineData("AudioOnly")]
    public void Every_client_video_state_is_accepted(string videoState)
    {
        var parsed = JsonSerializer.Deserialize<PlayerStateHubDto>(
            ClientJson("Playing", videoState),
            WireFormat
        );

        Assert.NotNull(parsed);
        Assert.Equal(videoState, parsed!.VideoState.ToString());
    }

    /// <summary>Имена полей едут в camelCase — как их отдаёт клиент.</summary>
    [Fact]
    public void Property_names_match_client_payload()
    {
        var parsed = JsonSerializer.Deserialize<PlayerStateHubDto>(
            ClientJson("Playing", "NoVideo"),
            WireFormat
        );

        Assert.NotNull(parsed);
        Assert.False(parsed!.IsMuted);
        Assert.False(parsed.PausedByMute);
        Assert.Equal(80, parsed.Volume);
        Assert.Equal("00:01:30", parsed.CurrentTrackProgress);
    }

    [Theory]
    [InlineData(SoundRequestPlaybackState.SwitchingTrack, PlayerStateStateEnum.SwitchingTrack)]
    [InlineData(SoundRequestPlaybackState.WaitingForTrack, PlayerStateStateEnum.WaitingForTrack)]
    [InlineData(SoundRequestPlaybackState.Stopped, PlayerStateStateEnum.Stopped)]
    [InlineData(SoundRequestPlaybackState.Playing, PlayerStateStateEnum.Playing)]
    [InlineData(SoundRequestPlaybackState.Paused, PlayerStateStateEnum.Paused)]
    public void Playback_state_round_trips(
        SoundRequestPlaybackState proto,
        PlayerStateStateEnum expected
    ) => Assert.Equal(expected, SoundRequestHubMapper.ToHubState(proto));

    [Theory]
    [InlineData(SoundRequestVideoDisplay.Video, PlayerStateVideoStateEnum.Video)]
    [InlineData(SoundRequestVideoDisplay.NoVideo, PlayerStateVideoStateEnum.NoVideo)]
    [InlineData(SoundRequestVideoDisplay.AudioOnly, PlayerStateVideoStateEnum.AudioOnly)]
    public void Video_state_round_trips(
        SoundRequestVideoDisplay proto,
        PlayerStateVideoStateEnum expected
    ) => Assert.Equal(expected, SoundRequestHubMapper.ToHubState(proto));

    /// <summary>
    /// Прогресс доезжает до клиента в том же формате, в каком клиент его шлёт.
    /// </summary>
    /// <remarks>
    /// В proto прогресс в целых секундах, а клиент читает строку <c>hh:mm:ss</c>.
    /// Без разбора TimeSpan на часы компонент получал <c>"00:00:00"</c> и
    /// начинал видео с нуля вместо сохранённого места.
    /// </remarks>
    [Fact]
    public void Progress_is_rendered_as_client_format()
    {
        var hub = SoundRequestHubMapper.ToHubState(
            new PlayerStateSnapshot
            {
                HasCurrentTrackProgress = true,
                CurrentTrackProgressSeconds = 90,
            }
        );

        // Форма закреплена строкой, а не вычислением той же стороны: сравнение
        // «ожидаемое ToString() против фактического ToString()» прошло бы, даже
        // если бы mapper отдавал TimeSpan или число.
        Assert.Equal("00:01:30", hub.CurrentTrackProgress);
    }

    /// <summary>
    /// Отсутствие прогресса остаётся отсутствием, а не нулевым отрезком.
    /// </summary>
    /// <remarks>
    /// Разница видна в поведении: «прогресса нет» и «прогресс нулевой» — разные
    /// состояния, и подставлять одно вместо другого нельзя.
    /// </remarks>
    [Fact]
    public void Missing_progress_stays_null()
    {
        var hub = SoundRequestHubMapper.ToHubState(
            new PlayerStateSnapshot { HasCurrentTrackProgress = false }
        );

        Assert.Null(hub.CurrentTrackProgress);
    }
}
