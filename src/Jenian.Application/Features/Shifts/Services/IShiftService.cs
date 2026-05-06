using Jenian.Application.Features.Shifts.Commands;
using Jenian.Application.Features.Shifts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Features.Shifts.Services
{
  public interface IShiftService
  {

    Task<IEnumerable<ShiftDto>> CreateShiftsAsync(CreateShiftsCommand command, CancellationToken cancellationToken);

    Task<IEnumerable<ShiftDto>> UpdateShiftsAsync(UpdateShiftsCommand command, CancellationToken cancellationToken);

    Task<IEnumerable<ShiftDto>> SaveShiftsAsync(
        SaveShiftsCommand command,
        CancellationToken cancellationToken);

    Task DeleteShiftsAsync(DeleteShiftsCommand command, CancellationToken cancellationToken);

    Task<IEnumerable<ShiftDto>> GetShiftsForUserAsync(GetShiftsForUserCommand command, CancellationToken cancellationToken);




  }
}
