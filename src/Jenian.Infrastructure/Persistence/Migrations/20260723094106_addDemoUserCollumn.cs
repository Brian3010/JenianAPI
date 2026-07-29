using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jenian.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addDemoUserCollumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DemoCreatedAtUtc",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DemoExpiresAtUtc",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DemoStatus",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemoUser",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IsDemoUser_DemoExpiresAtUtc",
                table: "AspNetUsers",
                columns: new[] { "IsDemoUser", "DemoExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IsDemoUser_DemoExpiresAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DemoCreatedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DemoExpiresAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DemoStatus",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsDemoUser",
                table: "AspNetUsers");
        }
    }
}
