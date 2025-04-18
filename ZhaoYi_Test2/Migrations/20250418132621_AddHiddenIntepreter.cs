using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZhaoYi_Test2.Migrations
{
    /// <inheritdoc />
    public partial class AddHiddenIntepreter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isHidden",
                table: "Interpreters",
                type: "bit",
                nullable: true,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isHidden",
                table: "Interpreters");
        }
    }
}
