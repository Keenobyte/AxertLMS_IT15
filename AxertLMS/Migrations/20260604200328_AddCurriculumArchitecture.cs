using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AxertLMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurriculumArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. ADD THIS LINE TO KILL THE CONSTRAINT FIRST
            migrationBuilder.Sql("ALTER TABLE Sections DROP CONSTRAINT CK__Sections__strand__4F7CD00D");

            // 2. NOW IT CAN SAFELY DROP THE COLUMN
            migrationBuilder.DropColumn(
                name: "strand",
                table: "Sections");

            migrationBuilder.AddColumn<int>(
                name: "strand_id",
                table: "Sections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                 name: "Strands",
                 columns: table => new
                 {
                     strand_id = table.Column<int>(type: "int", nullable: false)
                         .Annotation("SqlServer:Identity", "1, 1"),
                     strand_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                     description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                 },
                 constraints: table =>
                 {
                     table.PrimaryKey("PK_Strands", x => x.strand_id);
                 });
            // This forces SQL to accept an ID of 0 and creates a placeholder for your existing sections
            migrationBuilder.Sql("SET IDENTITY_INSERT Strands ON; INSERT INTO Strands (strand_id, strand_code, description) VALUES (0, 'N/A', 'Legacy Unassigned Strand'); SET IDENTITY_INSERT Strands OFF;");
            migrationBuilder.CreateTable(
                name: "CurriculumMaps",
                columns: table => new
                {
                    map_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subject_id = table.Column<int>(type: "int", nullable: false),
                    is_core_subject = table.Column<bool>(type: "bit", nullable: false),
                    strand_id = table.Column<int>(type: "int", nullable: true),
                    year_lvl = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumMaps", x => x.map_id);
                    table.ForeignKey(
                        name: "FK_CurriculumMaps_Strands_strand_id",
                        column: x => x.strand_id,
                        principalTable: "Strands",
                        principalColumn: "strand_id");
                    table.ForeignKey(
                        name: "FK_CurriculumMaps_Subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "Subjects",
                        principalColumn: "subject_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sections_strand_id",
                table: "Sections",
                column: "strand_id");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumMaps_strand_id",
                table: "CurriculumMaps",
                column: "strand_id");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumMaps_subject_id",
                table: "CurriculumMaps",
                column: "subject_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sections_Strands_strand_id",
                table: "Sections",
                column: "strand_id",
                principalTable: "Strands",
                principalColumn: "strand_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sections_Strands_strand_id",
                table: "Sections");

            migrationBuilder.DropTable(
                name: "CurriculumMaps");

            migrationBuilder.DropTable(
                name: "Strands");

            migrationBuilder.DropIndex(
                name: "IX_Sections_strand_id",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "strand_id",
                table: "Sections");

            migrationBuilder.AddColumn<string>(
                name: "strand",
                table: "Sections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
