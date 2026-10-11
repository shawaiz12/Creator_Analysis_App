using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreatorAnalytics.Strategy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                schema: "strategy",
                table: "OutboxMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attempts",
                schema: "strategy",
                table: "OutboxMessages");
        }
    }
}
