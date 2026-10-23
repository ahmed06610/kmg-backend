using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddMisrInsuranceAndPayrollBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HourlyBonusAmount",
                table: "PayrollPayouts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyDeductionAmount",
                table: "PayrollPayouts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MisrInsuranceAmount",
                table: "PayrollPayouts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MissionDays",
                table: "PayrollPayouts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkshopDays",
                table: "PayrollPayouts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MisrInsuranceAmount",
                table: "Employees",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MisrInsurancePayoutId",
                table: "CashBoxTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MisrInsurancePayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByEmployeeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MisrInsurancePayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MisrInsurancePayouts_Employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxTransactions_MisrInsurancePayoutId",
                table: "CashBoxTransactions",
                column: "MisrInsurancePayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_MisrInsurancePayouts_CreatedByEmployeeId",
                table: "MisrInsurancePayouts",
                column: "CreatedByEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxTransactions_MisrInsurancePayouts_MisrInsurancePayoutId",
                table: "CashBoxTransactions",
                column: "MisrInsurancePayoutId",
                principalTable: "MisrInsurancePayouts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashBoxTransactions_MisrInsurancePayouts_MisrInsurancePayoutId",
                table: "CashBoxTransactions");

            migrationBuilder.DropTable(
                name: "MisrInsurancePayouts");

            migrationBuilder.DropIndex(
                name: "IX_CashBoxTransactions_MisrInsurancePayoutId",
                table: "CashBoxTransactions");

            migrationBuilder.DropColumn(
                name: "HourlyBonusAmount",
                table: "PayrollPayouts");

            migrationBuilder.DropColumn(
                name: "HourlyDeductionAmount",
                table: "PayrollPayouts");

            migrationBuilder.DropColumn(
                name: "MisrInsuranceAmount",
                table: "PayrollPayouts");

            migrationBuilder.DropColumn(
                name: "MissionDays",
                table: "PayrollPayouts");

            migrationBuilder.DropColumn(
                name: "WorkshopDays",
                table: "PayrollPayouts");

            migrationBuilder.DropColumn(
                name: "MisrInsuranceAmount",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MisrInsurancePayoutId",
                table: "CashBoxTransactions");
        }
    }
}
