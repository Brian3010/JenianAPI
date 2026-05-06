using Jenian.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Abstractions.Persistence
{
  public interface IShiftRepository
  {
    Task<UserShift> AddAsync(UserShift userShift, CancellationToken cancellationToken = default);

    Task<UserShift> UpdateAsync(UserShift userShift, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserShift>> AddRangeAsync(IEnumerable<UserShift> shifts, CancellationToken cancellationToken = default);

    Task<UserShift?> GetByIdForUserAsync(string userId, Guid shiftId, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserShift>> GetByIdsForUserAsync(string userId, IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserShift>> GetByUserAndDateRangeAsync(string userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    Task RemoveByIdForUserAsync(string userId, Guid shiftId, CancellationToken cancellationToken = default);

    Task RemoveByIdsForUserAsync(string userId, IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
  }
}
