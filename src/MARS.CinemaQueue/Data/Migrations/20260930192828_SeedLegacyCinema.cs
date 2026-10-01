using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MARS.Shared.Data;

#nullable disable

namespace MARS.CinemaQueue.Data.Migrations
{
    /// <summary>
    /// Перенос очереди Cinema из staging-схемы <c>public</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Порядок колонок задан явно: в legacy имена идут вперемешку с
    /// <c>Title</c> без <c>NOT NULL</c>, а <c>SELECT *</c> молча переставил бы
    /// значения при любом изменении порядка в staging.
    /// </para>
    /// <para>
    /// <c>MediaUrl</c> объявлен <c>NOT NULL</c> в новой схеме, а в legacy
    /// nullable, поэтому пустое значение приводится к строке, а не к <c>NULL</c>.
    /// </para>
    /// </remarks>
    public partial class SeedLegacyCinema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                LegacyDataSeed.Seed(
                    new[]
                    {
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "CinemaQueue",
                            TargetSchema: "cinema",
                            TargetTable: "CinemaQueue",
                            Columns: new (string Target, string Source)[]
                            {
                                LegacyDataSeed.Column("Id"),
                                LegacyDataSeed.Column("Title"),
                                LegacyDataSeed.Column("Description"),
                                LegacyDataSeed.Column("MediaUrl", "coalesce(\"MediaUrl\", '')"),
                                LegacyDataSeed.Column("Status"),
                                LegacyDataSeed.Column("Priority"),
                                LegacyDataSeed.Column("CreatedAt"),
                                LegacyDataSeed.Column("ScheduledFor"),
                                LegacyDataSeed.Column("TwitchUserId"),
                                LegacyDataSeed.Column("Notes"),
                                LegacyDataSeed.Column("IsNext"),
                                LegacyDataSeed.Column("LastModified"),
                            }
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