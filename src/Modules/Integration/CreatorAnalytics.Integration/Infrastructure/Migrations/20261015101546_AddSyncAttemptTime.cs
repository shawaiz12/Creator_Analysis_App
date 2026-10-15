using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreatorAnalytics.Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncAttemptTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAttemptAtUtc",
                schema: "integration",
                table: "ChannelConnections",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSyncAttemptAtUtc",
                schema: "integration",
                table: "ChannelConnections");
        }
    }
}
