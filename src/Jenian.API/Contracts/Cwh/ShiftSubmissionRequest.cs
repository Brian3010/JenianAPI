using Jenian.Application.Features.Shifts.Dtos;

namespace Jenian.API.Contracts.Cwh
{
  public class ShiftSubmissionRequest
  {
    public List<ShiftDto> Shifts { get; set; } = [];
    public List<Guid> DeletedShiftIds { get; set; } = [];
  }


}

