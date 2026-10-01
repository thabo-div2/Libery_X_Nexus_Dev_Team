using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class FixClientDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clients_IdentityProviderSubjectId",
                table: "Clients");

            migrationBuilder.AlterColumn<string>(
                name: "IdentityProviderSubjectId",
                table: "Clients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_IdentityProviderSubjectId",
                table: "Clients",
                column: "IdentityProviderSubjectId",
                unique: true,
                filter: "[IdentityProviderSubjectId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clients_IdentityProviderSubjectId",
                table: "Clients");

            migrationBuilder.AlterColumn<string>(
                name: "IdentityProviderSubjectId",
                table: "Clients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_IdentityProviderSubjectId",
                table: "Clients",
                column: "IdentityProviderSubjectId",
                unique: true);
        }
    }
}
