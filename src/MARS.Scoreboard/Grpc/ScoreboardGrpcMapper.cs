using MARS.Scoreboard.Entities;
using MARS.Shared.Grpc.Scoreboard;
using ProtoPlayer = MARS.Shared.Grpc.Scoreboard.ScoreboardPlayer;

namespace MARS.Scoreboard.Grpc;

public static class ScoreboardGrpcMapper
{
    public static ScoreboardSnapshot ToProto(ScoreboardDto dto)
    {
        var snapshot = new ScoreboardSnapshot
        {
            Player1 = ToProto(dto.Player1),
            Player2 = ToProto(dto.Player2),
            Meta = new ScoreboardMeta { Title = dto.Meta.Title, FightRule = dto.Meta.FightRule },
            Colors = new ScoreboardColors
            {
                MainColor = dto.Colors.MainColor,
                PlayerNamesColor = dto.Colors.PlayerNamesColor,
                TournamentTitleColor = dto.Colors.TournamentTitleColor,
                FightModeColor = dto.Colors.FightModeColor,
                ScoreColor = dto.Colors.ScoreColor,
                BackgroundColor = dto.Colors.BackgroundColor,
                BorderColor = dto.Colors.BorderColor,
            },
            IsVisible = dto.IsVisible,
            AnimationDuration = dto.AnimationDuration,
        };

        if (dto.Layout is not null)
        {
            snapshot.Layout = ToProto(dto.Layout);
        }

        return snapshot;
    }

    public static ScoreboardDto ToDto(ScoreboardSnapshot snapshot)
    {
        return new ScoreboardDto
        {
            Player1 = ToDto(snapshot.Player1),
            Player2 = ToDto(snapshot.Player2),
            Meta = snapshot.Meta is null
                ? new ScoreboardMetaDto()
                : new ScoreboardMetaDto
                {
                    Title = snapshot.Meta.Title,
                    FightRule = snapshot.Meta.FightRule,
                },
            Colors = snapshot.Colors is null
                ? new ScoreboardColorsDto()
                : new ScoreboardColorsDto
                {
                    MainColor = snapshot.Colors.MainColor,
                    PlayerNamesColor = snapshot.Colors.PlayerNamesColor,
                    TournamentTitleColor = snapshot.Colors.TournamentTitleColor,
                    FightModeColor = snapshot.Colors.FightModeColor,
                    ScoreColor = snapshot.Colors.ScoreColor,
                    BackgroundColor = snapshot.Colors.BackgroundColor,
                    BorderColor = snapshot.Colors.BorderColor,
                },
            IsVisible = snapshot.IsVisible,
            AnimationDuration = snapshot.AnimationDuration,
            Layout = snapshot.Layout is null ? null : ToDto(snapshot.Layout),
        };
    }

    private static ProtoPlayer ToProto(ScoreboardPlayerDto player)
    {
        return new ProtoPlayer
        {
            Name = player.Name,
            Sponsor = player.Sponsor,
            Score = player.Score,
            Tag = player.Tag,
            Flag = player.Flag,
            Final = player.Final,
        };
    }

    private static ScoreboardPlayerDto ToDto(ProtoPlayer? player)
    {
        return new ScoreboardPlayerDto
        {
            Name = player?.Name ?? string.Empty,
            Sponsor = player?.Sponsor ?? string.Empty,
            Score = player?.Score ?? 0,
            Tag = player?.Tag ?? string.Empty,
            Flag = player?.Flag ?? string.Empty,
            Final = string.IsNullOrEmpty(player?.Final) ? "none" : player.Final,
        };
    }

    private static ScoreboardLayoutSnapshot ToProto(ScoreboardLayoutDto layout)
    {
        return new ScoreboardLayoutSnapshot
        {
            HeaderTop = layout.HeaderTop,
            HeaderLeft = layout.HeaderLeft,
            PlayersTop = layout.PlayersTop,
            PlayersLeft = layout.PlayersLeft,
            PlayersRight = layout.PlayersRight,
            HeaderHeight = layout.HeaderHeight,
            HeaderWidth = layout.HeaderWidth,
            PlayerBarHeight = layout.PlayerBarHeight,
            PlayerBarWidth = layout.PlayerBarWidth,
            ScoreSize = layout.ScoreSize,
            FlagSize = layout.FlagSize,
            Spacing = layout.Spacing,
            Padding = layout.Padding,
            ShowHeader = layout.ShowHeader,
            ShowFlags = layout.ShowFlags,
            ShowSponsors = layout.ShowSponsors,
            ShowTags = layout.ShowTags,
        };
    }

    private static ScoreboardLayoutDto ToDto(ScoreboardLayoutSnapshot layout)
    {
        return new ScoreboardLayoutDto
        {
            HeaderTop = layout.HeaderTop,
            HeaderLeft = layout.HeaderLeft,
            PlayersTop = layout.PlayersTop,
            PlayersLeft = layout.PlayersLeft,
            PlayersRight = layout.PlayersRight,
            HeaderHeight = layout.HeaderHeight,
            HeaderWidth = layout.HeaderWidth,
            PlayerBarHeight = layout.PlayerBarHeight,
            PlayerBarWidth = layout.PlayerBarWidth,
            ScoreSize = layout.ScoreSize,
            FlagSize = layout.FlagSize,
            Spacing = layout.Spacing,
            Padding = layout.Padding,
            ShowHeader = layout.ShowHeader,
            ShowFlags = layout.ShowFlags,
            ShowSponsors = layout.ShowSponsors,
            ShowTags = layout.ShowTags,
        };
    }
}
