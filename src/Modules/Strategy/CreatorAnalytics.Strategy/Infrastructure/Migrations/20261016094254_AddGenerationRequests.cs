using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreatorAnalytics.Strategy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGenerationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GenerationRequest_StrategyDocuments_StrategyDocumentId",
                schema: "strategy",
                table: "GenerationRequest");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GenerationRequest",
                schema: "strategy",
                table: "GenerationRequest");

            migrationBuilder.DropIndex(
                name: "IX_GenerationRequest_StrategyDocumentId",
                schema: "strategy",
                table: "GenerationRequest");

            migrationBuilder.RenameTable(
                name: "GenerationRequest",
                schema: "strategy",
                newName: "GenerationRequests",
                newSchema: "strategy");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "strategy",
                table: "GenerationRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "PromptVersion",
                schema: "strategy",
                table: "GenerationRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                schema: "strategy",
                table: "GenerationRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Error",
                schema: "strategy",
                table: "GenerationRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GenerationRequests",
                schema: "strategy",
                table: "GenerationRequests",
                column: "Id");

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
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationRequests_TenantId",
                schema: "strategy",
                table: "GenerationRequests",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_GenerationRequests_StrategyDocuments_StrategyDocumentId",
                schema: "strategy",
                table: "GenerationRequests",
                column: "StrategyDocumentId",
                principalSchema: "strategy",
                principalTable: "StrategyDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GenerationRequests_StrategyDocuments_StrategyDocumentId",
                schema: "strategy",
                table: "GenerationRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GenerationRequests",
                schema: "strategy",
                table: "GenerationRequests");

            migrationBuilder.DropIndex(
                name: "IX_GenerationRequests_Status_NextAttemptAtUtc",
                schema: "strategy",
                table: "GenerationRequests");

            migrationBuilder.DropIndex(
                name: "IX_GenerationRequests_StrategyDocumentId",
                schema: "strategy",
                table: "GenerationRequests");

            migrationBuilder.DropIndex(
                name: "IX_GenerationRequests_TenantId",
                schema: "strategy",
                table: "GenerationRequests");

            migrationBuilder.RenameTable(
                name: "GenerationRequests",
                schema: "strategy",
                newName: "GenerationRequest",
                newSchema: "strategy");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "strategy",
                table: "GenerationRequest",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PromptVersion",
                schema: "strategy",
                table: "GenerationRequest",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                schema: "strategy",
                table: "GenerationRequest",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Error",
                schema: "strategy",
                table: "GenerationRequest",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GenerationRequest",
                schema: "strategy",
                table: "GenerationRequest",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationRequest_StrategyDocumentId",
                schema: "strategy",
                table: "GenerationRequest",
                column: "StrategyDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_GenerationRequest_StrategyDocuments_StrategyDocumentId",
                schema: "strategy",
                table: "GenerationRequest",
                column: "StrategyDocumentId",
                principalSchema: "strategy",
                principalTable: "StrategyDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
