using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AxertLMS.Data.Migrations
{
    public partial class AddDepEdGradingFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ONLY add our two new columns to the Assignments table
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "WW");

            migrationBuilder.AddColumn<int>(
                name: "MaxScore",
                table: "Assignments",
                type: "int",
                nullable: false,
                defaultValue: 100);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove them if we ever need to undo
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "Assignments");
        }
    }
}