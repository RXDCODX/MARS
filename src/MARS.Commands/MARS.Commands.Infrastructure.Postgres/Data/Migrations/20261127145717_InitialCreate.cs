using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.Commands.Infrastructure.Postgres.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "commands");

            migrationBuilder.CreateTable(
                name: "CommandExecutions",
                schema: "commands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Platform = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CommandName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CallerId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CallerDisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    InputHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeaseUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    NeedsReview = table.Column<bool>(type: "boolean", nullable: false),
                    JobId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommandOutboxMessages",
                schema: "commands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeaseUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandOutboxMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandOutboxMessages_CommandExecutions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalSchema: "commands",
                        principalTable: "CommandExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommandExecutions_RequestId",
                schema: "commands",
                table: "CommandExecutions",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandExecutions_Status_AcceptedAtUtc",
                schema: "commands",
                table: "CommandExecutions",
                columns: new[] { "Status", "AcceptedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommandOutboxMessages_ExecutionId_MessageType",
                schema: "commands",
                table: "CommandOutboxMessages",
                columns: new[] { "ExecutionId", "MessageType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandOutboxMessages_Status_NextAttemptAtUtc",
                schema: "commands",
                table: "CommandOutboxMessages",
                columns: new[] { "Status", "NextAttemptAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandOutboxMessages",
                schema: "commands");

            migrationBuilder.DropTable(
                name: "CommandExecutions",
                schema: "commands");
        }
    }
}
