using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddCustody : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustodyId",
                table: "CashBoxTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Custodies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SettledAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SettledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Custodies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Custodies_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Custodies_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxTransactions_CustodyId",
                table: "CashBoxTransactions",
                column: "CustodyId");

            migrationBuilder.CreateIndex(
                name: "IX_Custodies_EmployeeId",
                table: "Custodies",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Custodies_ProjectId",
                table: "Custodies",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxTransactions_Custodies_CustodyId",
                table: "CashBoxTransactions",
                column: "CustodyId",
                principalTable: "Custodies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashBoxTransactions_Custodies_CustodyId",
                table: "CashBoxTransactions");

            migrationBuilder.DropTable(
                name: "Custodies");

            migrationBuilder.DropIndex(
                name: "IX_CashBoxTransactions_CustodyId",
                table: "CashBoxTransactions");

            migrationBuilder.DropColumn(
                name: "CustodyId",
                table: "CashBoxTransactions");
        }
    }
}
