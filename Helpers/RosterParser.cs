using System.Text.RegularExpressions;

namespace JenianAPI.Helpers
{
  public record Pt(int X, int Y);
  public record OcrItem(string Text, Pt[] Poly)
  {
    public double Cx => Poly.Average(p => p.X);
    public double Cy => Poly.Average(p => p.Y);
    public int Height => Poly.Max(p => p.Y) - Poly.Min(p => p.Y);
  }
  public record Column(string Dow, double Cx, double Left, double Right);
  public record Shift(string Day, string Time, OcrItem Source);
  public class RosterParser
  {

    // e.g. "TEXT,[(806, 59) (818, 60) (818, 69) (805, 70)]"
    static readonly Regex ItemRe = new(
        @"(.+?),\s*\[\s*\((\d+),\s*(\d+)\)\s*\((\d+),\s*(\d+)\)\s*\((\d+),\s*(\d+)\)\s*\((\d+),\s*(\d+)\)\s*\]",
        RegexOptions.Compiled);

    // tolerates “3 .9”, “8 - 430”, “8-4.30”, “11 - 7”, “1.9”, etc.
    static readonly Regex TimeRe = new(
        @"(?<!\d)(?<h1>\d{1,2})\s*[-–.:]\s*(?<h2>\d{1,2}(?::\d{2})?|\d{3,4})\b",
        RegexOptions.Compiled);

    static readonly HashSet<string> DayLabels = new(StringComparer.OrdinalIgnoreCase)
        { "MON","TUE","WED","THU","THUR","FRI","SAT","SUN" };

    public static List<OcrItem> ParseItems(string raw) {
      var items = new List<OcrItem>();
      foreach (Match m in ItemRe.Matches(raw)) {
        string text = m.Groups[1].Value.Trim();
        var pts = new[]
        {
                new Pt(int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value)),
                new Pt(int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value)),
                new Pt(int.Parse(m.Groups[6].Value), int.Parse(m.Groups[7].Value)),
                new Pt(int.Parse(m.Groups[8].Value), int.Parse(m.Groups[9].Value)),
            };
        items.Add(new OcrItem(text, pts));
      }
      return items;
    }

    public static List<Column> WeekdayColumns(IEnumerable<OcrItem> items) {
      var dayItems = items
          .Where(i => DayLabels.Contains(i.Text.Trim().ToUpperInvariant()))
          .Select(i => new { Dow = NormalizeDow(i.Text), Item = i })
          .OrderBy(x => x.Item.Cx)
          .ToList();

      if (dayItems.Count == 0)
        throw new InvalidOperationException("No weekday headers found in OCR text.");

      var centers = dayItems.Select(d => d.Item.Cx).ToArray();
      var cols = new List<Column>(dayItems.Count);
      for (int i = 0; i < dayItems.Count; i++) {
        double left = (i == 0) ? double.NegativeInfinity : (centers[i - 1] + centers[i]) / 2.0;
        double right = (i == dayItems.Count - 1) ? double.PositiveInfinity : (centers[i] + centers[i + 1]) / 2.0;
        cols.Add(new Column(dayItems[i].Dow, dayItems[i].Item.Cx, left, right));
      }
      return cols;
    }

    public static List<Shift> ExtractPersonShifts(string ocr, string personName) {
      var items = ParseItems(ocr);
      var cols = WeekdayColumns(items);

      var person = items.FirstOrDefault(i =>
          i.Text.Contains(personName, StringComparison.OrdinalIgnoreCase));

      if (person is null)
        throw new InvalidOperationException($"Person '{personName}' not found in OCR text.");

      // Estimate row height from nearby tokens (robust to OCR variance)
      var neighbors = items.Where(i => Math.Abs(i.Cy - person.Cy) < 24).ToList();
      double avgHeight = neighbors.Count > 0 ? Math.Max(12, neighbors.Average(n => n.Height)) : 18;
      double rowTol = Math.Max(12, avgHeight * 0.9); // vertical band around the name

      // Collect time tokens on the same row (within tolerance)
      var shiftItems = items
          .Where(i => Math.Abs(i.Cy - person.Cy) <= rowTol && TimeRe.IsMatch(i.Text))
          .ToList();

      var results = new List<Shift>();
      foreach (var s in shiftItems) {
        string normalizedTime = NormalizeTime(TimeRe.Match(s.Text));
        var day = MapToDay(s.Cx, cols);
        if (day != null)
          results.Add(new Shift(day, normalizedTime, s));
      }

      // sort by weekday column order
      var order = cols.Select((c, idx) => (c.Dow, idx)).ToDictionary(x => x.Dow, x => x.idx);
      results.Sort((a, b) => order[a.Day].CompareTo(order[b.Day]));
      return results;
    }

    static string? MapToDay(double x, IEnumerable<Column> cols)
        => cols.FirstOrDefault(c => x >= c.Left && x <= c.Right)?.Dow;

    static string NormalizeDow(string raw) {
      raw = raw.Trim().ToUpperInvariant();
      return raw is "THUR" ? "THU" : raw;
    }

    static string NormalizeTime(Match m) {
      // groups come from TimeRe: h1 separator h2
      string start = m.Groups["h1"].Value;
      string end = m.Groups["h2"].Value;

      return $"{FormatClock(start)} - {FormatClock(end)}";
    }

    static string FormatClock(string token) {
      token = token.Trim();

      // already H:MM
      if (Regex.IsMatch(token, @"^\d{1,2}:\d{2}$"))
        return token;

      // H.MM -> H:MM
      var dot = Regex.Match(token, @"^(?<h>\d{1,2})\.(?<m>\d{2})$");
      if (dot.Success)
        return $"{int.Parse(dot.Groups["h"].Value)}:{dot.Groups["m"].Value}";

      // HMM or HHMM -> HH:MM (only if minutes <= 59)
      var packed = Regex.Match(token, @"^(?<hm>\d{3,4})$");
      if (packed.Success) {
        var hm = packed.Groups["hm"].Value;
        var h = int.Parse(hm[..^2]);
        var m = int.Parse(hm[^2..]);
        if (m <= 59) return $"{h}:{m:00}";
      }

      // plain hour (e.g., "9" or "11")
      if (Regex.IsMatch(token, @"^\d{1,2}$"))
        return int.Parse(token).ToString();

      // fallback to original token if unsure
      return token;
    }
  }
}
