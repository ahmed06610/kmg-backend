using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTaxInsuranceGuaranteePercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // مقصود DropColumn + AddColumn جديد (مش RenameColumn) رغم إن الأداة اقترحت rename تلقائي -
            // العمودين القدام كانوا قيم مطلقة (جنيه) والجداد نسب (%)، فأي rename كان هيفسّر القيم
            // القديمة غلط تمامًا (مثلًا تأمين قديم 5000 جنيه هيبقى 5000% لو اتعمله rename لعمود النسبة)
            migrationBuilder.DropColumn(
                name: "TenderInsuranceAmount",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TenderTaxAmount",
                table: "Projects");

            migrationBuilder.AddColumn<DateTime>(
                name: "InsuranceDueDate",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InsuranceRecovered",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TenderInsurancePercent",
                table: "Projects",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TenderTaxPercent",
                table: "Projects",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkGuaranteePercent",
                table: "Projects",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WorkGuaranteeDueDate",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WorkGuaranteeRecovered",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsuranceDueDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "InsuranceRecovered",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TenderInsurancePercent",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TenderTaxPercent",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "WorkGuaranteePercent",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "WorkGuaranteeDueDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "WorkGuaranteeRecovered",
                table: "Projects");

            migrationBuilder.AddColumn<decimal>(
                name: "TenderInsuranceAmount",
                table: "Projects",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TenderTaxAmount",
                table: "Projects",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
