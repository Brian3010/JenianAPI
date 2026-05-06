using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Domain.Entities
{

  public enum ShiftEntryType
  {
    Worked = 1,
    PaidNonWorked = 2,
    Leave = 3,
  }

  public enum EmploymentType
  {
    FullTime = 1,
    PartTime = 2,
    Casual = 3
  }

  public enum ShiftSource
  {
    Manual = 1,
    OCR = 2,
    Telegram = 3,
    CsvImport = 4,
    ApiImport = 5
  }
  public class UserShift
  {
    public Guid Id { get; private set; } = Guid.NewGuid();

    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string TimeZoneId { get; set; } = "Australia/Melbourne";

    public int UnpaidBreakMinutes { get; set; }
    public int PaidBreakMinutes { get; set; }

    public ShiftEntryType EntryType { get; set; } = ShiftEntryType.Worked;
    public EmploymentType EmploymentType { get; set; }

    public ShiftSource Source { get; set; } = ShiftSource.Manual;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public required string UserId { get; set; }
    public Guid? UserDailyPaySummaryId { get; set; }
    public UserDailyPaySummary? UserDailyPaySummary { get; set; }

  }
}
