using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MARS.Shared.Data;

#nullable disable

namespace MARS.TwitchCore.Data.Migrations
{
    /// <summary>
    /// Перенос данных Twitch-сервиса из staging-схемы <c>public</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>TwitchToken.ExpiresIn</c> в legacy — это <c>time</c> без зоны
    /// («04:23:12»), а в новой модели это <c>TimeSpan?</c>. Приводится к
    /// <c>interval</c> обычным кастом: значение типа <c>time</c> неотрицательно
    /// по построению, так что отдельная обработка знака не нужна, и
    /// <see cref="LegacyDataSeed.TextTimestamp"/> тут был бы неуместен.
    /// </para>
    /// <para>
    /// <c>WhenExpires</c> переносить некуда: в новой схеме срок вычисляется из
    /// <c>WhenCreated + ExpiresIn</c>, а хранить его отдельно значило бы завести
    /// второе поле, которое может разойтись с первым.
    /// </para>
    /// <para>
    /// <c>Husbands</c> берётся из той же legacy-таблицы, что и в Waifu, но только
    /// четыре колонки, относящиеся к Twitch: приватность, время приватности и
    /// месяц последнего поздравления. Идентификаторы жениха и невесты — домен
    /// Waifu, и в базе Twitch им не на что сослаться.
    /// </para>
    /// </remarks>
    public partial class SeedLegacyTwitch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                LegacyDataSeed.Seed(
                    new[]
                    {
                        // TwitchUsers — родитель пяти таблиц ниже (FollowersEntitys,
                        // FumoUsers, HelloVideosUsers, Husbands, TwitchLeaderboardUsers),
                        // поэтому идёт первым: иначе внешний ключ отверг бы вставку.
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "TwitchUsers",
                            TargetSchema: "twitch",
                            TargetTable: "TwitchUsers",
                            Columns: LegacyDataSeed.Columns(
                                "TwitchId",
                                "UserLogin",
                                "DisplayName",
                                "ProfileImageUrl",
                                "ChatColor",
                                "IsModerator",
                                "IsVip",
                                "FollowedAt",
                                "LastUpdated",
                                "CreatedAt",
                                "AliasNickname",
                                "IsInBlockList"),
                            UniqueSourceColumns: new[] { "TwitchId" }
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "FollowersEntitys",
                            TargetSchema: "twitch",
                            TargetTable: "FollowersEntitys",
                            Columns: LegacyDataSeed.Columns("UserId"),
                            UniqueSourceColumns: new[] { "UserId" }
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "SevenTvEmotes",
                            TargetSchema: "twitch",
                            TargetTable: "SevenTvEmotes",
                            Columns: LegacyDataSeed.Columns("Name", "LoadedAt"),
                            UniqueSourceColumns: new[] { "Name" }
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "AutoMessages",
                            TargetSchema: "twitch",
                            TargetTable: "AutoMessages",
                            Columns: LegacyDataSeed.Columns("Id", "Message")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "HelloVideosUsers",
                            TargetSchema: "twitch",
                            TargetTable: "HelloVideosUsers",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "TwitchId",
                                "LastTimeNotif",
                                "MediaInfoId")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "Husbands",
                            TargetSchema: "twitch",
                            TargetTable: "Husbands",
                            Columns: LegacyDataSeed.Columns(
                                "TwitchId",
                                "IsPrivated",
                                "WhenPrivated",
                                "LastWeddingCongratulatedMonths"),
                            UniqueSourceColumns: new[] { "TwitchId" }
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "FumoUsers",
                            TargetSchema: "twitch",
                            TargetTable: "FumoUsers",
                            Columns: LegacyDataSeed.Columns("TwitchId", "LastTime"),
                            UniqueSourceColumns: new[] { "TwitchId" }
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "RollCooldowns",
                            TargetSchema: "twitch",
                            TargetTable: "RollCooldowns",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "TwitchUserId",
                                "RollType",
                                "LastRollTime")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "ChannelRewards",
                            TargetSchema: "twitch",
                            TargetTable: "ChannelRewards",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "Title",
                                "Cost",
                                "IsEnabled",
                                "Prompt",
                                "BackgroundColor",
                                "IsUserInputRequired",
                                "IsMaxPerStreamEnabled",
                                "MaxPerStream",
                                "IsMaxPerUserPerStreamEnabled",
                                "MaxPerUserPerStream",
                                "IsGlobalCooldownEnabled",
                                "GlobalCooldownSeconds",
                                "ShouldRedemptionsSkipRequestQueue",
                                "IsDeleted",
                                "TwitchRewardId",
                                "MediaInfoId")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "TwitchToken",
                            TargetSchema: "twitch",
                            TargetTable: "TwitchToken",
                            Columns: new (string Target, string Source)[]
                            {
                                LegacyDataSeed.Column("Id"),
                                LegacyDataSeed.Column("AccessToken"),
                                LegacyDataSeed.Column("RefreshToken"),
                                LegacyDataSeed.Column("ExpiresIn", "\"ExpiresIn\"::interval"),
                                LegacyDataSeed.Column("WhenCreated"),
                            }
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "TwitchLeaderboardUsers",
                            TargetSchema: "twitch",
                            TargetTable: "TwitchLeaderboardUsers",
                            Columns: LegacyDataSeed.Columns(
                                "TwitchId",
                                "RussianRouletteWins",
                                "RussianRouletteWinsWithWaifu",
                                "TriviaWins",
                                "TriviaWinsWithWaifus"),
                            UniqueSourceColumns: new[] { "TwitchId" }
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "RootState",
                            TargetSchema: "twitch",
                            TargetTable: "RootState",
                            Columns: LegacyDataSeed.Columns(
                                "Name",
                                "Value",
                                "Description",
                                "TypeDescription"),
                            Filter:
                                "\"Name\" IN ('PuntoSwitcherFilterEnabled', 'TtsFilterEnabled')",
                            ExpectedRows: 2
                        ),
                    },
                    "AutoArtsInfo",
                    "Clips",
                    "LiveChannelState",
                    "PostedImageRecords",
                    "Replies",
                    "ResendLinks",
                    "TwitchChannelInspectors",
                    "WaifuChatFacts"
                )
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Откат переноса данных невозможен: staging удаляется на этапе Up, а сами
            // строки — единственная копия legacy-данных. Восстановление только из
            // резервной копии БД.
            throw new NotSupportedException(
                "Откат переноса данных не поддерживается: staging удаляется при применении"
                    + " миграции. Для восстановления разверните резервную копию."
            );
        }
    }
}