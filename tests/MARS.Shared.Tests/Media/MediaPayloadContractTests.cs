using System.Reflection;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts.Media;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Shared.Tests.Media;

/// <summary>
/// Дрейф-детектор для пары «каноническая модель ↔ wire-контракт медиа».
/// Маппер содержит настоящие преобразования (char? → int + bool, Guid/DateTime →
/// string, enum по имени), поэтому полная генерация маппинга изменила бы форму
/// события на проводе. Здесь проверяется главное: новое поле модели не может
/// молча не попасть в контракт, а значения enum'ов не могут разойтись.
/// </summary>
public class MediaPayloadContractTests
{
    private static readonly (
        Type Source,
        Type Payload,
        Dictionary<string, string[]> Aliases
    )[] Pairs =
    [
        (
            typeof(MediaInfo),
            typeof(MediaInfoPayload),
            new(StringComparer.Ordinal) { [nameof(MediaInfo.Id)] = ["Id"] }
        ),
        (
            typeof(MediaTextInfo),
            typeof(MediaTextPayload),
            new(StringComparer.Ordinal)
            {
                [nameof(MediaTextInfo.KeyWordsColor)] = ["KeywordsColor"],
                [nameof(MediaTextInfo.KeyWordSybmolDelimiter)] =
                [
                    "KeywordSymbolDelimiter",
                    "HasKeywordSymbolDelimiter",
                ],
            }
        ),
        (typeof(MediaFileInfo), typeof(MediaFilePayload), new(StringComparer.Ordinal)),
        (typeof(MediaPositionInfo), typeof(MediaPositionPayload), new(StringComparer.Ordinal)),
        (typeof(MediaMetaInfo), typeof(MediaMetaPayload), new(StringComparer.Ordinal)),
        (typeof(MediaStylesInfo), typeof(MediaStylesPayload), new(StringComparer.Ordinal)),
    ];

    [Fact]
    public void Every_canonical_property_has_a_payload_counterpart()
    {
        foreach (var (source, payload, aliases) in Pairs)
        {
            var payloadNames = payload
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name)
                .ToHashSet(StringComparer.Ordinal);

            var canonicalNames = source
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name);

            foreach (var name in canonicalNames)
            {
                var candidates = aliases.TryGetValue(name, out var mapped) ? mapped : [name];
                Assert.True(
                    candidates.Any(payloadNames.Contains),
                    $"{source.Name}.{name} не представлен в {payload.Name}."
                );
            }
        }
    }

    [Fact]
    public void Enum_values_match_their_proto_twins()
    {
        foreach (var value in Enum.GetValues<MediaType>())
        {
            Assert.True(Enum.IsDefined(typeof(MediaTypeKind), value.ToString()));
            Assert.Equal(
                (int)value,
                (int)Enum.Parse<MediaTypeKind>(value.ToString(), ignoreCase: true)
            );
        }

        foreach (var value in Enum.GetValues<MediaAlertPriority>())
        {
            Assert.True(Enum.IsDefined(typeof(MediaAlertPriorityKind), value.ToString()));
            Assert.Equal(
                (int)value,
                (int)Enum.Parse<MediaAlertPriorityKind>(value.ToString(), ignoreCase: true)
            );
        }
    }

    [Fact]
    public void Populated_media_round_trips_through_the_wire_contract()
    {
        var id = Guid.NewGuid();
        var twitchGuid = Guid.NewGuid();
        var timestamp = new DateTime(2026, 11, 22, 10, 30, 0, DateTimeKind.Utc);
        var payload = MediaPayloadMapper.ToPayload(
            new MediaDto(
                new MediaInfo
                {
                    Id = id,
                    TextInfo = new MediaTextInfo
                    {
                        KeyWordsColor = "red",
                        TriggerWord = "nuke",
                        Text = "text",
                        TextColor = "blue",
                        KeyWordSybmolDelimiter = '#',
                    },
                    FileInfo = new MediaFileInfo
                    {
                        Type = MediaType.Video,
                        FilePath = "/alerts/video.mp4",
                        FileName = "video.mp4",
                        Extension = "mp4",
                    },
                    PositionInfo = new MediaPositionInfo { Height = 720, Width = 1280 },
                    MetaInfo = new MediaMetaInfo
                    {
                        TwitchGuid = twitchGuid,
                        DisplayName = "viewer",
                        Priority = MediaAlertPriority.High,
                    },
                    StylesInfo = new MediaStylesInfo { IsBorder = true },
                }
            )
            {
                UploadStartTime = timestamp,
            }
        );

        var restored = MediaPayloadMapper.ToDto(payload);

        Assert.Equal(id, restored.MediaInfo.Id);
        Assert.Equal('#', restored.MediaInfo.TextInfo.KeyWordSybmolDelimiter);
        Assert.Equal(MediaType.Video, restored.MediaInfo.FileInfo.Type);
        Assert.Equal(twitchGuid, restored.MediaInfo.MetaInfo.TwitchGuid);
        Assert.Equal(MediaAlertPriority.High, restored.MediaInfo.MetaInfo.Priority);
        Assert.Equal(timestamp, restored.UploadStartTime);
    }
}
