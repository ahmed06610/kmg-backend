using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeInsurance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceAmount",
                table: "Employees",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "InsurancePayoutId",
                table: "CashBoxTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InsurancePayouts",
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
                    table.PrimaryKey("PK_InsurancePayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsurancePayouts_Employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxTransactions_InsurancePayoutId",
                table: "CashBoxTransactions",
                column: "InsurancePayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePayouts_CreatedByEmployeeId",
                table: "InsurancePayouts",
                column: "CreatedByEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxTransactions_InsurancePayouts_InsurancePayoutId",
                table: "CashBoxTransactions",
                column: "InsurancePayoutId",
                principalTable: "InsurancePayouts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashBoxTransactions_InsurancePayouts_InsurancePayoutId",
                table: "CashBoxTransactions");

            migrationBuilder.DropTable(
                name: "InsurancePayouts");

            migrationBuilder.DropIndex(
                name: "IX_CashBoxTransactions_InsurancePayoutId",
                table: "CashBoxTransactions");

            migrationBuilder.DropColumn(
                name: "InsuranceAmount",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "InsurancePayoutId",
                table: "CashBoxTransactions");
        }
    }
}
