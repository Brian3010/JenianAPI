using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JenianAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TelegramLinkToken",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelegramUserId",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelegramLinkToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TelegramUserId",
                table: "AspNetUsers");
        }
    }
}
