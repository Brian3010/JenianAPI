namespace Jenian.Application.Features.PaySummaries.Dtos
{
  public class UserDailyPaySummaryDto
  {
    public DateOnly WorkDate { get; set; }

    // total minutes contributing to pay
    public int TotalPayableMinutes { get; set; }

    // aggregated from UserShift
    public int? TotalPaidBreakMinutes { get; set; }
    public int TotalUnpaidBreakMinutes { get; set; }

    // calculation buckets
    public int TotalEveningPenaltyMinutes { get; set; }
    public int TotalOvertimeMinutes { get; set; }

    public decimal BaseRateUsed { get; set; }
    public decimal GrossPay { get; set; }
    public required string UserId { get; set; }
  }
}
