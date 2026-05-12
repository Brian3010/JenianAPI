
using Jenian.Application.Features.Shifts.Dtos;

namespace Jenian.Application.Features.Shifts.Commands
{
  public class CreatePayCycleSettingsCommand
  {
    public required string UserId { get; set; }
    public required DateOnly AnchorStartDate { get; set; }
    public required PayCycleTypeDTO PayCycleType { get; set; }

  }
}
