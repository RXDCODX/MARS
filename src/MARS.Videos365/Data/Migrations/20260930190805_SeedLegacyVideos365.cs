using System;
using MARS.Shared.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.Videos365.Data.Migrations
{
    /// <summary>
    /// Перенос 371 записи <c>Videos365</c> из staging-схемы <c>public</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// В legacy-базе <c>DateUpload</c> был <c>NOT NULL</c>, и незаполненное значение
    /// записывалось как <c>-infinity</c>. В новой схеме колонка nullable, поэтому
    /// бесконечность переводится в <c>NULL</c>: «дата неизвестна» точнее, чем дата
    /// за пределами календаря.
    /// </para>
    /// <para>
    /// Уникального индекса по <c>SiteId</c> в legacy не было, а модель его требует.
    /// Если в staging есть дубли, перенос падает до вставки, а не после неё:
    /// так ни одна строка не успевает записаться «наполовину».
    /// </para>
    /// </remarks>
    public partial class SeedLegacyVideos365 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                LegacyDataSeed.Seed(
                    new[]
                    {
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "Videos365",
                            TargetSchema: "videos365",
                            TargetTable: "Videos365",
                            Columns: new (string Target, string Source)[]
                            {
                                LegacyDataSeed.Column("Id"),
                                LegacyDataSeed.Column("SiteId"),
                                LegacyDataSeed.Column("Title"),
                                LegacyDataSeed.Column("PlayerUrl"),
                                LegacyDataSeed.Column("DirectLinkUrl"),
                                LegacyDataSeed.Column("Description"),
                                LegacyDataSeed.Column("DownloadUrl"),
                                LegacyDataSeed.Column(
                                    "DateUpload",
                                    LegacyDataSeed.DateOrNull("\"DateUpload\"")
                                ),
                                LegacyDataSeed.Column("Duration", "\"Duration\"::interval"),
                                LegacyDataSeed.Column("TelegramMessageId"),
                                LegacyDataSeed.Column("IsUploaded"),
                                LegacyDataSeed.Column("VideoHeight"),
                                LegacyDataSeed.Column("VideoWidth"),
                            },
                            UniqueSourceColumns: new[] { "SiteId" }
                        ),
                    }
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
