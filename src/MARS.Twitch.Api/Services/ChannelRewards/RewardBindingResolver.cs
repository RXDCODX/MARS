using MARS.Shared.Contracts.Rewards;
using MARS.Twitch.Api.Entities;

namespace MARS.Twitch.Api.Services.ChannelRewards;

public static class RewardBindingResolver
{
    public static RewardBindingReport Resolve(
        IReadOnlyList<RewardDefinition> definitions,
        IList<ChannelRewardRecord> localRecords,
        IReadOnlyList<RemoteReward> remoteRewards
    )
    {
        var results = new List<RewardBindingResult>();
        var usedRemoteIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var record in localRecords)
        {
            if (
                !string.IsNullOrWhiteSpace(record.RewardCode)
                && !string.IsNullOrWhiteSpace(record.TwitchRewardId)
            )
            {
                usedRemoteIds.Add(record.TwitchRewardId!);
            }
        }

        foreach (var definition in definitions)
        {
            results.Add(ResolveDefinition(definition, localRecords, remoteRewards, usedRemoteIds));
        }

        return new RewardBindingReport(results);
    }

    private static RewardBindingResult ResolveDefinition(
        RewardDefinition definition,
        IList<ChannelRewardRecord> localRecords,
        IReadOnlyList<RemoteReward> remoteRewards,
        HashSet<string> usedRemoteIds
    )
    {
        var code = definition.Code;

        var codeRecords = localRecords
            .Where(record => string.Equals(record.RewardCode, code, StringComparison.Ordinal))
            .ToList();

        if (codeRecords.Count > 1)
        {
            return new RewardBindingResult(
                code,
                RewardBindingStatus.Conflicting,
                null,
                "duplicate reward code rows"
            );
        }

        ChannelRewardRecord? record = codeRecords.FirstOrDefault();
        if (record is null)
        {
            var legacyRecords = localRecords
                .Where(existing =>
                    string.IsNullOrWhiteSpace(existing.RewardCode)
                    && string.Equals(
                        existing.Title,
                        definition.LegacyTitle,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .ToList();

            if (legacyRecords.Count > 1)
            {
                return new RewardBindingResult(
                    code,
                    RewardBindingStatus.Conflicting,
                    null,
                    "duplicate legacy title rows"
                );
            }

            record = legacyRecords.FirstOrDefault();
        }

        if (record is null)
        {
            return new RewardBindingResult(
                code,
                RewardBindingStatus.Orphaned,
                null,
                "no local reward record"
            );
        }

        if (string.IsNullOrWhiteSpace(record.RewardCode))
        {
            record.RewardCode = code;
        }

        if (string.IsNullOrWhiteSpace(record.NormalizedEmoji))
        {
            record.NormalizedEmoji = definition.NormalizedEmoji;
        }

        if (!string.IsNullOrWhiteSpace(record.TwitchRewardId))
        {
            var liveRemote = remoteRewards.FirstOrDefault(remote =>
                remote.Id == record.TwitchRewardId
            );
            if (liveRemote is not null)
            {
                if (liveRemote.Cost != definition.ExpectedCost)
                {
                    record.BindingStatus = nameof(RewardBindingStatus.CostDrift);
                    return new RewardBindingResult(
                        code,
                        RewardBindingStatus.CostDrift,
                        record.TwitchRewardId,
                        "cost drift"
                    );
                }

                record.BindingStatus = nameof(RewardBindingStatus.AlreadyBound);
                record.Title = liveRemote.Title;
                return new RewardBindingResult(
                    code,
                    RewardBindingStatus.AlreadyBound,
                    record.TwitchRewardId,
                    null
                );
            }
        }

        var candidates = remoteRewards
            .Where(remote =>
                remote.Cost == definition.ExpectedCost
                && string.Equals(
                    RewardEmojiExtractor.ExtractNormalizedLeadingEmoji(remote.Title),
                    definition.NormalizedEmoji,
                    StringComparison.Ordinal
                )
            )
            .ToList();

        var available = candidates
            .Where(candidate => !usedRemoteIds.Contains(candidate.Id))
            .ToList();

        if (available.Count == 1)
        {
            var candidate = available[0];
            if (
                localRecords.Any(existing =>
                    !string.Equals(existing.RewardCode, code, StringComparison.Ordinal)
                    && existing.TwitchRewardId == candidate.Id
                )
            )
            {
                record.BindingStatus = nameof(RewardBindingStatus.Conflicting);
                return new RewardBindingResult(
                    code,
                    RewardBindingStatus.Conflicting,
                    record.TwitchRewardId,
                    "candidate already bound"
                );
            }

            record.TwitchRewardId = candidate.Id;
            record.Title = candidate.Title;
            record.BindingStatus = nameof(RewardBindingStatus.Rebound);
            usedRemoteIds.Add(candidate.Id);
            return new RewardBindingResult(code, RewardBindingStatus.Rebound, candidate.Id, null);
        }

        if (available.Count > 1)
        {
            record.BindingStatus = nameof(RewardBindingStatus.NeedsManualBinding);
            return new RewardBindingResult(
                code,
                RewardBindingStatus.NeedsManualBinding,
                record.TwitchRewardId,
                "ambiguous fingerprint"
            );
        }

        var titleCandidates = remoteRewards
            .Where(remote =>
                string.Equals(
                    remote.Title,
                    definition.LegacyTitle,
                    StringComparison.OrdinalIgnoreCase
                ) && !usedRemoteIds.Contains(remote.Id)
            )
            .ToList();

        if (titleCandidates.Count == 1)
        {
            var candidate = titleCandidates[0];
            if (
                localRecords.Any(existing =>
                    !string.Equals(existing.RewardCode, code, StringComparison.Ordinal)
                    && existing.TwitchRewardId == candidate.Id
                )
            )
            {
                record.BindingStatus = nameof(RewardBindingStatus.Conflicting);
                return new RewardBindingResult(
                    code,
                    RewardBindingStatus.Conflicting,
                    record.TwitchRewardId,
                    "candidate already bound"
                );
            }

            record.TwitchRewardId = candidate.Id;
            record.Title = candidate.Title;
            record.BindingStatus = nameof(RewardBindingStatus.Rebound);
            usedRemoteIds.Add(candidate.Id);
            return new RewardBindingResult(code, RewardBindingStatus.Rebound, candidate.Id, null);
        }

        record.BindingStatus = nameof(RewardBindingStatus.Orphaned);
        return new RewardBindingResult(
            code,
            RewardBindingStatus.Orphaned,
            record.TwitchRewardId,
            "no remote candidate"
        );
    }
}
