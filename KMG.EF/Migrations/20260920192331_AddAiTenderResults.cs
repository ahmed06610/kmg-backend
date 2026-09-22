using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTenderResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiTenderResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenderTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuingEntity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceSite = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmissionDeadline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DaysUntilDeadline = table.Column<int>(type: "int", nullable: true),
                    BusinessCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsNewCategory = table.Column<bool>(type: "bit", nullable: true),
                    MatchedVia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatchReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentReadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentEntity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScopeOfWork = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialsRequired = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookletFee = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InitialInsurance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentSubmitBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiDocumentRelevant = table.Column<bool>(type: "bit", nullable: true),
                    RelevanceNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDismissed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiTenderResults", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiTenderResults_TenderId",
                table: "AiTenderResults",
                column: "TenderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiTenderResults");
        }
    }
}
