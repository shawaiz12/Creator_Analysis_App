using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreatorAnalytics.Strategy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "strategy");

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "strategy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategyDocuments",
                schema: "strategy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GenerationRequests",
                schema: "strategy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PromptVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenerationRequests_StrategyDocuments_StrategyDocumentId",
                        column: x => x.StrategyDocumentId,
                        principalSchema: "strategy",
                        principalTable: "StrategyDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrategyReviews",
                schema: "strategy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategyReviews_StrategyDocuments_StrategyDocumentId",
                        column: x => x.StrategyDocumentId,
                        principalSchema: "strategy",
                        principalTable: "StrategyDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrategyRevisions",
                schema: "strategy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Origin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategyRevisions_StrategyDocuments_StrategyDocumentId",
                        column: x => x.StrategyDocumentId,
                        principalSchema: "strategy",
                        principalTable: "StrategyDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GenerationRequests_Status_NextAttemptAtUtc",
                schema: "strategy",
                table: "GenerationRequests",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GenerationRequests_StrategyDocumentId",
                schema: "strategy",
                table: "GenerationRequests",
                column: "StrategyDocumentId",
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationRequests_TenantId",
                schema: "strategy",
                table: "GenerationRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyDocuments_TenantId",
                schema: "strategy",
                table: "StrategyDocuments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyReviews_StrategyDocumentId",
                schema: "strategy",
                table: "StrategyReviews",
                column: "StrategyDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyReviews_TenantId",
                schema: "strategy",
                table: "StrategyReviews",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyRevisions_StrategyDocumentId_VersionNumber",
                schema: "strategy",
                table: "StrategyRevisions",
                columns: new[] { "StrategyDocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategyRevisions_TenantId",
                schema: "strategy",
                table: "StrategyRevisions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GenerationRequests",
                schema: "strategy");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "strategy");

            migrationBuilder.DropTable(
                name: "StrategyReviews",
                schema: "strategy");

            migrationBuilder.DropTable(
                name: "StrategyRevisions",
                schema: "strategy");

            migrationBuilder.DropTable(
                name: "StrategyDocuments",
                schema: "strategy");
        }
    }
}
