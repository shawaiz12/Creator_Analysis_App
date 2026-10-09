using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreatorAnalytics.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_UserId",
                schema: "identity",
                table: "TenantMemberships",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Organizations_TenantId",
                schema: "identity",
                table: "Invitations",
                column: "TenantId",
                principalSchema: "identity",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMemberships_Organizations_TenantId",
                schema: "identity",
                table: "TenantMemberships",
                column: "TenantId",
                principalSchema: "identity",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMemberships_Users_UserId",
                schema: "identity",
                table: "TenantMemberships",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Organizations_TenantId",
                schema: "identity",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantMemberships_Organizations_TenantId",
                schema: "identity",
                table: "TenantMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantMemberships_Users_UserId",
                schema: "identity",
                table: "TenantMemberships");

            migrationBuilder.DropIndex(
                name: "IX_TenantMemberships_UserId",
                schema: "identity",
                table: "TenantMemberships");
        }
    }
}
