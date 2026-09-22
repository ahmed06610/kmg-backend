using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentCashBoxLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MiscExpenseId",
                table: "CashBoxTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectPaymentId",
                table: "CashBoxTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierPaymentId",
                table: "CashBoxTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxTransactions_ProjectPaymentId",
                table: "CashBoxTransactions",
                column: "ProjectPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxTransactions_SupplierPaymentId",
                table: "CashBoxTransactions",
                column: "SupplierPaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxTransactions_ProjectPayments_ProjectPaymentId",
                table: "CashBoxTransactions",
                column: "ProjectPaymentId",
                principalTable: "ProjectPayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxTransactions_SupplierPayments_SupplierPaymentId",
                table: "CashBoxTransactions",
                column: "SupplierPaymentId",
                principalTable: "SupplierPayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashBoxTransactions_ProjectPayments_ProjectPaymentId",
                table: "CashBoxTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_CashBoxTransactions_SupplierPayments_SupplierPaymentId",
                table: "CashBoxTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CashBoxTransactions_ProjectPaymentId",
                table: "CashBoxTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CashBoxTransactions_SupplierPaymentId",
                table: "CashBoxTransactions");

            migrationBuilder.DropColumn(
                name: "MiscExpenseId",
                table: "CashBoxTransactions");

            migrationBuilder.DropColumn(
                name: "ProjectPaymentId",
                table: "CashBoxTransactions");

            migrationBuilder.DropColumn(
                name: "SupplierPaymentId",
                table: "CashBoxTransactions");
        }
    }
}
