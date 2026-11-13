namespace Jenian.Tests.TestSupport
{
  internal static class MelbourneTime
  {
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Australia/Melbourne");

    public static DateTimeOffset CreateMelbourneTime(int year, int month, int day, int hour, int minute) {
      var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
      return new DateTimeOffset(local, Zone.GetUtcOffset(local));
    }
  }
}
