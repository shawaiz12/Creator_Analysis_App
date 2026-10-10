using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreatorAnalytics.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                schema: "identity",
                table: "Invitations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "InvitedByUserId",
                schema: "identity",
                table: "Invitations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_InvitedByUserId",
                schema: "identity",
                table: "Invitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TenantId_Email",
                schema: "identity",
                table: "Invitations",
                columns: new[] { "TenantId", "Email" },
                unique: true,
                filter: "[Status] = 'Pending'");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Users_InvitedByUserId",
                schema: "identity",
                table: "Invitations",
                column: "InvitedByUserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Users_InvitedByUserId",
                schema: "identity",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_InvitedByUserId",
                schema: "identity",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_TenantId_Email",
                schema: "identity",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                schema: "identity",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "InvitedByUserId",
                schema: "identity",
                table: "Invitations");
        }
    }
}
