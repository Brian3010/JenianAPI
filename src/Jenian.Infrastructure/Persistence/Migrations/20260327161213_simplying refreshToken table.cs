using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jenian.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class simplyingrefreshTokentable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceIpAddress",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "RefreshTokens");

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "RefreshTokens",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "RefreshTokens");

            migrationBuilder.AddColumn<string>(
                name: "DeviceIpAddress",
                table: "RefreshTokens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "RefreshTokens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
