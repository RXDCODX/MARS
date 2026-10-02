using System;
using MARS.Shared.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.MediaStorage.Data.Migrations
{
    /// <summary>
    /// Перенос озвучки, типов мемов и порядка показа из staging-схемы
    /// <c>public</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>RandomMemeType</c> идёт перед <c>RandomMemeOrder</c>: у записи порядка
    /// есть ссылка на тип, и наоборот — нет.
    /// </para>
    /// <para>
    /// <c>FileInfo_LocalFilePath</c> в новой схеме <c>NOT NULL</c>, хотя в legacy
    /// пустых значений не было. Подстановка стоит на случай следующего
    /// экспорта: молча упавшая миграция из-за одного <c>NULL</c> хуже пустого
    /// пути, который алерты не используют.
    /// </para>
    /// <para>
    /// <c>MediaEntries</c> в staging-источнике нет: описание файлов заведено уже
    /// после разделения баз.
    /// </para>
    /// </remarks>
    public partial class SeedLegacyMediaStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                LegacyDataSeed.Seed(
                    new[]
                    {
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "RandomMemeType",
                            TargetSchema: "mediastorage",
                            TargetTable: "RandomMemeType",
                            Columns: LegacyDataSeed.Columns("Id", "Name", "FolderPath")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "RandomMemeOrder",
                            TargetSchema: "mediastorage",
                            TargetTable: "RandomMemeOrder",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "Order",
                                "FilePath",
                                "MemeTypeId",
                                "IsFileNotConvertable"
                            )
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "Alerts",
                            TargetSchema: "mediastorage",
                            TargetTable: "Alerts",
                            Columns: new (string Target, string Source)[]
                            {
                                LegacyDataSeed.Column("Id"),
                                LegacyDataSeed.Column("TextInfo_KeyWordsColor"),
                                LegacyDataSeed.Column("TextInfo_TriggerWord"),
                                LegacyDataSeed.Column("TextInfo_Text"),
                                LegacyDataSeed.Column("TextInfo_TextColor"),
                                LegacyDataSeed.Column("TextInfo_KeyWordSybmolDelimiter"),
                                LegacyDataSeed.Column(
                                    "FileInfo_LocalFilePath",
                                    "coalesce(\"FileInfo_LocalFilePath\", '')"
                                ),
                                LegacyDataSeed.Column("FileInfo_Type"),
                                LegacyDataSeed.Column("FileInfo_IsLocal"),
                                LegacyDataSeed.Column("FileInfo_FileName"),
                                LegacyDataSeed.Column("FileInfo_Extension"),
                                LegacyDataSeed.Column("FileInfo_IsFileNotConvertable"),
                                LegacyDataSeed.Column("PositionInfo_IsProportion"),
                                LegacyDataSeed.Column("PositionInfo_IsResizeRequires"),
                                LegacyDataSeed.Column("PositionInfo_Height"),
                                LegacyDataSeed.Column("PositionInfo_Width"),
                                LegacyDataSeed.Column("PositionInfo_IsRotated"),
                                LegacyDataSeed.Column("PositionInfo_Rotation"),
                                LegacyDataSeed.Column("PositionInfo_XCoordinate"),
                                LegacyDataSeed.Column("PositionInfo_YCoordinate"),
                                LegacyDataSeed.Column("PositionInfo_RandomCoordinates"),
                                LegacyDataSeed.Column("PositionInfo_IsVerticallCenter"),
                                LegacyDataSeed.Column("PositionInfo_IsHorizontalCenter"),
                                LegacyDataSeed.Column("PositionInfo_IsUseOriginalWidthAndHeight"),
                                LegacyDataSeed.Column("MetaInfo_TwitchPointsCost"),
                                LegacyDataSeed.Column("MetaInfo_TwitchGuid"),
                                LegacyDataSeed.Column("MetaInfo_VIP"),
                                LegacyDataSeed.Column("MetaInfo_DisplayName"),
                                LegacyDataSeed.Column("MetaInfo_IsLooped"),
                                LegacyDataSeed.Column("MetaInfo_IsFreezeRequired"),
                                LegacyDataSeed.Column("MetaInfo_Duration"),
                                LegacyDataSeed.Column("MetaInfo_Priority"),
                                LegacyDataSeed.Column("MetaInfo_Volume"),
                                LegacyDataSeed.Column("MetaInfo_IsEnabled"),
                                LegacyDataSeed.Column("StylesInfo_IsBorder"),
                                LegacyDataSeed.Column("StylesInfo_IsShowLetterbox"),
                            }
                        ),
                    },
                    // Таблицы, которые остались от монолита и никуда не поехали.
                    // Их удаление делает перенос завершённым: в сервисной базе не
                    // остаётся legacy-мусора, который ни одна миграция не читает.
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
