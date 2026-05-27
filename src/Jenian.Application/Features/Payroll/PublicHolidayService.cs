namespace Jenian.Application.Features.Payroll
{
  public class PublicHolidayService : IPublicHolidayService
  {
    //note: hardcoded for now, will later be replaced by API calls to a public holiday service or database

    private static readonly HashSet<DateOnly> VictorianPublicHolidays = [
          new DateOnly(2026, 1, 1),
          new DateOnly(2026, 1, 26),
          new DateOnly(2026, 3, 9),
          new DateOnly(2026, 4, 3),
          new DateOnly(2026, 4, 4),
          new DateOnly(2026, 4, 5),
          new DateOnly(2026, 4, 6),
          new DateOnly(2026, 4, 25),
          new DateOnly(2026, 6, 8),
          new DateOnly(2026, 9, 25),
          new DateOnly(2026, 11, 3),
          new DateOnly(2026, 12, 25),
          new DateOnly(2026, 12, 26),
          new DateOnly(2026, 12, 28),
      ];

    public bool IsPublicHoliday(DateOnly date, string state) {
      if (state != "VIC") {
        return false;
      }
      return VictorianPublicHolidays.Contains(date);
    }
  }
}
