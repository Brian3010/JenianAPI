namespace Jenian.Application.Common
{
  public static class ShiftDateHelper
  {
    private const string _defaultTimeZoneId = "Australia/Melbourne";
    private static readonly TimeSpan _maximumTimeZoneOffset = TimeSpan.FromHours(14);

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

    public static (DateTimeOffset FromUtc, DateTimeOffset ToUtc) GetUtcCandidateRange(
      DateOnly from,
      DateOnly to) {
      if (to < from) {
        throw new ArgumentOutOfRangeException(nameof(to), "The end date must not be earlier than the start date.");
      }

      // A local calendar date can be represented by an instant up to 14 hours
      // either side of UTC. Repositories narrow by this range in SQL, then apply
      // each shift's actual TimeZoneId in memory.
      var fromUtc = new DateTimeOffset(
        from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)) - _maximumTimeZoneOffset;
      var toUtc = new DateTimeOffset(
        to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)) + _maximumTimeZoneOffset;

      return (fromUtc, toUtc);
    }

    public static bool IsWorkDateInRange(
      DateTimeOffset startAt,
      string? timeZoneId,
      DateOnly from,
      DateOnly to) {
      var workDate = GetWorkDate(startAt, timeZoneId);
      return workDate >= from && workDate <= to;
    }
  }
}
