using Jenian.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Features.Shifts.Dtos
{
  public class ShiftDto
  {
    public Guid Id { get; set; }
    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public string TimeZoneId { get; set; } = "Australia/Melbourne";

    public int UnpaidBreakMinutes { get; set; }
    public int PaidBreakMinutes { get; set; }

    public ShiftEntryType EntryType { get; set; }
    public EmploymentType EmploymentType { get; set; }

    public ShiftSource Source { get; set; }

  }
}
