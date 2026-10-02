using System;
using MARS.Shared.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.Admin.Data.Migrations
{
    /// <summary>
    /// Перенос общих справочников, архивов и полного состояния настроек из
    /// staging-схемы <c>public</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>admin</c> — единственный сервис, который получает <em>все</em> строки
    /// <c>RootState</c>: остальные сервисы забирают свои ключи, а интерфейс
    /// админки читает полный список. Поэтому для этой копии ожидаемое число не
    /// задаётся — сверяется равенство с числом строк в staging.
    /// </para>
    /// <para>
    /// <c>ServiceStates</c>: legacy-колонка <c>IsActive</c> стала
    /// <c>IsServiceActive</c>. В новой модели «активен» может означать «включён в
    /// конфигурации» (например, выключен feature-флагом), а не «отвечает»,
    /// поэтому одинаковое имя у двух разных вопросов сбивало бы с толку.
    /// </para>
    /// <para>
    /// <c>TwitchUsers</c> и <c>SevenTvEmotes</c> хранятся в двух базах. Здесь
    /// переносится проекция для интерфейса: пять колонок без авторизации и
    /// приватности, полная запись уезжает в <c>twitch.TwitchUsers</c>. Семь
    /// эмоций-дублей не создают проблемы: набор приходит из внешнего источника и
    /// при следующей синхронизации перезаписывается целиком.
    /// </para>
    /// </remarks>
    public partial class SeedLegacyAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                LegacyDataSeed.Seed(
                    new[]
                    {
                        // TwitchUsers — родитель FollowerInfos: подписчики ссылаются
                        // на него по TwitchId, поэтому идёт раньше.
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "TwitchUsers",
                            TargetSchema: "admin",
                            TargetTable: "TwitchUsers",
                            Columns: LegacyDataSeed.Columns(
                                "TwitchId",
                                "UserLogin",
                                "DisplayName",
                                "ProfileImageUrl",
                                "LastUpdated"
                            ),
                            UniqueSourceColumns: new[] { "TwitchId" }
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "FollowersEntitys",
                            TargetSchema: "admin",
                            TargetTable: "FollowerInfos",
                            Columns: LegacyDataSeed.Columns("UserId"),
                            UniqueSourceColumns: new[] { "UserId" }
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "EnvironmentVariables",
                            TargetSchema: "admin",
                            TargetTable: "EnvironmentVariables",
                            Columns: LegacyDataSeed.Columns(
                                "Key",
                                "Value",
                                "Description",
                                "CreatedAt",
                                "UpdatedAt"
                            ),
                            UniqueSourceColumns: new[] { "Key" }
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "ServiceStates",
                            TargetSchema: "admin",
                            TargetTable: "ServiceStates",
                            Columns: new (string Target, string Source)[]
                            {
                                LegacyDataSeed.Column("Id"),
                                LegacyDataSeed.Column("ServiceName"),
                                LegacyDataSeed.Column("DisplayName"),
                                LegacyDataSeed.Column("Description"),
                                LegacyDataSeed.Column("IsServiceActive", "\"IsActive\""),
                                LegacyDataSeed.Column("Status"),
                                LegacyDataSeed.Column("LastStartTime"),
                                LegacyDataSeed.Column("LastActivity"),
                                LegacyDataSeed.Column("CreatedAt"),
                                LegacyDataSeed.Column("UpdatedAt"),
                            }
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "SevenTvEmotes",
                            TargetSchema: "admin",
                            TargetTable: "SevenTvEmotes",
                            Columns: LegacyDataSeed.Columns("Name", "LoadedAt"),
                            UniqueSourceColumns: new[] { "Name" }
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "StreamArchiveConfigs",
                            TargetSchema: "admin",
                            TargetTable: "StreamArchiveConfigs",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "TelegramChannelId",
                                "FileNameFormat",
                                "CheckSpan",
                                "FolderPath",
                                "IsConvertFile",
                                "FileConvertType"
                            )
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "StreamArchiveFiles",
                            TargetSchema: "admin",
                            TargetTable: "StreamArchiveFiles",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "ConfigId",
                                "OriginalFileName",
                                "ProcessedFileName",
                                "OriginalFilePath",
                                "OriginalFileSize",
                                "DiscoveredAt",
                                "ProcessingStartedAt",
                                "ProcessingCompletedAt",
                                "Status",
                                "ChunksCount",
                                "ErrorMessage",
                                "TelegramMessageId"
                            )
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "StreamArchiveFileChunks",
                            TargetSchema: "admin",
                            TargetTable: "StreamArchiveFileChunks",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "FileId",
                                "ChunkNumber",
                                "TotalChunks",
                                "ChunkFileName",
                                "ChunkSize",
                                "OffsetInOriginalFile",
                                "UploadedAt",
                                "TelegramMessageId",
                                "Status",
                                "ErrorMessage"
                            )
                        ),
                        // Идёт последним: остальные таблицы читают ключи из RootState
                        // при первом обращении, и на момент их вставки настроек ещё нет.
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "RootState",
                            TargetSchema: "admin",
                            TargetTable: "RootState",
                            Columns: LegacyDataSeed.Columns(
                                "Name",
                                "Value",
                                "Description",
                                "TypeDescription"
                            ),
                            UniqueSourceColumns: new[] { "Name" }
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
