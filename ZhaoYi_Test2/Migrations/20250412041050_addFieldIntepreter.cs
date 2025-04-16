using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZhaoYi_Test2.Migrations
{
    /// <inheritdoc />
    public partial class addFieldIntepreter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Field",
                table: "Interpreters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Field",
                table: "Interpreters");
        }
    }
}
