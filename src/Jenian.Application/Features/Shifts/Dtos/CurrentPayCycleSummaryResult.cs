namespace Jenian.Application.Features.Shifts.Dtos
{
  public class CurrentPayCycleSummaryResult
  {
    public bool HasPayCycleSettings { get; set; }
    public string? PayCycle { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int ShiftCount { get; set; }
    public decimal EstimatedGrossPay { get; set; }
  }
}
