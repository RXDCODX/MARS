using MARS.Twitch.Infrastructure.Data;
using MARS.Twitch.Domain.Entities;
using MARS.Twitch.Infrastructure.Services.ChannelRewards;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;

namespace MARS.TwitchCore.Tests.Services;

public sealed class ChannelRewardsSyncServiceTests : IAsyncLifetime
{
    private readonly TwitchTestDbContextFactory _factory = new();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task CreatesNewRewardAndBindsId()
    {
        var ct = TestContext.Current.CancellationToken;
        await using (var db = await _factory.CreateDbContextAsync(ct))
        {
            db.ChannelRewards.Add(new ChannelRewardRecord { Title = "награда", Cost = 100 });
            await db.SaveChangesAsync(ct);
        }

        var rewards = new Mock<IChannelRewardsService>();
        rewards.Setup(r => r.GetRewardsAsync()).ReturnsAsync(new List<CustomReward>());
        rewards
            .Setup(r => r.CreateRewardAsync(It.IsAny<CreateCustomRewardsRequest>()))
            .ReturnsAsync("new-id");

        var binding = new Mock<IRewardBindingSyncService>();
        binding
            .Setup(b => b.SyncAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RewardBindingReport([]));

        var service = new ChannelRewardsSyncService(
            rewards.Object,
            binding.Object,
            _factory,
            NullLogger<ChannelRewardsSyncService>.Instance
        );

        await service.SyncOnce(ct);

        await using (var db = await _factory.CreateDbContextAsync(ct))
        {
            var record = Assert.Single(db.ChannelRewards);
            Assert.Equal("new-id", record.TwitchRewardId);
        }

        rewards.Verify(
            r => r.CreateRewardAsync(It.IsAny<CreateCustomRewardsRequest>()),
            Times.Once
        );
    }

    [Fact]
    public async Task IsIdempotentWhenRewardAlreadyBound()
    {
        var ct = TestContext.Current.CancellationToken;
        await using (var db = await _factory.CreateDbContextAsync(ct))
        {
            db.ChannelRewards.Add(
                new ChannelRewardRecord
                {
                    Title = "награда",
                    Cost = 100,
                    TwitchRewardId = "existing-id",
                }
            );
            await db.SaveChangesAsync(ct);
        }

        var rewards = new Mock<IChannelRewardsService>();
        rewards
            .Setup(r => r.GetRewardsAsync())
            .ReturnsAsync(
                new List<CustomReward>
                {
                    TwitchLibModels.Create<CustomReward>(
                        ("Id", "existing-id"),
                        ("Title", "награда"),
                        ("Cost", 100)
                    ),
                }
            );
        rewards
            .Setup(r => r.CreateRewardAsync(It.IsAny<CreateCustomRewardsRequest>()))
            .ReturnsAsync("should-not-be-called");

        var binding = new Mock<IRewardBindingSyncService>();
        binding
            .Setup(b => b.SyncAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RewardBindingReport([]));

        var service = new ChannelRewardsSyncService(
            rewards.Object,
            binding.Object,
            _factory,
            NullLogger<ChannelRewardsSyncService>.Instance
        );

        await service.SyncOnce(ct);
        await service.SyncOnce(ct);

        rewards.Verify(
            r => r.CreateRewardAsync(It.IsAny<CreateCustomRewardsRequest>()),
            Times.Never
        );

        await using (var db = await _factory.CreateDbContextAsync(ct))
        {
            var record = Assert.Single(db.ChannelRewards);
            Assert.Equal("existing-id", record.TwitchRewardId);
        }
    }

    [Fact]
    public async Task HandlesConflictIdGracefully()
    {
        var ct = TestContext.Current.CancellationToken;
        Guid ownerId;
        await using (var db = await _factory.CreateDbContextAsync(ct))
        {
            var owner = new ChannelRewardRecord
            {
                Title = "owner",
                Cost = 50,
                TwitchRewardId = "conflict-id",
            };
            db.ChannelRewards.Add(owner);
            db.ChannelRewards.Add(new ChannelRewardRecord { Title = "новая", Cost = 100 });
            await db.SaveChangesAsync(ct);
            ownerId = owner.Id;
        }

        var rewards = new Mock<IChannelRewardsService>();
        rewards.Setup(r => r.GetRewardsAsync()).ReturnsAsync(new List<CustomReward>());
        rewards
            .Setup(r => r.CreateRewardAsync(It.IsAny<CreateCustomRewardsRequest>()))
            .ReturnsAsync("conflict-id");

        var binding = new Mock<IRewardBindingSyncService>();
        binding
            .Setup(b => b.SyncAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RewardBindingReport([]));

        var service = new ChannelRewardsSyncService(
            rewards.Object,
            binding.Object,
            _factory,
            NullLogger<ChannelRewardsSyncService>.Instance
        );

        await service.SyncOnce(ct);

        await using (var db = await _factory.CreateDbContextAsync(ct))
        {
            // Уникальный индекс не нарушен: ровно одна строка с conflict-id.
            Assert.Equal(1, db.ChannelRewards.Count(r => r.TwitchRewardId == "conflict-id"));
            var owner = db.ChannelRewards.First(r => r.Id == ownerId);
            Assert.Equal("conflict-id", owner.TwitchRewardId);
        }
    }
}
