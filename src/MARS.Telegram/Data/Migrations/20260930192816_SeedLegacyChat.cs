using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MARS.Shared.Data;

#nullable disable

namespace MARS.Telegram.Data.Migrations
{
    /// <summary>
    /// Перенос данных Telegram/Discord-моста и планировщика Booru из
    /// staging-схемы <c>public</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>TelegramUpdateReceiverOffset</c> переименована в
    /// <c>TelegramUpdateReceiverOffsets</c>: одиночное «Offset» описывало смещение
    /// одного получателя, а при разделении баз их стало несколько.
    /// </para>
    /// <para>
    /// <c>WTelegramAlloweedChannels</c> переноса в базу не имеет: список
    /// разрешённых каналов читается из конфигурации раздела <c>WTelegram</c>
    /// (свойство <c>AllowedChannelIds</c>), а не из таблицы. Таблица указана в
    /// списке на зачистку, и её значение нужно перенести в конфигурацию
    /// развёртывания вручную — иначе WTelegram перестанет читать канал после
    /// переезда. Это единственное legacy-значение, которому нет места в схемах.
    /// </para>
    /// </remarks>
    public partial class SeedLegacyChat : Migration
    {
        /// <summary>
        /// Ключи <c>RootState</c>, которые читает сервис Telegram: настройки
        /// Google Photos и прокси WTelegram. Остальные живут в своих сервисах,
        /// а полная копия остаётся в <c>admin.RootState</c> для интерфейса.
        /// </summary>
        private const string ChatRootStateFilter =
            "\"Name\" IN ('GooglePhotosAccessToken', 'GooglePhotosAccessTokenExpiresAtUtc',"
            + " 'GooglePhotosIsAuthorized', 'GooglePhotosOAuthState', 'GooglePhotosRefreshToken',"
            + " 'WTelegramMtProxyUrl', 'WTelegramProxyUrl')";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                LegacyDataSeed.Seed(
                    new[]
                    {
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "TelegramUsers",
                            TargetSchema: "chat",
                            TargetTable: "TelegramUsers",
                            Columns: LegacyDataSeed.Columns(
                                "UserId",
                                "Name",
                                "LastTimeMessage",
                                "RaidHelper",
                                "PyroAlertsAccess",
                                "IsRandomMemeSendler",
                                "HonkaiNotifications",
                                "StreamUpNotifications",
                                "ZenlessZoneZeroDailyNotif",
                                "GenshinImpactDailyNotif",
                                "ByeByeLastMessageTime",
                                "ByeByeServiceNotification")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "ChannelProcessingStates",
                            TargetSchema: "chat",
                            TargetTable: "ChannelProcessingStates",
                            Columns: LegacyDataSeed.Columns(
                                "ChannelId",
                                "OffsetId",
                                "MessagesHash",
                                "LastUpdated")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "TelegramDiscordChannelBindings",
                            TargetSchema: "chat",
                            TargetTable: "TelegramDiscordChannelBindings",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "TelegramChannelId",
                                "DiscordChannelId",
                                "IsEnabled",
                                "CreatedAtUtc",
                                "UpdatedAtUtc",
                                "LastError")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "TelegramDiscordChannelStates",
                            TargetSchema: "chat",
                            TargetTable: "TelegramDiscordChannelStates",
                            Columns: LegacyDataSeed.Columns(
                                "TelegramChannelId",
                                "LastProcessedMessageId",
                                "LastUpdatedUtc")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "TelegramUpdateReceiverOffset",
                            TargetSchema: "chat",
                            TargetTable: "TelegramUpdateReceiverOffsets",
                            Columns: LegacyDataSeed.Columns("Id", "Offset")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "WTelegramSessions",
                            TargetSchema: "chat",
                            TargetTable: "WTelegramSessions",
                            Columns: LegacyDataSeed.Columns("Name", "Data")
                        ),

                        // BooruAutoPostConfigs идёт перед BooruScheduledPosts:
                        // у запланированных постов есть внешний ключ на конфиг.
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "BooruAutoPostConfigs",
                            TargetSchema: "chat",
                            TargetTable: "BooruAutoPostConfigs",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "Source",
                                "TargetPlatform",
                                "DiscordChannelId",
                                "TelegramChannelId",
                                "TargetPostCount",
                                "SpecificPostId",
                                "Tags",
                                "CronExpression",
                                "PlanningHorizonDays",
                                "IsEnabled",
                                "Message",
                                "TelegramParseMode",
                                "LastExecutedAtUtc",
                                "CreatedAtUtc",
                                "UpdatedAtUtc")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "BooruScheduledPosts",
                            TargetSchema: "chat",
                            TargetTable: "BooruScheduledPosts",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "ConfigId",
                                "Source",
                                "ScheduledAtUtc",
                                "Status",
                                "PostedAtUtc",
                                "ErrorMessage",
                                "CreatedAtUtc")
                        ),

                        new LegacyDataSeed.TableCopy(
                            StagingTable: "RootState",
                            TargetSchema: "chat",
                            TargetTable: "RootState",
                            Columns: LegacyDataSeed.Columns(
                                "Name",
                                "Value",
                                "Description",
                                "TypeDescription"),
                            Filter: ChatRootStateFilter,
                            ExpectedRows: 7
                        ),
                    },
                    "AutoArtsInfo",
                    "Clips",
                    "LiveChannelState",
                    "PostedImageRecords",
                    "Replies",
                    "ResendLinks",
                    "TwitchChannelInspectors",
                    "WaifuChatFacts",
                    "WTelegramAlloweedChannels"
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