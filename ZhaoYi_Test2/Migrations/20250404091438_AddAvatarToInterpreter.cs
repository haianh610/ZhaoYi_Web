using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZhaoYi_Test2.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatarToInterpreter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarPath",
                table: "Interpreters",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarPath",
                table: "Interpreters");
        }
    }
}
