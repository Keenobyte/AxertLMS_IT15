using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AxertLMS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExternalAccountStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleAccounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoogleAccounts",
                columns: table => new
                {
                    GoogleAccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AccessTokenExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EncryptedAccessToken = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    EncryptedRefreshToken = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    EnrollmentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GoogleEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    GoogleSubject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    GrantedScopes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ProfilePictureUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefreshTokenExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAccounts", x => x.GoogleAccountId);
                    table.ForeignKey(
                        name: "FK_GoogleAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAccounts_GoogleSubject",
                table: "GoogleAccounts",
                column: "GoogleSubject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAccounts_UserId",
                table: "GoogleAccounts",
                column: "UserId",
                unique: true);

        }
    }
}
