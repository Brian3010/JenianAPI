using System.Text;

namespace Jenian.Infrastructure.Services.AI.Roster
{
  // This class builds a prompt for normalizing raw roster shift text for a single staff member.
  public class RosterShiftNormaliserPromptBuilder
  {
    public static string Build(string staffName, IReadOnlyList<RawMappedShift> mappedShifts) {
      var sb = new StringBuilder();

      foreach (var shift in mappedShifts.OrderBy(x => RosterDayOrder.GetOrder(x.Day))) {
        sb.AppendLine($"{shift.Day}: {shift.RawShiftText}");
      }

      return $"""
          You normalize already-mapped raw roster shift text for exactly one staff member.

          The weekday mapping has already been done in code.
          Do not change the weekday.
          Only normalize the shift text.

          Staff name:
          {staffName}

          Mapped raw shifts:
          {sb}

          Rules:
          1. Standard output format for each valid shift:
             DAY: h:mm AM/PM - h:mm AM/PM

          2. Normalize messy formats BEFORE interpreting:
             - Replace "." with ":" when used in times (e.g., 4.30 → 4:30)
             - Replace ".", "|" or extra spaces between numbers with "-" (e.g., 3 . 9 → 3 - 9, 1.9 → 1 - 9)
             - Remove duplicate spaces

          3. Handle incomplete shifts:
             - "11 -" → start_time = 11, end_time = null
             - "- 9" → start_time = null, end_time = 9
             - If either start or end is missing, treat the shift as VALID and and replace with either ? - 9 or 11 - ?

          4. Valid shift examples:
             - 8 - 4 → 8:00 AM - 4:00 PM
             - 8 - 4:30 → 8:00 AM - 4:30 PM
             - 1 - 9 → 1:00 PM - 9:00 PM
             - 3 - 9 → 3:00 PM - 9:00 PM
             - 11 - 7 → 11:00 AM - 7:00 PM
             - 9 - 5 → 9:00 AM - 5:00 PM
             - 9 - 7 → 9:00 AM - 7:00 PM
             - 9 - 9 → 9:00 AM - 9:00 PM
             - 8 - → 8:00 AM - ?
             - 8 - ? → ? - 4:00 PM
             -   -   → ? - ?

          5. AM/PM rules:
             - Start hours 8–11 → AM
             - Start hours 1–7 → PM
             - End time must be later than start time on the same day
             - Do NOT create overnight shifts

          6. Tags:
             - If a trailing tag exists (e.g., MT), preserve it in parentheses

          7. Important:
             - Only include VALID shifts (both start and end present and logical)
             - Valid entries can include missing time (e.g., "11 -" or "- 9" or " - ")

          Strict output:

          - If no valid shifts remain:
            {staffName} is enjoying the holiday

          - Otherwise output exactly:
            {staffName} has shifts on:
            MON: ...
            TUE: ...
            WED: ...
            THU: ...
            FRI: ...
            SAT: ...
            SUN: ...

          Output rules:
          - Include only days that have valid shifts
          - Order must be MON to SUN
          - No markdown
          - No explanations
          - No extra text
          """;
    }
  }
}
