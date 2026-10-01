using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MARS.Shared.Data;

#nullable disable

namespace MARS.WaifuGacha.Data.Migrations
{
    /// <summary>
    /// Перенос каталога фумо, мику и лотереи вайфу из staging-схемы
    /// <c>public</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>WhenAdded</c> и <c>LastOrder</c> в <c>Fumos</c> хранились текстом и
    /// содержали <c>-infinity</c> — «дата неизвестна». Здесь
    /// <see cref="LegacyDataSeed.TextTimestamp"/> оставляет <c>-infinity</c>
    /// как есть: целевая колонка объявлена <c>NOT NULL</c> и в доменной модели
    /// это непустой <c>DateTime</c>, так что <c>NULL</c> отверг бы перенос.
    /// Обратимая альтернатива — <c>NULL</c>, но менять модель ради одной
    /// миграции значит менять и поведение всех читателей.
    /// </para>
    /// <para>
    /// <c>AutoHelloMessages</c> уже заполнена миграцией
    /// <c>SeedAutoHelloMessages</c>: её текст и порядок совпадают с legacy
    /// построчно. Guard на непустую целевую таблицу пропустит вставку, а
    /// проверка количества подтвердит, что содержимое то же.
    /// </para>
    /// <para>
    /// <c>RootState</c> отдаёт сюда единственный ключ
    /// <c>WaifuRollCooldownMinutes</c>; полная копия настроек остаётся в
    /// <c>admin.RootState</c>.
    /// </para>
    /// </remarks>
    public partial class SeedLegacyWaifu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                LegacyDataSeed.Seed(
                    new[]
                    {
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "Frogs",
                            TargetSchema: "waifu",
                            TargetTable: "Frogs",
                            Columns: LegacyDataSeed.Columns(
                                "Pid",
                                "CommonName",
                                "ScientificName",
                                "Family",
                                "RussianName",
                                "ThumbnailUrl",
                                "Size",
                                "Status",
                                "Category",
                                "Habits",
                                "WhenAdded",
                                "LastOrder",
                                "OrderCount")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "Fumos",
                            TargetSchema: "waifu",
                            TargetTable: "Fumos",
                            Columns: new (string Target, string Source)[]
                            {
                                LegacyDataSeed.Column("MfcId"),
                                LegacyDataSeed.Column("Name"),
                                LegacyDataSeed.Column("Character"),
                                LegacyDataSeed.Column("CharacterTranslit"),
                                LegacyDataSeed.Column("Origin"),
                                LegacyDataSeed.Column("ThumbnailUrl"),
                                LegacyDataSeed.Column("Rating", "\"Rating\"::double precision"),
                                LegacyDataSeed.Column("RatingCount"),
                                LegacyDataSeed.Column(
                                    "WhenAdded",
                                    LegacyDataSeed.TextTimestamp("\"WhenAdded\"")),
                                LegacyDataSeed.Column(
                                    "LastOrder",
                                    LegacyDataSeed.TextTimestamp("\"LastOrder\"")),
                                LegacyDataSeed.Column("OrderCount"),
                            }
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "MikuModules",
                            TargetSchema: "waifu",
                            TargetTable: "MikuModules",
                            Columns: LegacyDataSeed.Columns(
                                "PageId",
                                "Title",
                                "JapaneseName",
                                "Designer",
                                "ThumbnailUrl",
                                "Description",
                                "Songs",
                                "WhenAdded",
                                "LastOrder",
                                "OrderCount")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "MikuMondayTracks",
                            TargetSchema: "waifu",
                            TargetTable: "MikuMondayTracks",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "Number",
                                "BaseTrackInfoId",
                                "CreatedAt")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "MikuMondayActivations",
                            TargetSchema: "waifu",
                            TargetTable: "MikuMondayActivations",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "TwitchUserId",
                                "DisplayName",
                                "MikuMondayTrackId",
                                "ActivatedAt",
                                "WeekOfYear",
                                "Year")
                        ),
                        // Родитель для Waifus.AudioId: аудио должно существовать раньше
                        // строк вайфу, которые на него ссылаются.
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "WaifuRollAudios",
                            TargetSchema: "waifu",
                            TargetTable: "WaifuRollAudios",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "Name",
                                "AudioData",
                                "FileExtension",
                                "CreatedAt")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "Waifus",
                            TargetSchema: "waifu",
                            TargetTable: "Waifus",
                            Columns: LegacyDataSeed.Columns(
                                "ShikiId",
                                "Name",
                                "Age",
                                "Anime",
                                "Manga",
                                "WhenAdded",
                                "LastOrder",
                                "OrderCount",
                                "IsPrivated",
                                "ImageUrl",
                                "AudioId")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "Husbands",
                            TargetSchema: "waifu",
                            TargetTable: "Husbands",
                            Columns: LegacyDataSeed.Columns(
                                "TwitchId",
                                "WhenOrdered",
                                "WaifuBrideId",
                                "IsPrivated",
                                "OrderCount",
                                "WaifuRollId",
                                "WhenPrivated",
                                "LastWeddingCongratulatedMonths",
                                "IsAutoHelloEnabled")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "HusbandCoolDowns",
                            TargetSchema: "waifu",
                            TargetTable: "HusbandCoolDowns",
                            Columns: LegacyDataSeed.Columns("Guid", "HusbandId", "Time")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "HusbandAutoHelloCooldowns",
                            TargetSchema: "waifu",
                            TargetTable: "HusbandAutoHelloCooldowns",
                            Columns: LegacyDataSeed.Columns("Guid", "HusbandId", "Time")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "RollCooldowns",
                            TargetSchema: "waifu",
                            TargetTable: "RollCooldowns",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "TwitchUserId",
                                "RollType",
                                "LastRollTime")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "UserFumoCollections",
                            TargetSchema: "waifu",
                            TargetTable: "UserFumoCollections",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "TwitchUserId",
                                "FumoMfcId",
                                "Count",
                                "FirstObtained",
                                "LastObtained")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "UserMikuCollections",
                            TargetSchema: "waifu",
                            TargetTable: "UserMikuCollections",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "TwitchUserId",
                                "MikuPageId",
                                "Count",
                                "FirstObtained",
                                "LastObtained")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "WaifuRollGuarantees",
                            TargetSchema: "waifu",
                            TargetTable: "WaifuRollGuarantees",
                            Columns: LegacyDataSeed.Columns(
                                "TwitchId",
                                "RollCount",
                                "LastRoll",
                                "CreatedAt",
                                "UpdatedAt")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "AutoHelloMessages",
                            TargetSchema: "waifu",
                            TargetTable: "AutoHelloMessages",
                            Columns: LegacyDataSeed.Columns("Guid", "Text", "Order")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "RootState",
                            TargetSchema: "waifu",
                            TargetTable: "RootState",
                            Columns: LegacyDataSeed.Columns(
                                "Name",
                                "Value",
                                "Description",
                                "TypeDescription"),
                            Filter: "\"Name\" = 'WaifuRollCooldownMinutes'",
                            ExpectedRows: 1
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