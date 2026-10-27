using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreatorAnalytics.Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "integration");

            migrationBuilder.CreateTable(
                name: "ChannelConnections",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    YouTubeChannelId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConnectedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Videos",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    YouTubeVideoId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    Transcript = table.Column<string>(type: "text", nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Videos_ChannelConnections_ChannelConnectionId",
                        column: x => x.ChannelConnectionId,
                        principalSchema: "integration",
                        principalTable: "ChannelConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetentionSnapshots",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    FetchedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PointsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetentionSnapshots_Videos_VideoId",
                        column: x => x.VideoId,
                        principalSchema: "integration",
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelConnections_TenantId",
                schema: "integration",
                table: "ChannelConnections",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelConnections_TenantId_YouTubeChannelId",
                schema: "integration",
                table: "ChannelConnections",
                columns: new[] { "TenantId", "YouTubeChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetentionSnapshots_TenantId",
                schema: "integration",
                table: "RetentionSnapshots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionSnapshots_VideoId_FetchedAtUtc",
                schema: "integration",
                table: "RetentionSnapshots",
                columns: new[] { "VideoId", "FetchedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Videos_ChannelConnectionId",
                schema: "integration",
                table: "Videos",
                column: "ChannelConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_TenantId",
                schema: "integration",
                table: "Videos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_TenantId_YouTubeVideoId",
                schema: "integration",
                table: "Videos",
                columns: new[] { "TenantId", "YouTubeVideoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetentionSnapshots",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "Videos",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "ChannelConnections",
                schema: "integration");
        }
    }
}
