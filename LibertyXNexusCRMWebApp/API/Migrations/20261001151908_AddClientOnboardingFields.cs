using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddClientOnboardingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Clients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Dependants",
                table: "Clients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Employer",
                table: "Clients",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmploymentStatus",
                table: "Clients",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ExistingInvestments",
                table: "Clients",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossMonthlyIncome",
                table: "Clients",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvestmentHorizonYears",
                table: "Clients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaritalStatus",
                table: "Clients",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyExpenses",
                table: "Clients",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetMonthlyIncome",
                table: "Clients",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                table: "Clients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingDebt",
                table: "Clients",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PopiaConsent",
                table: "Clients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PopiaConsentAt",
                table: "Clients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryGoal",
                table: "Clients",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PropertyValue",
                table: "Clients",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResidentialAddress",
                table: "Clients",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RetirementSavings",
                table: "Clients",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceOfFunds",
                table: "Clients",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "Clients",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TargetSubmissionDate",
                table: "Cases",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Dependants",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Employer",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "EmploymentStatus",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ExistingInvestments",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "GrossMonthlyIncome",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "InvestmentHorizonYears",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "MonthlyExpenses",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "NetMonthlyIncome",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Occupation",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "OutstandingDebt",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PopiaConsent",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PopiaConsentAt",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PrimaryGoal",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PropertyValue",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ResidentialAddress",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "RetirementSavings",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "SourceOfFunds",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "TargetSubmissionDate",
                table: "Cases");
        }
    }
}
