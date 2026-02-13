using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JenianAPI.Migrations.JenianDb
{
    /// <inheritdoc />
    public partial class UpdateEodReportAndDeliveryExtractionJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubmitedAt",
                table: "EodReports",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "EodReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "DeliveryExtractionJobs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmitedAt",
                table: "EodReports");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "EodReports");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "DeliveryExtractionJobs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
