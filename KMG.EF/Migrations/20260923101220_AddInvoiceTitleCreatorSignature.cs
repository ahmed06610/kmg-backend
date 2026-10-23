using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceTitleCreatorSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatorDisplayName",
                table: "GeneratedInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowCreatorName",
                table: "GeneratedInvoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowSignature",
                table: "GeneratedInvoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "GeneratedInvoices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatorDisplayName",
                table: "GeneratedInvoices");

            migrationBuilder.DropColumn(
                name: "ShowCreatorName",
                table: "GeneratedInvoices");

            migrationBuilder.DropColumn(
                name: "ShowSignature",
                table: "GeneratedInvoices");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "GeneratedInvoices");
        }
    }
}
