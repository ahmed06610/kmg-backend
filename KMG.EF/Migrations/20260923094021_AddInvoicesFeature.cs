using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicesFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "SupplierPayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "SupplierPayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "ProjectPayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "ProjectPayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "ProjectExpenses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "MiscExpenses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "MiscExpenses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GeneratedInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecipientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecipientAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecipientPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedInvoices_Employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeneratedInvoices_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedInvoiceLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GeneratedInvoiceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedInvoiceLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedInvoiceLineItems_GeneratedInvoices_GeneratedInvoiceId",
                        column: x => x.GeneratedInvoiceId,
                        principalTable: "GeneratedInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedInvoiceLineItems_GeneratedInvoiceId",
                table: "GeneratedInvoiceLineItems",
                column: "GeneratedInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedInvoices_CreatedByEmployeeId",
                table: "GeneratedInvoices",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedInvoices_InvoiceNumber",
                table: "GeneratedInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedInvoices_ProjectId",
                table: "GeneratedInvoices",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneratedInvoiceLineItems");

            migrationBuilder.DropTable(
                name: "GeneratedInvoices");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "ProjectPayments");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "ProjectPayments");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "ProjectExpenses");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "MiscExpenses");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "MiscExpenses");
        }
    }
}
