namespace Jenian.Domain.Entities
{
  public class UserDailyPaySummary
  {
    public Guid Id { get; private set; } = Guid.NewGuid();


    // one user + one local day
    public DateOnly WorkDate { get; set; }

    // total minutes contributing to pay
    public int TotalPayableMinutes { get; set; }

    // aggregated from UserShift
    public int TotalPaidBreakMinutes { get; set; }
    public int TotalUnpaidBreakMinutes { get; set; }

    // calculation buckets
    public int TotalEveningPenaltyMinutes { get; set; }
    public int TotalOvertimeMinutes { get; set; }

    public decimal BaseRateUsed { get; set; }
    public decimal GrossPay { get; set; }

    public DateTimeOffset CalculatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public required string UserId { get; set; }
    public ICollection<UserShift> Shifts { get; set; } = new List<UserShift>();
  }
}
