using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZhaoYi_Test2.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJobPostingWithSalaryRangeAndRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Salary",
                table: "JobPostings",
                newName: "MinSalary");

            migrationBuilder.AddColumn<bool>(
                name: "IsUrgent",
                table: "JobPostings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JobRequirements",
                table: "JobPostings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxSalary",
                table: "JobPostings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsUrgent",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "JobRequirements",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "MaxSalary",
                table: "JobPostings");

            migrationBuilder.RenameColumn(
                name: "MinSalary",
                table: "JobPostings",
                newName: "Salary");
        }
    }
}
