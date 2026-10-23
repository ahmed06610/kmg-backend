using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseTransportCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TransportCost",
                table: "StockMovements",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StockMovementId",
                table: "CashBoxTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxTransactions_StockMovementId",
                table: "CashBoxTransactions",
                column: "StockMovementId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxTransactions_StockMovements_StockMovementId",
                table: "CashBoxTransactions",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashBoxTransactions_StockMovements_StockMovementId",
                table: "CashBoxTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CashBoxTransactions_StockMovementId",
                table: "CashBoxTransactions");

            migrationBuilder.DropColumn(
                name: "TransportCost",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "StockMovementId",
                table: "CashBoxTransactions");
        }
    }
}
