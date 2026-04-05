using System.Text.RegularExpressions;

namespace Jenian.Infrastructure.Services.Helpers
{
  public static class TelegramOcrTextProcess
  {

    public static string ExtractAfterLastToday(string ocrText) {
      if (string.IsNullOrWhiteSpace(ocrText))
        return string.Empty;

      var lines = ocrText
          .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
          .ToList();

      static bool IsTodayMarker(string line) {
        var s = (line ?? "").Trim();
        return s.Equals("Today", StringComparison.OrdinalIgnoreCase)
            || s.Equals("oday", StringComparison.OrdinalIgnoreCase)
            || s.Equals("Taday", StringComparison.OrdinalIgnoreCase)
            || s.Equals("Todav", StringComparison.OrdinalIgnoreCase);
      }

      int lastTodayIndex = -1;
      for (int i = 0; i < lines.Count; i++) {
        if (IsTodayMarker(lines[i]))
          lastTodayIndex = i;
      }

      if (lastTodayIndex == -1 || lastTodayIndex == lines.Count - 1)
        return ocrText;

      return string.Join("\n", lines.Skip(lastTodayIndex + 1));
    }

    // Keep this small and explicit.
    // These are obvious Telegram/UI noise lines that should not help the model.
    private static readonly HashSet<string> ExactNoiseLines = new(StringComparer.OrdinalIgnoreCase)
    {
        "TELEGRAM",
        "Message",
        "messages",
        "member",
        "members",
        "<",
        ">",
        "<<",
        ">>",
        "M",
        "OB"
    };

    // Optional known usernames from your chat.
    // Remove these as standalone lines only.
    private static readonly HashSet<string> KnownUserLines = new(StringComparer.OrdinalIgnoreCase)
    {
        "Alish",
        "Alish Thapa",
        "Bindu",
        "Oakar",
        "Oakar Bo",
        "Nick Kyaw",
        "Darren",
        "Brian",
        "Nabil",
        "Volkan",
        "Claudio",
        "qinlan",
        "NK"
    };

    // Lines that are only page/header/footer junk like:
    // 37
    // 38
    // 1:25 S
    // 1:25
    private static readonly Regex HeaderFooterNoiseRegex = new(
        @"^(?:\d{1,3}|\d{1,2}:\d{2}(?:\s?[A-Za-z])?)$",
        RegexOptions.Compiled);

    // Timestamp line:
    // 10:21 AM
    // 3:29 PM
    // 10:52 am
    private static readonly Regex TimeOnlyRegex = new(
        @"^\d{1,2}:\d{2}\s?(?:AM|PM|am|pm)$",
        RegexOptions.Compiled);

    // Keep date markers because your prompt may use them.
    private static readonly Regex DateMarkerRegex = new(
        @"^(Today|Yesterday|[A-Za-z]+\s+\d{1,2})$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Delivery-like line:
    // sigma - 80
    // Loreal- 9
    // Pierre fabre - 3
    // Statrack- 4 - 2 3:00 PM   (still delivery-like, prompt can decide later)
    private static readonly Regex DeliveryLikeRegex = new(
        @"[A-Za-z].*\d",
        RegexOptions.Compiled);

    // Question line:
    // What is in StarTrack
    private static readonly Regex QuestionRegex = new(
        @"\?$|^(what|where|why|who|when|how|is|are|can|could|should|would)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Clean(string rawText, bool removeKnownUserLines = true, bool keepQuestions = true) {

      var filteredTodayText = ExtractAfterLastToday(rawText);

      if (string.IsNullOrWhiteSpace(filteredTodayText))
        return string.Empty;

      // 2. Split into individual lines
      var lines = filteredTodayText
          .Split('\n', StringSplitOptions.None)
          .ToList();

      var kept = new List<string>();

      foreach (var line in lines) {
        if (string.IsNullOrWhiteSpace(line))
          continue;

        // Remove obvious exact noise
        if (ExactNoiseLines.Contains(line))
          continue;

        // Remove standalone known usernames if wanted
        if (removeKnownUserLines && KnownUserLines.Contains(line))
          continue;

        // Remove header/footer junk like "37", "38", "1:25 S"
        // BUT do not remove real timestamp lines like "10:21 AM"
        if (!TimeOnlyRegex.IsMatch(line) && HeaderFooterNoiseRegex.IsMatch(line))
          continue;

        // Keep date markers
        if (DateMarkerRegex.IsMatch(line)) {
          kept.Add(line);
          continue;
        }

        // Convert "edited 4:00 PM" -> "4:00 PM"
        if (line.StartsWith("edited ", StringComparison.OrdinalIgnoreCase)) {
          var editedTime = line["edited ".Length..].Trim();

          if (TimeOnlyRegex.IsMatch(editedTime)) {
            kept.Add(editedTime);
            continue;
          }
        }

        // Keep pure timestamp lines
        if (TimeOnlyRegex.IsMatch(line)) {
          kept.Add(line);
          continue;
        }

        // Optionally keep question lines so the prompt can explicitly ignore them
        if (keepQuestions && QuestionRegex.IsMatch(line)) {
          kept.Add(line);
          continue;
        }

        // Keep likely delivery lines / other useful content
        if (DeliveryLikeRegex.IsMatch(line)) {
          kept.Add(line);
          continue;
        }

        // Otherwise drop it.
      }


      // 4. Return cleaned text
      return string.Join("\n", kept);
    }


  }
}


public static class DeliveryDeduplicator
{
  private static readonly System.Text.RegularExpressions.Regex DeliveryLineRegex =
      new(@"^(?<name>.+?)\s-\s(?<qty>\d+)(?:\s(?<extra>\(.+\)))?\s@\s(?<time>\d{1,2}:\d{2}(?:am|pm))$",
          System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

  public static string RemoveDuplicates(string text) {
    if (string.IsNullOrWhiteSpace(text))
      return string.Empty;

    var seen = new HashSet<string>();
    var output = new List<string>();

    var lines = text
        .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim())
        .Where(x => !string.IsNullOrWhiteSpace(x));

    foreach (var line in lines) {
      var match = DeliveryLineRegex.Match(line);

      // Keep unparseable lines as-is, or skip them if you prefer strict mode.
      if (!match.Success) {
        output.Add(line);
        continue;
      }

      var name = NormalizeName(match.Groups["name"].Value);
      var qty = match.Groups["qty"].Value.Trim();
      var extra = NormalizeExtra(match.Groups["extra"].Value);
      var time = NormalizeTime(match.Groups["time"].Value);

      var key = $"{name}|{qty}|{extra}|{time}";

      if (seen.Add(key))
        output.Add($"{match.Groups["name"].Value.Trim()} - {qty}" +
                   $"{(string.IsNullOrWhiteSpace(match.Groups["extra"].Value) ? "" : " " + match.Groups["extra"].Value.Trim())} @ {time}");
    }

    return string.Join(Environment.NewLine, output);
  }

  private static string NormalizeName(string value) {
    value = System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ");
    return value.ToLowerInvariant();
  }

  private static string NormalizeExtra(string value) {
    value ??= "";
    value = value.Trim();

    if (string.IsNullOrWhiteSpace(value))
      return "";

    value = System.Text.RegularExpressions.Regex.Replace(value, @"\s+", " ");
    return value.ToLowerInvariant();
  }

  private static string NormalizeTime(string value) {
    value = value.Trim().ToLowerInvariant();
    value = value.Replace(" ", "");
    return value;
  }
}
