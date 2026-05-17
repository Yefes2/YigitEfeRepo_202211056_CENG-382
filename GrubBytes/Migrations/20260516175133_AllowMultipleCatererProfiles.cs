using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrubBytes.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleCatererProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CatererProfiles_UserId",
                table: "CatererProfiles");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "CatererProfiles",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatererProfiles_ApplicationUserId",
                table: "CatererProfiles",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CatererProfiles_UserId",
                table: "CatererProfiles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CatererProfiles_AspNetUsers_ApplicationUserId",
                table: "CatererProfiles",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatererProfiles_AspNetUsers_ApplicationUserId",
                table: "CatererProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CatererProfiles_ApplicationUserId",
                table: "CatererProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CatererProfiles_UserId",
                table: "CatererProfiles");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "CatererProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_CatererProfiles_UserId",
                table: "CatererProfiles",
                column: "UserId",
                unique: true);
        }
    }
}
