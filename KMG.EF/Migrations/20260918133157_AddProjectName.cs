using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMG.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // تعبئة اسم مبدئي للمشاريع الموجودة بالفعل (كود المشروع) لحد ما المستخدم يعدّله لاحقًا
            migrationBuilder.Sql("UPDATE [Projects] SET [Name] = [ProjectCode] WHERE [Name] = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Projects");
        }
    }
}
