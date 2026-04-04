using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jenian.Infrastructure.Persistence.Migrations.JenianDb
{
    /// <inheritdoc />
    public partial class Changeaddtionalstocktostringtype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StockUpdate_AdditionalStock",
                table: "EodReports",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "StockUpdate_AdditionalStock",
                table: "EodReports",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
