namespace Jenian.Application.Features.Payroll
{
  public interface IPublicHolidayService
  {
    bool IsPublicHoliday(DateOnly date, string state);

  }
}
