using MARS.Shared.Grpc;
using MARS.Shared.Models.Media;
using MediaAlertPriorityKind = MARS.Shared.Grpc.Media.MediaAlertPriorityKind;
using MediaTypeKind = MARS.Shared.Grpc.Media.MediaTypeKind;

namespace MARS.Shared.Tests.Grpc;

public class MediaGrpcMapperTests
{
    [Fact]
    public void ToProto_KeepsEveryFieldOfMediaAlert()
    {
        var dto = CreateMediaDto();

        var payload = MediaGrpcMapper.ToProto(dto);

        Assert.Equal(dto.MediaInfo.Id.ToString(), payload.MediaInfo.Id);
        Assert.Equal("Привет", payload.MediaInfo.TextInfo.Text);
        Assert.Equal("#ffffff", payload.MediaInfo.TextInfo.KeywordsColor);
        Assert.True(payload.MediaInfo.TextInfo.HasKeywordSymbolDelimiter);
        Assert.Equal('#', (char)payload.MediaInfo.TextInfo.KeywordSymbolDelimiter);
        Assert.Equal("Alerts/greeting.gif", payload.MediaInfo.FileInfo.FilePath);
        Assert.Equal(MediaTypeKind.Gif, payload.MediaInfo.FileInfo.Type);
        Assert.Equal(320, payload.MediaInfo.PositionInfo.XCoordinate);
        Assert.Equal("viewer", payload.MediaInfo.MetaInfo.DisplayName);
        Assert.Equal(MediaAlertPriorityKind.High, payload.MediaInfo.MetaInfo.Priority);
        Assert.True(payload.MediaInfo.StylesInfo.IsBorder);
        Assert.Equal(dto.UploadStartTime.ToString("O"), payload.UploadStartTime);
    }

    [Fact]
    public void ToProto_MarksMissingKeywordSymbolDelimiter()
    {
        var dto = CreateMediaDto();
        dto.MediaInfo.TextInfo.KeyWordSybmolDelimiter = null;

        var payload = MediaGrpcMapper.ToProto(dto);

        Assert.False(payload.MediaInfo.TextInfo.HasKeywordSymbolDelimiter);
    }

    [Fact]
    public void ToDto_RestoresMediaAlertFromProto()
    {
        var original = CreateMediaDto();

        var restored = MediaGrpcMapper.ToDto(MediaGrpcMapper.ToProto(original));

        Assert.Equal(original.UploadStartTime, restored.UploadStartTime);
        Assert.Equal(original.MediaInfo.Id, restored.MediaInfo.Id);
        Assert.Equal(original.MediaInfo.FileInfo.Type, restored.MediaInfo.FileInfo.Type);
        Assert.Equal(
            original.MediaInfo.TextInfo.KeyWordSybmolDelimiter,
            restored.MediaInfo.TextInfo.KeyWordSybmolDelimiter
        );
        Assert.Equal(original.MediaInfo.MetaInfo.Priority, restored.MediaInfo.MetaInfo.Priority);
        Assert.Equal(
            original.MediaInfo.MetaInfo.TwitchGuid,
            restored.MediaInfo.MetaInfo.TwitchGuid
        );
        Assert.Equal(
            original.MediaInfo.PositionInfo.RandomCoordinates,
            restored.MediaInfo.PositionInfo.RandomCoordinates
        );
    }

    [Fact]
    public void ToProto_MapsUnknownEnumValuesToDefaults()
    {
        var dto = CreateMediaDto();
        dto.MediaInfo.FileInfo.Type = MediaType.None;
        dto.MediaInfo.MetaInfo.Priority = MediaAlertPriority.Normal;

        var payload = MediaGrpcMapper.ToProto(dto);

        Assert.Equal(MediaTypeKind.None, payload.MediaInfo.FileInfo.Type);
        Assert.Equal(MediaAlertPriorityKind.Normal, payload.MediaInfo.MetaInfo.Priority);
    }

    private static MediaDto CreateMediaDto()
    {
        return new MediaDto(
            new MediaInfo
            {
                Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                TextInfo = new MediaTextInfo
                {
                    Text = "Привет",
                    KeyWordsColor = "#ffffff",
                    TextColor = "#000000",
                    TriggerWord = "привет",
                    KeyWordSybmolDelimiter = '#',
                },
                FileInfo = new MediaFileInfo
                {
                    Type = MediaType.Gif,
                    FilePath = "Alerts/greeting.gif",
                    FileName = "greeting.gif",
                    Extension = "gif",
                    IsLocalFile = true,
                },
                PositionInfo = new MediaPositionInfo
                {
                    XCoordinate = 320,
                    YCoordinate = 240,
                    RandomCoordinates = false,
                },
                MetaInfo = new MediaMetaInfo
                {
                    DisplayName = "viewer",
                    Duration = 7,
                    Priority = MediaAlertPriority.High,
                    TwitchGuid = Guid.Parse("99999999-8888-7777-6666-555555555555"),
                },
                StylesInfo = new MediaStylesInfo { IsBorder = true },
            }
        )
        {
            UploadStartTime = new DateTime(2026, 5, 1, 12, 30, 0, DateTimeKind.Utc),
        };
    }
}
