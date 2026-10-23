using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustodyMiscExpenseCashCredit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "MiscExpenses",
                newName: "AmountCash");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Custodies",
                newName: "AmountCash");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountCredit",
                table: "MiscExpenses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountCredit",
                table: "Custodies",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountCredit",
                table: "MiscExpenses");

            migrationBuilder.DropColumn(
                name: "AmountCredit",
                table: "Custodies");

            migrationBuilder.RenameColumn(
                name: "AmountCash",
                table: "MiscExpenses",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "AmountCash",
                table: "Custodies",
                newName: "Amount");
        }
    }
}
