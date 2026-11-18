using API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <summary>
    /// Lets the app tag the tasks it creates itself (document replacements, confirmed meetings,
    /// policy renewals) so the same event never creates a second task.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261115090000_AddTaskSourceKey")]
    public partial class AddTaskSourceKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceKey",
                table: "AdvisorTasks",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorTasks_AdvisorId_SourceKey",
                table: "AdvisorTasks",
                columns: new[] { "AdvisorId", "SourceKey" },
                unique: true,
                filter: "[SourceKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdvisorTasks_AdvisorId_SourceKey",
                table: "AdvisorTasks");

            migrationBuilder.DropColumn(
                name: "SourceKey",
                table: "AdvisorTasks");
        }
    }
}
