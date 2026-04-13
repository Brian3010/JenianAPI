using System.Globalization;
using System.Text.RegularExpressions;

namespace Jenian.Infrastructure.Services.AI.Roster
{

  // Parses OCR text lines in the format:
  // "Name,[(x1, y1) (x2, y2) (x3, y3) (x4, y4)]"
  // Example:
  // "Alice,[(10, 20) (30, 40) (50, 60) (70, 80)]"
  // Captures the name and the 4 coordinate pairs for each token.
  // This is a simple, explicit parser that avoids complex NLP or ML for token extraction.
  // The regex breakdown:
  // ^(.*?) - Capture the name (non-greedy up to the first comma)
  // ,\[ - Match the literal ",["
  // \((\d+),\s*(\d+)\) - Capture each coordinate pair (x, y) with optional whitespace
  // \]$ - Match the closing "]" at the end of the line
  public class RosterOcrParser
  {
    private static readonly Regex TokenRegex = new(
    @"^(.*?),\[\((\d+),\s*(\d+)\)\s+\((\d+),\s*(\d+)\)\s+\((\d+),\s*(\d+)\)\s+\((\d+),\s*(\d+)\)\]$",
    RegexOptions.Compiled);

    //public static IReadOnlyList<OcrRosterToken> Parse(string ocrText) {
    public static List<OcrRosterToken> Parse(string ocrText) {
      var tokens = new List<OcrRosterToken>();

      if (string.IsNullOrWhiteSpace(ocrText))
        return tokens;

      // Split the OCR text into lines and process each line with the regex.
      var lines = ocrText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

      foreach (var rawLine in lines) {
        var line = rawLine.Trim();
        var match = TokenRegex.Match(line);

        if (!match.Success)
          continue;

        // Extract the captured groups and create an OcrRosterToken for each valid line.
        // The groups are indexed as follows:
        // Group 1: Name
        // Group 2-9: Coordinates (x1, y1, x2, y2, x3, y3, x4, y4)
        tokens.Add(new OcrRosterToken {
          Text = match.Groups[1].Value.Trim(), // The captured name
          X1 = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
          Y1 = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
          X2 = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture),
          Y2 = int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture),
          X3 = int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture),
          Y3 = int.Parse(match.Groups[7].Value, CultureInfo.InvariantCulture),
          X4 = int.Parse(match.Groups[8].Value, CultureInfo.InvariantCulture),
          Y4 = int.Parse(match.Groups[9].Value, CultureInfo.InvariantCulture)
        });
      }

      return tokens;
    }
  }
}
