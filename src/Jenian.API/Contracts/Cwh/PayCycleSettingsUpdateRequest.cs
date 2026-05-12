
using Jenian.Application.Features.Shifts.Dtos;
using System.ComponentModel.DataAnnotations;

namespace Jenian.API.Contracts.Cwh
{

  public class PayCycleSettingsUpdateRequest
  {

    [Range(1, 3, ErrorMessage = "Invalid cycle period, weekly(1), fornightly(2), monthly(3)")]
    public PayCycleTypeDTO PayCycleType { get; set; }

    public required DateOnly AnchorStartDate { get; set; }
  }
}
