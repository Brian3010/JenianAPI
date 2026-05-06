using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jenian.Infrastructure.Persistence.Migrations.JenianDb
  {
  /// <inheritdoc />
  public partial class AddUserShiftDailyPaySummary : Migration
    {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
      {
      migrationBuilder.CreateTable(
          name: "UserDailyPaySummaries",
          columns: table => new
            {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
            TotalPayableMinutes = table.Column<int>(type: "int", nullable: false),
            TotalPaidBreakMinutes = table.Column<int>(type: "int", nullable: false),
            TotalUnpaidBreakMinutes = table.Column<int>(type: "int", nullable: false),
            TotalEveningPenaltyMinutes = table.Column<int>(type: "int", nullable: false),
            TotalOvertimeMinutes = table.Column<int>(type: "int", nullable: false),
            BaseRateUsed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
            GrossPay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
            CalculatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
            },
          constraints: table =>
          {
            table.PrimaryKey("PK_UserDailyPaySummaries", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "UserShifts",
          columns: table => new
            {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            StartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            EndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            TimeZoneId = table.Column<string>(type: "nvarchar(max)", nullable: false),
            UnpaidBreakMinutes = table.Column<int>(type: "int", nullable: false),
            PaidBreakMinutes = table.Column<int>(type: "int", nullable: false),
            EntryType = table.Column<int>(type: "int", nullable: false),
            EmploymentType = table.Column<int>(type: "int", nullable: false),
            Source = table.Column<int>(type: "int", nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
            UserDailyPaySummaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
          constraints: table =>
          {
            table.PrimaryKey("PK_UserShifts", x => x.Id);
            table.ForeignKey(
                      name: "FK_UserShifts_UserDailyPaySummaries_UserDailyPaySummaryId",
                      column: x => x.UserDailyPaySummaryId,
                      principalTable: "UserDailyPaySummaries",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.SetNull);
          });

      migrationBuilder.CreateIndex(
          name: "IX_UserDailyPaySummaries_UserId_WorkDate",
          table: "UserDailyPaySummaries",
          columns: new[] { "UserId", "WorkDate" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_UserShifts_UserDailyPaySummaryId",
          table: "UserShifts",
          column: "UserDailyPaySummaryId");
      }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
      {
      migrationBuilder.DropTable(
          name: "UserShifts");

      migrationBuilder.DropTable(
          name: "UserDailyPaySummaries");
      }
    }
  }
