namespace Jenian.Infrastructure.Services.AI.Roster
{
  // Simple DTO to hold a raw shift text mapped to an X-center coordinate on the roster image.
  // This is used as an intermediate step before parsing the shift text into structured data.
  public class RawMappedShift
  {
    public string Day { get; init; } = string.Empty;
    public string RawShiftText { get; init; } = string.Empty;
    public double XCenter { get; init; }
  }
}
