namespace Jenian.Infrastructure.Services.AI.Roster
{
  // Represents the horizontal boundaries of a single day's column in the roster photo.
  // Used to assign detected shift blocks to the correct day based on their X center.
  // Example usage:
  // var dayColumns = new List<DayColumn> {
  //   new DayColumn { Day = "Monday", LeftBoundary = 0, RightBoundary = 100 },
  //   new DayColumn { Day = "Tuesday", LeftBoundary = 100, RightBoundary = 200 },
  //   ...
  // };
  public class DayColumn
  {
    public string Day { get; init; } = string.Empty;
    public double HeaderXCenter { get; init; }
    public double LeftBoundary { get; set; }
    public double RightBoundary { get; set; }

    // Checks if a given X center coordinate falls within this day's column boundaries.
    // Inclusive of LeftBoundary, exclusive of RightBoundary to avoid overlap between adjacent columns.
    // For example, if LeftBoundary=100 and RightBoundary=200, then Contains(100) is true but Contains(200) is false.
    public bool Contains(double xCenter) => xCenter >= LeftBoundary && xCenter < RightBoundary;
  }
}
