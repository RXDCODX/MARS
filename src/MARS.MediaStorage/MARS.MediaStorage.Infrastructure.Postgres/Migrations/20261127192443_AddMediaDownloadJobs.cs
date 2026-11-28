using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.MediaStorage.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaDownloadJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaDownloadJobs",
                schema: "mediastorage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    ExecutionId = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SourceKind = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    FailureCategory = table.Column<string>(type: "text", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    ArtifactUrl = table.Column<string>(type: "text", nullable: true),
                    ArtifactFileName = table.Column<string>(type: "text", nullable: true),
                    ArtifactMimeType = table.Column<string>(type: "text", nullable: true),
                    ArtifactKind = table.Column<int>(type: "integer", nullable: false),
                    ArtifactSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ArtifactDurationSeconds = table.Column<double>(
                        type: "double precision",
                        nullable: false
                    ),
                    ArtifactWidth = table.Column<int>(type: "integer", nullable: false),
                    ArtifactHeight = table.Column<int>(type: "integer", nullable: false),
                    Trimmed = table.Column<bool>(type: "boolean", nullable: false),
                    Caption = table.Column<string>(type: "text", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ProcessingTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: true),
                    Profile = table.Column<string>(type: "text", nullable: true),
                    ArtifactStoragePath = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    CompletedAtUtc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaDownloadJobs", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MediaDownloadJobs_CreatedAtUtc",
                schema: "mediastorage",
                table: "MediaDownloadJobs",
                column: "CreatedAtUtc"
            );

            migrationBuilder.CreateIndex(
                name: "IX_MediaDownloadJobs_Status",
                schema: "mediastorage",
                table: "MediaDownloadJobs",
                column: "Status"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MediaDownloadJobs", schema: "mediastorage");
        }
    }
}
