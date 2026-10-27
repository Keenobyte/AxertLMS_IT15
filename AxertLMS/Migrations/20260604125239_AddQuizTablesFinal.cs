using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AxertLMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizTablesFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quizzes",
                columns: table => new
                {
                    quiz_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    classroom_id = table.Column<int>(type: "int", nullable: false),
                    module_id = table.Column<int>(type: "int", nullable: true),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    difficulty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.quiz_id);
                    table.ForeignKey(
                        name: "FK_Quizzes_Classrooms_classroom_id",
                        column: x => x.classroom_id,
                        principalTable: "Classrooms",
                        principalColumn: "classroom_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Quizzes_Modules_module_id",
                        column: x => x.module_id,
                        principalTable: "Modules",
                        principalColumn: "module_id");
                });

            migrationBuilder.CreateTable(
                name: "QuizQuestions",
                columns: table => new
                {
                    question_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    quiz_id = table.Column<int>(type: "int", nullable: false),
                    question_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    question_text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    option_a = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    option_b = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    option_c = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    option_d = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    correct_answer = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuestions", x => x.question_id);
                    table.ForeignKey(
                        name: "FK_QuizQuestions_Quizzes_quiz_id",
                        column: x => x.quiz_id,
                        principalTable: "Quizzes",
                        principalColumn: "quiz_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_quiz_id",
                table: "QuizQuestions",
                column: "quiz_id");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_classroom_id",
                table: "Quizzes",
                column: "classroom_id");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_module_id",
                table: "Quizzes",
                column: "module_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuizQuestions");

            migrationBuilder.DropTable(
                name: "Quizzes");
        }
    }
}
