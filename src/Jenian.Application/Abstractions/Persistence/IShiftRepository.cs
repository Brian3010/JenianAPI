using Jenian.Domain.Entities;

namespace Jenian.Application.Abstractions.Persistence
{
  public interface IShiftRepository
  {
    Task AddAsync(UserShift userShift, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserShift userShift, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<UserShift> shifts, CancellationToken cancellationToken = default);

    Task<UserShift?> GetByIdForUserAsync(string userId, Guid shiftId, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserShift>> GetByIdsAndRangeAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserShift>> GetByIdsForUserAsync(string userId, IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserShift>> GetByDateAndUserAsync(string userId, DateOnly date, CancellationToken cancellationToken = default);

    Task RemoveByIdForUserAsync(string userId, Guid shiftId, CancellationToken cancellationToken = default);

    Task RemoveByIdsForUserAsync(string userId, IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default);

    /** PayCycleSetting  **/
    Task<PayCycleSetting?> GetPayCycleSettingByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdatePayCycleSettingsForUserAsync(string userId, PayCycleSetting payCycleSetting, CancellationToken cancellationToken = default);

    //Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);  
  }
}
