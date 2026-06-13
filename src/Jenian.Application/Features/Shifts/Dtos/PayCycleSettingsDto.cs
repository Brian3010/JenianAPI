
namespace Jenian.Application.Features.Shifts.Dtos
{

  public enum PayCycleTypeDTO
  {
    Weekly = 1,
    Fortnightly = 2,
    Monthly = 3
  }
  public class PayCycleSettingsDto
  {
    public bool HasPayCycleSettings { get; set; }
    public DateOnly? AnchorStartDate { get; set; }

    public PayCycleTypeDTO? PayCycle { get; set; }

    public DateOnly? PayCycleStartDate { get; set; }
    public DateOnly? PayCycleEndDate { get; set; }

    public int? ShiftCountInCycle { get; set; } = 0;

    public decimal? EstimatedGrossPay { get; set; } = 0;

  }
}
