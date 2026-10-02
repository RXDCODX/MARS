using MARS.Scoreboard.Entities;
using MARS.Scoreboard.Grpc;

namespace MARS.Scoreboard.Tests;

/// <summary>
/// Преобразование табло в gRPC-сообщение и обратно.
///
/// Оверлей на OBS получает снимок по gRPC, и раскладка в нём определяет, где
/// находятся имена игроков и счёт. Если разбор снимка теряет раскладку, оверлей
/// вернётся к значениям по умолчанию и начнёт рисовать не там.
/// </summary>
public class ScoreboardGrpcMapperTests
{
    /// <summary>
    /// Раскладка переживает обратное преобразование: иначе оверлей терял бы
    /// положение элементов после каждого обновления.
    /// </summary>
    [Fact]
    public void LayoutSurvivesRoundTrip()
    {
        var dto = new ScoreboardDto
        {
            Layout = new ScoreboardLayoutDto
            {
                HeaderTop = 17,
                HeaderLeft = 51,
                PlayersTop = 3,
                PlayersLeft = 4,
                PlayersRight = 5,
                HeaderHeight = 61,
                HeaderWidth = 401,
                PlayerBarHeight = 81,
                PlayerBarWidth = 501,
                ScoreSize = 61,
                FlagSize = 25,
                Spacing = 17,
                Padding = 17,
                ShowHeader = false,
                ShowFlags = false,
                ShowSponsors = false,
                ShowTags = false,
            },
        };

        var restored = ScoreboardGrpcMapper.ToDto(ScoreboardGrpcMapper.ToProto(dto));

        Assert.NotNull(restored.Layout);
        Assert.Equal(17, restored.Layout.HeaderTop);
        Assert.Equal(51, restored.Layout.HeaderLeft);
        Assert.Equal(3, restored.Layout.PlayersTop);
        Assert.Equal(61, restored.Layout.HeaderHeight);
        Assert.Equal(401, restored.Layout.HeaderWidth);
        Assert.Equal(81, restored.Layout.PlayerBarHeight);
        Assert.Equal(501, restored.Layout.PlayerBarWidth);
        Assert.Equal(61, restored.Layout.ScoreSize);
        Assert.Equal(25, restored.Layout.FlagSize);
        Assert.False(restored.Layout.ShowHeader);
        Assert.False(restored.Layout.ShowFlags);
        Assert.False(restored.Layout.ShowSponsors);
        Assert.False(restored.Layout.ShowTags);
    }

    /// <summary>
    /// Снимок без раскладки остаётся без неё: иначе оверлей получил бы выдуманные
    /// значения вместо своих умолчаний.
    /// </summary>
    [Fact]
    public void MissingLayoutStaysMissing()
    {
        var proto = ScoreboardGrpcMapper.ToProto(new ScoreboardDto());

        Assert.Null(proto.Layout);

        var restored = ScoreboardGrpcMapper.ToDto(proto);

        Assert.Null(restored.Layout);
    }
}
