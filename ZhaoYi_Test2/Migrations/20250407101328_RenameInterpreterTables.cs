using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZhaoYi_Test2.Migrations
{
    /// <inheritdoc />
    public partial class RenameInterpreterTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Educations_Interpreters_InterpreterId",
                table: "Educations");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Interpreters_InterpreterId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_Interpreters_InterpreterId",
                table: "WorkExperiences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkExperiences",
                table: "WorkExperiences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Projects",
                table: "Projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Educations",
                table: "Educations");

            migrationBuilder.RenameTable(
                name: "WorkExperiences",
                newName: "InterpreterWorkExperiences");

            migrationBuilder.RenameTable(
                name: "Projects",
                newName: "InterpreterProjects");

            migrationBuilder.RenameTable(
                name: "Educations",
                newName: "InterpreterEducations");

            migrationBuilder.RenameIndex(
                name: "IX_WorkExperiences_InterpreterId",
                table: "InterpreterWorkExperiences",
                newName: "IX_InterpreterWorkExperiences_InterpreterId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_InterpreterId",
                table: "InterpreterProjects",
                newName: "IX_InterpreterProjects_InterpreterId");

            migrationBuilder.RenameIndex(
                name: "IX_Educations_InterpreterId",
                table: "InterpreterEducations",
                newName: "IX_InterpreterEducations_InterpreterId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterpreterWorkExperiences",
                table: "InterpreterWorkExperiences",
                column: "WorkExperienceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterpreterProjects",
                table: "InterpreterProjects",
                column: "ProjectId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterpreterEducations",
                table: "InterpreterEducations",
                column: "EducationId");

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterEducations_Interpreters_InterpreterId",
                table: "InterpreterEducations",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterProjects_Interpreters_InterpreterId",
                table: "InterpreterProjects",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterWorkExperiences_Interpreters_InterpreterId",
                table: "InterpreterWorkExperiences",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterEducations_Interpreters_InterpreterId",
                table: "InterpreterEducations");

            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterProjects_Interpreters_InterpreterId",
                table: "InterpreterProjects");

            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterWorkExperiences_Interpreters_InterpreterId",
                table: "InterpreterWorkExperiences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterpreterWorkExperiences",
                table: "InterpreterWorkExperiences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterpreterProjects",
                table: "InterpreterProjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterpreterEducations",
                table: "InterpreterEducations");

            migrationBuilder.RenameTable(
                name: "InterpreterWorkExperiences",
                newName: "WorkExperiences");

            migrationBuilder.RenameTable(
                name: "InterpreterProjects",
                newName: "Projects");

            migrationBuilder.RenameTable(
                name: "InterpreterEducations",
                newName: "Educations");

            migrationBuilder.RenameIndex(
                name: "IX_InterpreterWorkExperiences_InterpreterId",
                table: "WorkExperiences",
                newName: "IX_WorkExperiences_InterpreterId");

            migrationBuilder.RenameIndex(
                name: "IX_InterpreterProjects_InterpreterId",
                table: "Projects",
                newName: "IX_Projects_InterpreterId");

            migrationBuilder.RenameIndex(
                name: "IX_InterpreterEducations_InterpreterId",
                table: "Educations",
                newName: "IX_Educations_InterpreterId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkExperiences",
                table: "WorkExperiences",
                column: "WorkExperienceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Projects",
                table: "Projects",
                column: "ProjectId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Educations",
                table: "Educations",
                column: "EducationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Educations_Interpreters_InterpreterId",
                table: "Educations",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Interpreters_InterpreterId",
                table: "Projects",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_Interpreters_InterpreterId",
                table: "WorkExperiences",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
