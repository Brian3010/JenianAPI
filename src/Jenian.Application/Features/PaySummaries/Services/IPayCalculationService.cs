namespace Jenian.Application.Features.PaySummaries.Services
{

  public interface IPayCalculationService
  {
    Task RecalculateForDatesAsync(string userId, HashSet<DateOnly> affectedWorkDates, CancellationToken cancellationToken);

  }
}
