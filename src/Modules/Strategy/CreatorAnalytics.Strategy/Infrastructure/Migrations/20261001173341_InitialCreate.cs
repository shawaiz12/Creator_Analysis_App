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
                name: "StrategyDocuments",
                schema: "strategy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VideoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategyReviews",
                schema: "strategy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrategyDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrategyDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
