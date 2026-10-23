using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionDailyRateAndPayrollPayoutEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppliedInPayrollPayoutId",
                table: "PayrollAdjustments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyRate",
                table: "MissionWorkers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // قبل التعديل ده كان الأجر اليومي للمأمورية = ضعف يومية الموظف العادية دايمًا (منطق مكتوب في الكود،
            // مش قيمة متخزنة) - عشان سجلات المأموريات القديمة متتصفرش فجأة، بنعبّي DailyRate بنفس القيمة اللي
            // كانت بتتحسب بيها فعليًا وقتها، وأي مأمورية جديدة بعد كده هتاخد القيمة اللي المستخدم هيحددها بنفسه
            migrationBuilder.Sql(@"
                UPDATE mw
                SET mw.DailyRate = 2 * e.WageAmount
                FROM MissionWorkers mw
                INNER JOIN Employees e ON e.Id = mw.EmployeeId
            ");

            migrationBuilder.CreateTable(
                name: "PayrollAdvanceCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PayrollPayoutId = table.Column<int>(type: "int", nullable: false),
                    AdvanceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAdvanceCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollAdvanceCharges_Advances_AdvanceId",
                        column: x => x.AdvanceId,
                        principalTable: "Advances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAdvanceCharges_PayrollPayouts_PayrollPayoutId",
                        column: x => x.PayrollPayoutId,
                        principalTable: "PayrollPayouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_AppliedInPayrollPayoutId",
                table: "PayrollAdjustments",
                column: "AppliedInPayrollPayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvanceCharges_AdvanceId",
                table: "PayrollAdvanceCharges",
                column: "AdvanceId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvanceCharges_PayrollPayoutId",
                table: "PayrollAdvanceCharges",
                column: "PayrollPayoutId");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollAdjustments_PayrollPayouts_AppliedInPayrollPayoutId",
                table: "PayrollAdjustments",
                column: "AppliedInPayrollPayoutId",
                principalTable: "PayrollPayouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayrollAdjustments_PayrollPayouts_AppliedInPayrollPayoutId",
                table: "PayrollAdjustments");

            migrationBuilder.DropTable(
                name: "PayrollAdvanceCharges");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAdjustments_AppliedInPayrollPayoutId",
                table: "PayrollAdjustments");

            migrationBuilder.DropColumn(
                name: "AppliedInPayrollPayoutId",
                table: "PayrollAdjustments");

            migrationBuilder.DropColumn(
                name: "DailyRate",
                table: "MissionWorkers");
        }
    }
}
