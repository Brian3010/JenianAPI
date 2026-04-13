namespace Jenian.Infrastructure.Services.AI.Roster
{
  // Identifies weekday columns in a roster OCR output by locating header tokens (e.g. "MON", "TUE") and defining column boundaries.
  // Assumes headers are in a single row and uses their X centers to set left/right boundaries for each day column.
  // Key steps:
  // 1) Filter tokens to find candidates matching weekday headers (case-insensitive).
  // 2) Group candidates by Y center to find the top header row (within a small vertical threshold).
  // 3) Sort header tokens by X center and map to DayColumn objects with day name and header position.
  // 4) Set left/right boundaries for each column based on midpoints between adjacent headers (or infinity for outer edges).
  // This allows subsequent logic to assign roster entries to the correct day column based on their X position.
  // Note: The 20-pixel Y threshold for grouping headers is a heuristic that may need adjustment based on OCR accuracy and roster formatting.
  public class RosterWeekdayLocator
  {
    private static readonly Dictionary<string, string> HeaderMap = new(StringComparer.OrdinalIgnoreCase) {
      ["MON"] = "MON",
      ["TUE"] = "TUE",
      ["WED"] = "WED",
      ["THU"] = "THU",
      ["THUR"] = "THU",
      ["FRI"] = "FRI",
      ["SAT"] = "SAT",
      ["SUN"] = "SUN"
    };

    public static IReadOnlyList<DayColumn> BuildDayColumns(IReadOnlyList<OcrRosterToken> tokens) {
      var candidateHeaders = tokens
        .Where(t => HeaderMap.ContainsKey(NormalizeHeader(t.Text)))
        .OrderBy(t => t.YCenter) // Sort by Y center to group headers in the same row together
        .ThenBy(t => t.XCenter) // Secondary sort by X center to maintain left-to-right order within the same row
        .ToList();

      // If no candidate headers are found, return an empty list
      // This can happen if OCR fails to recognize any weekday headers or if the roster format is unexpected
      if (candidateHeaders.Count == 0)
        return Array.Empty<DayColumn>();

      // Group candidate headers by their Y center to find the top header row
      var topHeaderY = candidateHeaders.Min(t => t.YCenter);

      // Select headers that are within a small vertical threshold (e.g. 20 pixels) of the top header Y center
      var headerRow = candidateHeaders
        .Where(t => Math.Abs(t.YCenter - topHeaderY) <= 20)
        .OrderBy(t => t.XCenter)
        .ToList();

      var columns = headerRow
        .Select(t => new DayColumn {
          Day = HeaderMap[NormalizeHeader(t.Text)],
          HeaderXCenter = t.XCenter
        })
        .OrderBy(c => c.HeaderXCenter)
        .ToList();

      // If no valid headers are found in the top row, return an empty list
      // This can happen if OCR recognizes some tokens as headers but they are not in the expected format or position
      if (columns.Count == 0)
        return Array.Empty<DayColumn>();

      // Set left and right boundaries for each column based on midpoints between adjacent headers
      // For the first column, the left boundary is negative infinity; for the last column, the right boundary is positive infinity
      for (int i = 0; i < columns.Count; i++) {
        columns[i].LeftBoundary = i == 0
          ? double.NegativeInfinity
          : (columns[i - 1].HeaderXCenter + columns[i].HeaderXCenter) / 2.0;

        columns[i].RightBoundary = i == columns.Count - 1
          ? double.PositiveInfinity
          : (columns[i].HeaderXCenter + columns[i + 1].HeaderXCenter) / 2.0;
      }

      return columns;
    }

    private static string NormalizeHeader(string text) => text.Trim().ToUpperInvariant();
  }
}
