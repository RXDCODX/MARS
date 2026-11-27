using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.MediaStorage.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaDownloadJobCacheAndDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CacheKey",
                schema: "mediastorage",
                table: "MediaDownloadJobs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeaseUntilUtc",
                schema: "mediastorage",
                table: "MediaDownloadJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PendingDeliveryCount",
                schema: "mediastorage",
                table: "MediaDownloadJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAtUtc",
                schema: "mediastorage",
                table: "MediaDownloadJobs",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CacheKey",
                schema: "mediastorage",
                table: "MediaDownloadJobs");

            migrationBuilder.DropColumn(
                name: "LeaseUntilUtc",
                schema: "mediastorage",
                table: "MediaDownloadJobs");

            migrationBuilder.DropColumn(
                name: "PendingDeliveryCount",
                schema: "mediastorage",
                table: "MediaDownloadJobs");

            migrationBuilder.DropColumn(
                name: "StartedAtUtc",
                schema: "mediastorage",
                table: "MediaDownloadJobs");
        }
    }
}
