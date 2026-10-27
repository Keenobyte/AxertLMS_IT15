using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AxertLMS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sections",
                columns: table => new
                {
                    section_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    section_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    strand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    year_lvl = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.section_id);
                });

            migrationBuilder.CreateTable(
                name: "Strands",
                columns: table => new
                {
                    strand_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strand_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    strand_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strands", x => x.strand_id);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    subject_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subject_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.subject_id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    f_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    l_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumMaps",
                columns: table => new
                {
                    curriculum_map_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subject_id = table.Column<int>(type: "int", nullable: false),
                    strand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    year_lvl = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumMaps", x => x.curriculum_map_id);
                    table.ForeignKey(
                        name: "FK_CurriculumMaps_Subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "Subjects",
                        principalColumn: "subject_id");
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    student_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    section_id = table.Column<int>(type: "int", nullable: false),
                    student_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.student_id);
                    table.ForeignKey(
                        name: "FK_Students_Sections_section_id",
                        column: x => x.section_id,
                        principalTable: "Sections",
                        principalColumn: "section_id");
                    table.ForeignKey(
                        name: "FK_Students_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    teacher_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    specialization = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.teacher_id);
                    table.ForeignKey(
                        name: "FK_Teachers_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Classrooms",
                columns: table => new
                {
                    classroom_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subject_id = table.Column<int>(type: "int", nullable: false),
                    teacher_id = table.Column<int>(type: "int", nullable: false),
                    section_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classrooms", x => x.classroom_id);
                    table.ForeignKey(
                        name: "FK_Classrooms_Sections_section_id",
                        column: x => x.section_id,
                        principalTable: "Sections",
                        principalColumn: "section_id");
                    table.ForeignKey(
                        name: "FK_Classrooms_Subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "Subjects",
                        principalColumn: "subject_id");
                    table.ForeignKey(
                        name: "FK_Classrooms_Teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "Teachers",
                        principalColumn: "teacher_id");
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    announcement_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    classroom_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.announcement_id);
                    table.ForeignKey(
                        name: "FK_Announcements_Classrooms_classroom_id",
                        column: x => x.classroom_id,
                        principalTable: "Classrooms",
                        principalColumn: "classroom_id");
                    table.ForeignKey(
                        name: "FK_Announcements_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    assignment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    classroom_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    attachment_link = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    due_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxScore = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.assignment_id);
                    table.ForeignKey(
                        name: "FK_Assignments_Classrooms_classroom_id",
                        column: x => x.classroom_id,
                        principalTable: "Classrooms",
                        principalColumn: "classroom_id");
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    enrollment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    classroom_id = table.Column<int>(type: "int", nullable: false),
                    enrollment_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.enrollment_id);
                    table.ForeignKey(
                        name: "FK_Enrollments_Classrooms_classroom_id",
                        column: x => x.classroom_id,
                        principalTable: "Classrooms",
                        principalColumn: "classroom_id");
                    table.ForeignKey(
                        name: "FK_Enrollments_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "student_id");
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    module_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    classroom_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_path = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.module_id);
                    table.ForeignKey(
                        name: "FK_Modules_Classrooms_classroom_id",
                        column: x => x.classroom_id,
                        principalTable: "Classrooms",
                        principalColumn: "classroom_id");
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    submission_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    assignment_id = table.Column<int>(type: "int", nullable: false),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    file_path = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    submission_text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlagiarismPercentage = table.Column<double>(type: "float", nullable: false),
                    PlagiarismReport = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.submission_id);
                    table.ForeignKey(
                        name: "FK_Submissions_Assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "Assignments",
                        principalColumn: "assignment_id");
                    table.ForeignKey(
                        name: "FK_Submissions_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "student_id");
                });

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
                        principalColumn: "classroom_id");
                    table.ForeignKey(
                        name: "FK_Quizzes_Modules_module_id",
                        column: x => x.module_id,
                        principalTable: "Modules",
                        principalColumn: "module_id");
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    grade_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    submission_id = table.Column<int>(type: "int", nullable: false),
                    score = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    feedback = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.grade_id);
                    table.ForeignKey(
                        name: "FK_Grades_Submissions_submission_id",
                        column: x => x.submission_id,
                        principalTable: "Submissions",
                        principalColumn: "submission_id");
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
                        principalColumn: "quiz_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_classroom_id",
                table: "Announcements",
                column: "classroom_id");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_user_id",
                table: "Announcements",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_classroom_id",
                table: "Assignments",
                column: "classroom_id");

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_section_id",
                table: "Classrooms",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_subject_id",
                table: "Classrooms",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_teacher_id",
                table: "Classrooms",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumMaps_subject_id",
                table: "CurriculumMaps",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_classroom_id",
                table: "Enrollments",
                column: "classroom_id");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_student_id",
                table: "Enrollments",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_submission_id",
                table: "Grades",
                column: "submission_id");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_classroom_id",
                table: "Modules",
                column: "classroom_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_Students_section_id",
                table: "Students",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "IX_Students_user_id",
                table: "Students",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_assignment_id",
                table: "Submissions",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_student_id",
                table: "Submissions",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_user_id",
                table: "Teachers",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "CurriculumMaps");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "Grades");

            migrationBuilder.DropTable(
                name: "QuizQuestions");

            migrationBuilder.DropTable(
                name: "Strands");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropTable(
                name: "Quizzes");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "Classrooms");

            migrationBuilder.DropTable(
                name: "Sections");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
