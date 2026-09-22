using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddMiscExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MiscExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmployeeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiscExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MiscExpenses_Employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxTransactions_MiscExpenseId",
                table: "CashBoxTransactions",
                column: "MiscExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_MiscExpenses_CreatedByEmployeeId",
                table: "MiscExpenses",
                column: "CreatedByEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxTransactions_MiscExpenses_MiscExpenseId",
                table: "CashBoxTransactions",
                column: "MiscExpenseId",
                principalTable: "MiscExpenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashBoxTransactions_MiscExpenses_MiscExpenseId",
                table: "CashBoxTransactions");

            migrationBuilder.DropTable(
                name: "MiscExpenses");

            migrationBuilder.DropIndex(
                name: "IX_CashBoxTransactions_MiscExpenseId",
                table: "CashBoxTransactions");
        }
    }
}
