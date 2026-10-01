using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MARS.Shared.Data;

#nullable disable

namespace MARS.Alerts.Data.Migrations
{
    /// <summary>
    /// Перенос единственной записи <c>AdhdLayoutConfig</c> из staging-схемы
    /// <c>public</c>.
    /// </summary>
    /// <remarks>
    /// Набор «видео» на странице alerts — это единственное состояние, которое
    /// администратор настраивал руками и которое больше нигде не восстановить:
    /// ни одна другая legacy-таблица его не описывает.
    /// </remarks>
    public partial class SeedLegacyAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                LegacyDataSeed.Seed(
                    new[]
                    {
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "AdhdLayoutConfig",
                            TargetSchema: "alerts",
                            TargetTable: "AdhdLayoutConfig",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "ShowRainEffect",
                                "ShowDVDLogos",
                                "ShowBreakingNews",
                                "ShowStreamerVideo",
                                "ShowFitnessVideo",
                                "ShowGTAVideo",
                                "ShowHydraulicMobileVideo",
                                "ShowSlimeVideo",
                                "ShowMukbangVideo",
                                "ShowQuiz",
                                "ShowSurfer",
                                "ShowLOFIGirl",
                                "ShowCatisa",
                                "ShowNotifications",
                                "CreatedAt",
                                "UpdatedAt",
                                "DvdLogosCount",
                                "ShowTimer")
                        )
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