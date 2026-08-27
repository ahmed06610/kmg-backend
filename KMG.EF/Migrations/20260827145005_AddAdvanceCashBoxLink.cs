using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvanceCashBoxLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdvanceId",
                table: "CashBoxTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxTransactions_AdvanceId",
                table: "CashBoxTransactions",
                column: "AdvanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxTransactions_Advances_AdvanceId",
                table: "CashBoxTransactions",
                column: "AdvanceId",
                principalTable: "Advances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashBoxTransactions_Advances_AdvanceId",
                table: "CashBoxTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CashBoxTransactions_AdvanceId",
                table: "CashBoxTransactions");

            migrationBuilder.DropColumn(
                name: "AdvanceId",
                table: "CashBoxTransactions");
        }
    }
}
