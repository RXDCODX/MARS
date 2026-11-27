using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.Commands.Infrastructure.Postgres.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDownloadJobTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArtifactKind",
                schema: "commands",
                table: "CommandExecutions",
                type: "integer",
                nullable: true
            );

            migrationBuilder.AddColumn<long>(
                name: "ArtifactSizeBytes",
                schema: "commands",
                table: "CommandExecutions",
                type: "bigint",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "MediaProfile",
                schema: "commands",
                table: "CommandExecutions",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "MediaStage",
                schema: "commands",
                table: "CommandExecutions",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "ProcessingDurationMs",
                schema: "commands",
                table: "CommandExecutions",
                type: "integer",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "Trimmed",
                schema: "commands",
                table: "CommandExecutions",
                type: "boolean",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArtifactKind",
                schema: "commands",
                table: "CommandExecutions"
            );

            migrationBuilder.DropColumn(
                name: "ArtifactSizeBytes",
                schema: "commands",
                table: "CommandExecutions"
            );

            migrationBuilder.DropColumn(
                name: "MediaProfile",
                schema: "commands",
                table: "CommandExecutions"
            );

            migrationBuilder.DropColumn(
                name: "MediaStage",
                schema: "commands",
                table: "CommandExecutions"
            );

            migrationBuilder.DropColumn(
                name: "ProcessingDurationMs",
                schema: "commands",
                table: "CommandExecutions"
            );

            migrationBuilder.DropColumn(
                name: "Trimmed",
                schema: "commands",
                table: "CommandExecutions"
            );
        }
    }
}
