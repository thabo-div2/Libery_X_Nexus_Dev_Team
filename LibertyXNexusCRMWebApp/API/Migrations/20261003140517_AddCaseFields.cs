using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdviserReviewAt",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DetailsSubmittedAt",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FicaVerifiedAt",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PolicyIssuedAt",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedToLibertyAt",
                table: "Cases",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdviserReviewAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "DetailsSubmittedAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "FicaVerifiedAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "PolicyIssuedAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "SubmittedToLibertyAt",
                table: "Cases");
        }
    }
}
