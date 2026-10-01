using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MARS.Shared.Data;

#nullable disable

namespace MARS.SoundRequest.Data.Migrations
{
    /// <summary>
    /// Перенос очереди SoundRequest, состояния плеера и треков из staging-схемы
    /// <c>public</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Порядок копий — <c>Tracks</c>, затем <c>QueueItems</c>, затем
    /// <c>PlayerStates</c>: и очередь, и плеер ссылаются на трек по внешнему
    /// ключу.
    /// </para>
    /// <para>
    /// <c>RootState</c> делится между сервисами: здесь едут только ключи
    /// SoundRequest/Spotify, полная копия всех 33 настроек остаётся в
    /// <c>admin.RootState</c>. Поэтому проверка количества сравнивает с семью
    /// ожидаемыми строками, а не со всем staging.
    /// </para>
    /// </remarks>
    public partial class SeedLegacyMedia : Migration
    {
        /// <summary>
        /// Ключи <c>RootState</c>, которые читает SoundRequest. Список из 13
        /// имён задан явно: перенос «всех ключей, похожих на SoundRequest» означал
        /// бы, что новая настройка сервиса молча уедет в чужую базу при первом же
        /// совпадении по имени.
        /// </summary>
        private const string MediaRootStateFilter =
            "\"Name\" IN ('SoundRequestProvider',"
            + " 'SoundRequestSpotifyAccessToken', 'SoundRequestSpotifyAccessTokenExpiresAtUtc',"
            + " 'SoundRequestSpotifyAvatarUrl', 'SoundRequestSpotifyClientId',"
            + " 'SoundRequestSpotifyClientSecret', 'SoundRequestSpotifyDeviceId',"
            + " 'SoundRequestSpotifyDisplayName', 'SoundRequestSpotifyOAuthState',"
            + " 'SoundRequestSpotifyProduct', 'SoundRequestSpotifyRedirectUri',"
            + " 'SoundRequestSpotifyRefreshToken', 'SoundRequestSpotifyUserId')";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                LegacyDataSeed.Seed(
                    new[]
                    {
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "SoundRequestBaseTrackInfos",
                            TargetSchema: "media",
                            TargetTable: "Tracks",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "TrackName",
                                "Authors",
                                "Duration",
                                "Url",
                                "LastTimePlays",
                                "ArtworkUrl",
                                "VideoId",
                                "IsDeleted",
                                "CreatedAt",
                                "UpdatedAt")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "SoundRequestQueueItems",
                            TargetSchema: "media",
                            TargetTable: "QueueItems",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "TrackId",
                                "QueueOrder",
                                "RequestedByTwitchId",
                                "RequestedAt")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "SoundRequestPlayerState",
                            TargetSchema: "media",
                            TargetTable: "PlayerStates",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "CurrentQueueItemId",
                                "CurrentTrackProgress",
                                "State",
                                "VideoState",
                                "IsMuted",
                                "PausedByMute",
                                "Volume")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "RootState",
                            TargetSchema: "media",
                            TargetTable: "RootState",
                            Columns: LegacyDataSeed.Columns(
                                "Name",
                                "Value",
                                "Description",
                                "TypeDescription"),
                            Filter: MediaRootStateFilter,
                            ExpectedRows: 13
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