using System.Text.RegularExpressions;

namespace Jenian.Infrastructure.Services.AI.Roster
{
  // This class is responsible for mapping raw OCR tokens from a roster row into structured shift data, using heuristics to identify which tokens likely represent shifts and which day they correspond to.
  // It takes a StaffRowMatch (which contains the OCR tokens for a staff member's row) and a list of DayColumns (which represent the columns for each day of the week) and produces a list of RawMappedShift objects that include the day, raw shift text, and horizontal position.
  // The key method is MapRawShifts, which iterates through the tokens in the row, applies heuristics to filter out non-shift tokens, and matches shift tokens to the correct day column based on their horizontal position (XCenter).
  // The LooksLikeShiftToken method uses simple heuristics to determine if a token is likely to represent a shift, such as checking for the presence of digits and filtering out common non-shift patterns.
  // The resulting list of RawMappedShift objects is then ordered by the day of the week and the horizontal position to maintain a consistent structure for further processing.
  public class RosterShiftMapper
  {

    public static IReadOnlyList<RawMappedShift> MapRawShifts(
    StaffRowMatch rowMatch,
    IReadOnlyList<DayColumn> dayColumns) {
      var mapped = new List<RawMappedShift>();

      foreach (var token in rowMatch.RowTokens) {
        if (ReferenceEquals(token, rowMatch.NameToken))
          continue;

        if (!LooksLikeShiftToken(token.Text))
          continue;

        var dayColumn = dayColumns.FirstOrDefault(c => c.Contains(token.XCenter));
        if (dayColumn is null)
          continue;


        mapped.Add(new RawMappedShift {
          Day = dayColumn.Day,
          RawShiftText = token.Text.Trim(),
          XCenter = token.XCenter
        });
      }

      return mapped
        .OrderBy(x => RosterDayOrder.GetOrder(x.Day))
        .ThenBy(x => x.XCenter)
        .ToList();
    }



    // Heuristics to filter out tokens that are unlikely to represent shifts, such as: 
    // - Empty or whitespace-only tokens
    // - Tokens without any digits (since shifts typically include times)
    // - Tokens that are just "..." which might be used for ellipses in names or other text
    // - Tokens that look like incomplete OCR artifacts (e.g., "12 -" or "3.") which are unlikely to be valid shifts
    // This is a simple heuristic and may need to be adjusted based on the specific formats of shifts in the rosters being processed.
    private static bool LooksLikeShiftToken(string text) {
      var value = text.Trim();

      if (string.IsNullOrWhiteSpace(value))
        return false;

      if (!value.Any(char.IsDigit))
        return false;

      if (value == "...")
        return false;

      // Exclude tokens that look like "12 -" or "3." which are likely incomplete OCR artifacts rather than valid shifts
      //if (Regex.IsMatch(value, @"^\d{1,2}\s*[-.]\s*$"))
      //  return true;

      return true;
    }
  }
}
