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
      Console.WriteLine(sb);

      return $"""
          You normalize already-mapped roster shift text for exactly one staff member.

          The weekday mapping has already been done in code.
          You must NOT change the weekday.
          You must output one result line for every input line.

          Staff name:
          {staffName}

          Mapped raw shifts:
          {sb}

          Rules:

          1. Every input line is valid and must appear in the output.
          - Never skip a line
          - Never remove a day
          - Never add a day

          2. Normalize the shift separator.
          - If "." appears between two time values, treat it as "-"
          - If ":" appears between two time values, treat it as "-"
          - If "-" has missing spaces, normalize it to " - "
          Examples:
          - "1 . 9" => "1 - 9"
          - "11 . 9" => "11 - 9"
          - "1:9" => "1 - 9"
          - "1 -9" => "1 - 9"
          - "6-2" => "6 - 2"

          3. Keep the left and right values as they are.
          - Do not add AM
          - Do not add PM
          - Do not convert to 12-hour format
          - Do not infer meaning
          - Do not judge whether the shift is logical
          - Do not infer overnight shifts

          4. Final output format for each line:
          DAY: start - end

          5. Do not do any extra reasoning.
          - Do not change the weekday
          - Do not change the numbers
          - Do not rewrite to any other format
          - Only normalize the separator to " - "

          Strict output:

          {staffName}:
          MON: ...
          TUE: ...
          WED: ...
          THU: ...
          FRI: ...
          SAT: ...
          SUN: ...

          Output rules:
          - Include only the days present in the input
          - Order must be MON to SUN
          - No markdown
          - No explanations
          - No extra text
          """;
    }
  }
}
