using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectPaymentCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckDueDate",
                table: "ProjectPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckStatus",
                table: "ProjectPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCheck",
                table: "ProjectPayments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckDueDate",
                table: "ProjectPayments");

            migrationBuilder.DropColumn(
                name: "CheckStatus",
                table: "ProjectPayments");

            migrationBuilder.DropColumn(
                name: "IsCheck",
                table: "ProjectPayments");
        }
    }
}
