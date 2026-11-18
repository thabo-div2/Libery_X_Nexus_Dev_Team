using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class ScopeDocumentRequestsToPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PolicyId",
                table: "Documents",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "PolicyId",
                table: "DocumentRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRequests_PolicyId",
                table: "DocumentRequests",
                column: "PolicyId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentRequests_Policies_PolicyId",
                table: "DocumentRequests",
                column: "PolicyId",
                principalTable: "Policies",
                principalColumn: "PolicyId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do not silently attach general documents to an arbitrary policy on rollback.
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [Documents] WHERE [PolicyId] IS NULL) THROW 50000, 'Assign general documents to a policy before rolling back this migration.', 1;");
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentRequests_Policies_PolicyId",
                table: "DocumentRequests");

            migrationBuilder.DropIndex(
                name: "IX_DocumentRequests_PolicyId",
                table: "DocumentRequests");

            migrationBuilder.DropColumn(
                name: "PolicyId",
                table: "DocumentRequests");

            migrationBuilder.AlterColumn<int>(
                name: "PolicyId",
                table: "Documents",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
