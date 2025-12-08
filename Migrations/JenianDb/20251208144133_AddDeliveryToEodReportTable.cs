using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JenianAPI.Migrations.JenianDb
{
    /// <inheritdoc />
    public partial class AddDeliveryToEodReportTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Delivery",
                table: "EodReports",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Delivery",
                table: "EodReports");
        }
    }
}
