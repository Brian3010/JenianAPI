using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JenianAPI.Migrations.JenianDb
{
    /// <inheritdoc />
    public partial class AddEodReportTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "DeliveryExtractionJobs");

            migrationBuilder.DropColumn(
                name: "InputLocation",
                table: "DeliveryExtractionJobs");

            migrationBuilder.DropColumn(
                name: "InputPayloadJson",
                table: "DeliveryExtractionJobs");

            migrationBuilder.RenameColumn(
                name: "ResultJson",
                table: "DeliveryExtractionJobs",
                newName: "Result");

            migrationBuilder.CreateTable(
                name: "EodReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockUpdate_TrolleyOfStock = table.Column<int>(type: "int", nullable: false),
                    StockUpdate_StockNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StockUpdate_TrolleyOfCosmetics = table.Column<int>(type: "int", nullable: false),
                    StockUpdate_CosmeticNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StockUpdate_TrolleyofFragrances = table.Column<int>(type: "int", nullable: false),
                    StockUpdate_FragranceNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StockUpdate_AdditionalStock = table.Column<int>(type: "int", nullable: true),
                    StockUpdate_AdditionalNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NightTasks_DispLedge = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NightTasks_Gondolas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NightTasks_Mesh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NightTasks_Tills = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NightTasks_ClipStrips = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NightTasks_Podiums = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NightTasks_LowLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NightTasks_FloorStack = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NightTasks_TopSellers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NightTasks_BatWings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NightTasks_Sunglasses = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NightTasks_Catalogue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AislesFacing_FrontCounter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AislesFacing_FemHygSummer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AislesFacing_Haircare = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AislesFacing_Skincare = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AislesFacing_Vitamins = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AislesFacing_PSA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AislesFacing_Backwall = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AislesFacing_SportNutritions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AislesFacing_BabyFirstAid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AislesFacing_Cosmetics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AislesFacing_Fragrances = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cleaning_BinRun = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cleaning_Sweeping = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cleaning_TeaRoom = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cleaning_ConsultingRoom = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneralCheck_FreeTrolleys = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneralCheck_FreeCages = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneralCheck_NumOfClickCollect = table.Column<int>(type: "int", nullable: false),
                    GeneralCheck_NumOfCataBundle = table.Column<int>(type: "int", nullable: false),
                    GeneralCheck_NumOfMagaBundle = table.Column<int>(type: "int", nullable: false),
                    GeneralCheck_NumOfMyPals = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneralCheck_NumOfFragKeys = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneralCheck_NumOfLiftPasses = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneralCheck_NumOfAugmodos = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EodReports", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EodReports");

            migrationBuilder.RenameColumn(
                name: "Result",
                table: "DeliveryExtractionJobs",
                newName: "ResultJson");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "DeliveryExtractionJobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InputLocation",
                table: "DeliveryExtractionJobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InputPayloadJson",
                table: "DeliveryExtractionJobs",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
