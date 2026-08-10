namespace Jenian.Application.Common
{
  public static class ShiftDateHelper
  {
    private const string _defaultTimeZoneId = "Australia/Melbourne";

    public static DateOnly GetWorkDate(DateTimeOffset startAt) {
      var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_defaultTimeZoneId);
      var localDateTime = TimeZoneInfo.ConvertTime(startAt, timeZone).DateTime;

      return DateOnly.FromDateTime(localDateTime);
    }

    public static DateOnly GetWorkDate(DateTimeOffset startAt, string? timeZoneId) {
      var resolvedTimeZoneId = string.IsNullOrWhiteSpace(timeZoneId)
          ? _defaultTimeZoneId
          : timeZoneId;

      var timeZone = TimeZoneInfo.FindSystemTimeZoneById(resolvedTimeZoneId);
      var localDateTime = TimeZoneInfo.ConvertTime(startAt, timeZone).DateTime;

      return DateOnly.FromDateTime(localDateTime);
    }

    public static TimeZoneInfo GetTimeZoneInfo(string timeZoneId) {
      var resolvedTimeZoneId = string.IsNullOrWhiteSpace(timeZoneId)
          ? _defaultTimeZoneId
          : timeZoneId;
      return TimeZoneInfo.FindSystemTimeZoneById(resolvedTimeZoneId);
    }

    public static DateTimeOffset ToDateTimeOffsetStartOfDay(
    DateOnly date,
    string timeZoneId) {
      var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

      var localDateTime = date.ToDateTime(TimeOnly.MinValue);

      var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(
       localDateTime,
       timeZone);

      return new DateTimeOffset(utcDateTime);
    }
  }
}
