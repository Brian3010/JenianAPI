using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jenian.Infrastructure.Persistence.Migrations.JenianDb
{
    /// <inheritdoc />
    public partial class AddUniqueIndexForUserShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserShifts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_UserShifts_UserId_StartAt_EndAt",
                table: "UserShifts",
                columns: new[] { "UserId", "StartAt", "EndAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserShifts_UserId_StartAt_EndAt",
                table: "UserShifts");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserShifts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
