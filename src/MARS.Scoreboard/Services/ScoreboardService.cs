using MARS.Scoreboard.Data;
using MARS.Scoreboard.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.Scoreboard.Services;

public class ScoreboardService(
    IDbContextFactory<ScoreboardDbContext> factory,
    ILogger<ScoreboardService> logger
)
{
    public async Task<ScoreboardDto> GetCurrentStateAsync()
    {
        var result = CreateDefaultState();

        await using var context = await factory.CreateDbContextAsync();

        var state = await context
            .ScoreboardStates.AsNoTracking()
            .Include(s => s.Players)
            .Include(s => s.Layout)
            .SingleOrDefaultAsync();

        if (state != null)
        {
            result = MapToDto(state);
        }

        return result;
    }

    public async Task UpdateStateAsync(ScoreboardDto? dto)
    {
        if (dto == null)
        {
            return;
        }

        await using var context = await factory.CreateDbContextAsync();

        var state = await context
            .ScoreboardStates.Include(s => s.Players)
            .Include(s => s.Layout)
            .SingleOrDefaultAsync();

        if (state == null)
        {
            state = new ScoreboardState { CreatedAt = DateTime.Now };
            context.ScoreboardStates.Add(state);
        }

        state.Title = dto.Meta.Title;
        state.FightRule = dto.Meta.FightRule;
        state.MainColor = dto.Colors.MainColor;
        state.PlayerNamesColor = dto.Colors.PlayerNamesColor;
        state.TournamentTitleColor = dto.Colors.TournamentTitleColor;
        state.FightModeColor = dto.Colors.FightModeColor;
        state.ScoreColor = dto.Colors.ScoreColor;
        state.BackgroundColor = dto.Colors.BackgroundColor;
        state.BorderColor = dto.Colors.BorderColor;
        state.IsVisible = dto.IsVisible;
        state.AnimationDuration = dto.AnimationDuration;
        state.UpdatedAt = DateTime.Now;
        state.IsActive = true;

        var player1 = state.Players.FirstOrDefault(p => p.Position == 1);
        if (player1 == null)
        {
            player1 = new ScoreboardPlayer { Position = 1 };
            state.Players.Add(player1);
        }
        player1.Name = dto.Player1.Name;
        player1.Sponsor = dto.Player1.Sponsor;
        player1.Score = dto.Player1.Score;
        player1.Tag = dto.Player1.Tag;
        player1.Flag = dto.Player1.Flag;
        player1.Final = dto.Player1.Final;

        var player2 = state.Players.FirstOrDefault(p => p.Position == 2);
        if (player2 == null)
        {
            player2 = new ScoreboardPlayer { Position = 2 };
            state.Players.Add(player2);
        }
        player2.Name = dto.Player2.Name;
        player2.Sponsor = dto.Player2.Sponsor;
        player2.Score = dto.Player2.Score;
        player2.Tag = dto.Player2.Tag;
        player2.Flag = dto.Player2.Flag;
        player2.Final = dto.Player2.Final;

        if (dto.Layout != null)
        {
            state.Layout ??= new ScoreboardLayout();
            state.Layout.HeaderTop = dto.Layout.HeaderTop;
            state.Layout.HeaderLeft = dto.Layout.HeaderLeft;
            state.Layout.PlayersTop = dto.Layout.PlayersTop;
            state.Layout.PlayersLeft = dto.Layout.PlayersLeft;
            state.Layout.PlayersRight = dto.Layout.PlayersRight;
            state.Layout.HeaderHeight = dto.Layout.HeaderHeight;
            state.Layout.HeaderWidth = dto.Layout.HeaderWidth;
            state.Layout.PlayerBarHeight = dto.Layout.PlayerBarHeight;
            state.Layout.PlayerBarWidth = dto.Layout.PlayerBarWidth;
            state.Layout.ScoreSize = dto.Layout.ScoreSize;
            state.Layout.FlagSize = dto.Layout.FlagSize;
            state.Layout.Spacing = dto.Layout.Spacing;
            state.Layout.Padding = dto.Layout.Padding;
            state.Layout.ShowHeader = dto.Layout.ShowHeader;
            state.Layout.ShowFlags = dto.Layout.ShowFlags;
            state.Layout.ShowSponsors = dto.Layout.ShowSponsors;
            state.Layout.ShowTags = dto.Layout.ShowTags;
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Scoreboard state updated: {Title}", state.Title);
    }

    public async Task<bool> SetVisibilityAsync(bool isVisible)
    {
        await using var context = await factory.CreateDbContextAsync();

        var currentState = await context.ScoreboardStates.SingleOrDefaultAsync();

        if (currentState != null)
        {
            currentState.IsVisible = isVisible;
            currentState.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();
            logger.LogInformation("Scoreboard visibility set to: {IsVisible}", isVisible);
            return true;
        }

        var newState = new ScoreboardState
        {
            IsVisible = isVisible,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsActive = true,
        };
        context.ScoreboardStates.Add(newState);
        await context.SaveChangesAsync();
        logger.LogInformation("Scoreboard state created with visibility: {IsVisible}", isVisible);
        return true;
    }

    public async Task<bool> UpdatePlayerScoreAsync(int playerPosition, int newScore)
    {
        if (playerPosition <= 0)
        {
            return false;
        }

        await using var context = await factory.CreateDbContextAsync();

        var currentStateId = await context
            .ScoreboardStates.Select(s => s.Id)
            .SingleOrDefaultAsync();

        if (currentStateId == 0)
        {
            return false;
        }

        var player = await context
            .ScoreboardPlayers.Include(p => p.ScoreboardState)
            .Where(p => p.ScoreboardStateId == currentStateId && p.Position == playerPosition)
            .FirstOrDefaultAsync();

        if (player == null)
        {
            return false;
        }

        player.Score = newScore;
        player.ScoreboardState.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        logger.LogInformation("Player {Position} score updated to: {Score}", playerPosition, newScore);
        return true;
    }

    public async Task<bool> SetPlayerFinalAsync(int playerPosition, string final)
    {
        if (playerPosition <= 0 || string.IsNullOrWhiteSpace(final))
        {
            return false;
        }

        await using var context = await factory.CreateDbContextAsync();

        var currentStateId = await context
            .ScoreboardStates.Select(s => s.Id)
            .SingleOrDefaultAsync();

        if (currentStateId == 0)
        {
            return false;
        }

        var player = await context
            .ScoreboardPlayers.Include(p => p.ScoreboardState)
            .Where(p => p.ScoreboardStateId == currentStateId && p.Position == playerPosition)
            .FirstOrDefaultAsync();

        if (player == null)
        {
            return false;
        }

        player.Final = final;
        player.ScoreboardState.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        logger.LogInformation(
            "Player {Position} final status set to: {Final}",
            playerPosition,
            final
        );
        return true;
    }

    private static ScoreboardDto CreateDefaultState()
    {
        return new ScoreboardDto
        {
            Player1 = new ScoreboardPlayerDto { Name = "Player 1" },
            Player2 = new ScoreboardPlayerDto { Name = "Player 2" },
            Meta = new ScoreboardMetaDto { Title = "Tournament", FightRule = "Grand Finals" },
            Colors = new ScoreboardColorsDto(),
            IsVisible = true,
            AnimationDuration = 800,
            Layout = new ScoreboardLayoutDto(),
        };
    }

    private static ScoreboardDto MapToDto(ScoreboardState state)
    {
        var player1 = state.Players.FirstOrDefault(p => p.Position == 1);
        var player2 = state.Players.FirstOrDefault(p => p.Position == 2);

        return new ScoreboardDto
        {
            Player1 = new ScoreboardPlayerDto
            {
                Name = player1?.Name ?? "",
                Sponsor = player1?.Sponsor ?? "",
                Score = player1?.Score ?? 0,
                Tag = player1?.Tag ?? "",
                Flag = player1?.Flag ?? "",
                Final = player1?.Final ?? "none",
            },
            Player2 = new ScoreboardPlayerDto
            {
                Name = player2?.Name ?? "",
                Sponsor = player2?.Sponsor ?? "",
                Score = player2?.Score ?? 0,
                Tag = player2?.Tag ?? "",
                Flag = player2?.Flag ?? "",
                Final = player2?.Final ?? "none",
            },
            Meta = new ScoreboardMetaDto { Title = state.Title, FightRule = state.FightRule },
            Colors = new ScoreboardColorsDto
            {
                MainColor = state.MainColor,
                PlayerNamesColor = state.PlayerNamesColor,
                TournamentTitleColor = state.TournamentTitleColor,
                FightModeColor = state.FightModeColor,
                ScoreColor = state.ScoreColor,
                BackgroundColor = state.BackgroundColor,
                BorderColor = state.BorderColor,
            },
            IsVisible = state.IsVisible,
            AnimationDuration = state.AnimationDuration,
            Layout =
                state.Layout != null
                    ? new ScoreboardLayoutDto
                    {
                        HeaderTop = state.Layout.HeaderTop,
                        HeaderLeft = state.Layout.HeaderLeft,
                        PlayersTop = state.Layout.PlayersTop,
                        PlayersLeft = state.Layout.PlayersLeft,
                        PlayersRight = state.Layout.PlayersRight,
                        HeaderHeight = state.Layout.HeaderHeight,
                        HeaderWidth = state.Layout.HeaderWidth,
                        PlayerBarHeight = state.Layout.PlayerBarHeight,
                        PlayerBarWidth = state.Layout.PlayerBarWidth,
                        ScoreSize = state.Layout.ScoreSize,
                        FlagSize = state.Layout.FlagSize,
                        Spacing = state.Layout.Spacing,
                        Padding = state.Layout.Padding,
                        ShowHeader = state.Layout.ShowHeader,
                        ShowFlags = state.Layout.ShowFlags,
                        ShowSponsors = state.Layout.ShowSponsors,
                        ShowTags = state.Layout.ShowTags,
                    }
                    : new ScoreboardLayoutDto(),
        };
    }
}
