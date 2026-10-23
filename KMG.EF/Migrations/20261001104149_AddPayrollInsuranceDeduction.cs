using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollInsuranceDeduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceAmount",
                table: "PayrollPayouts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsuranceAmount",
                table: "PayrollPayouts");
        }
    }
}
