namespace Jenian.Application.Features.Shifts.Services
{
  public interface IPublicHolidayService
  {
    bool IsPublicHoliday(DateOnly date, string state);

  }
}
