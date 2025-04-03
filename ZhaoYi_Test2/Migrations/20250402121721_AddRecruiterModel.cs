using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZhaoYi_Test2.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruiterModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Interpreters_AspNetUsers_UserId",
                table: "Interpreters");

            migrationBuilder.DropIndex(
                name: "IX_Interpreters_UserId",
                table: "Interpreters");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Interpreters",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateTable(
                name: "Recruiters",
                columns: table => new
                {
                    RecruiterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecruiterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DetailedAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruiters", x => x.RecruiterId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recruiters");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Interpreters",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Interpreters_UserId",
                table: "Interpreters",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Interpreters_AspNetUsers_UserId",
                table: "Interpreters",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
