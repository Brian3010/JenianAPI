using System.Text.RegularExpressions;

namespace Jenian.Infrastructure.Services.Helpers;

/// <summary>
/// Cleans Telegram OCR text and keeps date markers and supplier delivery lines.
///
/// Main delivery formats supported:
/// - Sigma - 77
/// - Sigma-77
/// - Warehouse- 60
/// - Sigma: 78 boxes
/// - Sigma ? - 1
/// - Startrack - 60 blackmores, pharmacare
/// - Linfox healthcare - 1 sanofi
///
/// Timestamp behaviour:
/// - A standalone Telegram timestamp is treated as the end of the current message/batch.
/// - Any pending delivery lines above that timestamp receive the same time.
/// - Example:
///     Startrack - 7
///     metagenics-1
///     15:37
///   becomes:
///     Startrack - 7 @ 15:37
///     metagenics - 1 @ 15:37
/// </summary>
public static class TelegramOcrTextProcess
{
  public static string ExtractAfterLastToday(string ocrText) {
    if (string.IsNullOrWhiteSpace(ocrText))
      return string.Empty;

    var lines = ocrText
        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
        .ToList();

    static bool IsTodayMarker(string line) {
      var value = (line ?? string.Empty).Trim();

      return value.Equals("Today", StringComparison.OrdinalIgnoreCase)
          || value.Equals("oday", StringComparison.OrdinalIgnoreCase)
          || value.Equals("Taday", StringComparison.OrdinalIgnoreCase)
          || value.Equals("Todav", StringComparison.OrdinalIgnoreCase);
    }

    var lastTodayIndex = -1;

    for (var i = 0; i < lines.Count; i++) {
      if (IsTodayMarker(lines[i]))
        lastTodayIndex = i;
    }

    if (lastTodayIndex == -1 || lastTodayIndex == lines.Count - 1)
      return ocrText;

    return string.Join("\n", lines.Skip(lastTodayIndex + 1));
  }

  // Keep this small and explicit.
  // These are obvious Telegram/UI noise lines that should not help extraction.
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
    "OB",
    "V",
    "C",
    "JD",
    "NK",
    "ZL"
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
    "Nick Kvaw", // OCR typo seen in sample
    "Darren",
    "Brian",
    "Nabil",
    "Volkan",
    "Claudio",
    "Claudio CW",
    "qinlan",
    "Zheng Xian Lau"
  };

  // Optional supplier-like OCR/header false positives.
  // Add items here only when a line looks like "name - number" but is not a real supplier.
  private static readonly HashSet<string> IgnoredSupplierNames = new(StringComparer.OrdinalIgnoreCase)
  {
    "ella"
  };

  // Lines that are only page/header/footer junk like:
  // 37
  // 38
  // 1:25 S
  // 02:47 G
  // Important: timestamp lines are handled before this regex in Clean().
  private static readonly Regex HeaderFooterNoiseRegex = new(
      @"^(?:\d{1,3}|\d{1,2}:\d{2}(?:\s?[A-Za-z]|\s?V/|\s?/|\s?\?)?)$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

  // Telegram timestamp line:
  // 09:22
  // 14:29
  // 10:20 AM
  // 13:51 V/
  // 04:12 ?
  private static readonly Regex TelegramTimestampLineRegex = new(
      @"^(?<time>(?:[01]?\d|2[0-3]):[0-5]\d(?:\s?(?:AM|PM))?)(?:\s?(?:[A-Za-z]|V/|/|\?))?$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

  // Timestamp attached at the end of a useful line:
  // Sigma ? - 1 9:44 AM -> Sigma ? - 1 + 9:44 AM
  // Blackmores - 3 4:13 PM V/ -> Blackmores - 3 + 4:13 PM
  private static readonly Regex TrailingTimestampRegex = new(
      @"\s+\b(?<time>(?:[01]?\d|2[0-3]):[0-5]\d(?:\s?(?:AM|PM))?)\b(?:\s?(?:[A-Za-z]|V/|/|\?))?\s*$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

  // edited 12:35 PM -> 12:35 PM
  private static readonly Regex EditedTimestampRegex = new(
      @"^edited\s+(?<time>(?:[01]?\d|2[0-3]):[0-5]\d(?:\s?(?:AM|PM))?)$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

  // Keep date markers because they help separate delivery days.
  // Examples:
  // Today
  // Yesterday
  // June 12
  // December 29, 2025
  // March 5
  private static readonly Regex DateMarkerRegex = new(
      @"^(Today|Yesterday|[A-Za-z]+\s+\d{1,2}(?:,\s*\d{4})?)$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

  // Supplier delivery line:
  // sigma - 80
  // Loreal- 9
  // Pierre fabre - 3
  // Sigma: 78 boxes
  // Sigma ? - 1
  // Startrack - 60 blackmores, pharmacare
  //
  // Supplier must start with a letter, so a time like "14:29" will not become "14 - 29".
  private static readonly Regex DeliveryReportRegex = new(
      @"^\s*(?<supplier>[\p{L}][\p{L}\p{N}&'’./\s]*?)(?:\s*\?)?\s*[-:–—]\s*(?<quantity>\d+)(?:\s*box(?:es)?)?\b(?<note>.*)$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

  public static string Clean(string rawText, bool removeKnownUserLines = true) {
    var filteredTodayText = ExtractAfterLastToday(rawText);

    if (string.IsNullOrWhiteSpace(filteredTodayText))
      return string.Empty;

    var lines = filteredTodayText.Split('\n', StringSplitOptions.None);
    var kept = new List<string>();
    var pendingDeliveryLines = new List<string>();

    void FlushPendingDeliveries(string? timestamp = null) {
      foreach (var deliveryLine in pendingDeliveryLines) {
        kept.Add(string.IsNullOrWhiteSpace(timestamp)
            ? deliveryLine
            : $"{deliveryLine} @ {timestamp}");
      }

      pendingDeliveryLines.Clear();
    }

    foreach (var rawLine in lines) {
      var line = rawLine.Trim();

      if (string.IsNullOrWhiteSpace(line))
        continue;

      if (ExactNoiseLines.Contains(line))
        continue;

      if (removeKnownUserLines && KnownUserLines.Contains(line))
        continue;

      // "edited 12:35 PM" usually belongs to the message above it.
      // Treat it as a timestamp and flush pending deliveries.
      if (TryGetEditedTimestamp(line, out var editedTimestamp)) {
        FlushPendingDeliveries(editedTimestamp);
        continue;
      }

      // If this line is a Telegram timestamp, attach it to delivery lines collected above it.
      // This must run before HeaderFooterNoiseRegex because values like "14:29" look like header noise too.
      if (TryGetTelegramTimestamp(line, out var timestamp)) {
        FlushPendingDeliveries(timestamp);
        continue;
      }

      // Remove header/footer junk like "37", "38", "1:25 S".
      if (HeaderFooterNoiseRegex.IsMatch(line))
        continue;

      if (DateMarkerRegex.IsMatch(line)) {
        FlushPendingDeliveries();
        kept.Add(line);
        continue;
      }

      // Handle inline timestamp:
      // "Sigma ? - 1 9:44 AM" -> "Sigma ? - 1" + "9:44 AM"
      var trailingTimestamp = string.Empty;

      if (TryRemoveTrailingTimestamp(line, out var lineWithoutTimestamp, out var extractedTrailingTimestamp)) {
        line = lineWithoutTimestamp;
        trailingTimestamp = extractedTrailingTimestamp;
      }

      if (DeliveryReportRegex.IsMatch(line)) {
        if (IsIgnoredSupplierLine(line))
          continue;

        var normalizedDeliveryLine = NormalizeDeliveryLine(line);

        if (string.IsNullOrWhiteSpace(trailingTimestamp))
          pendingDeliveryLines.Add(normalizedDeliveryLine);
        else
          kept.Add($"{normalizedDeliveryLine} @ {trailingTimestamp}");

        continue;
      }
    }

    // If OCR ends without a timestamp after the last delivery batch, still keep them.
    FlushPendingDeliveries();

    return DeliveryDeduplicator.RemoveDuplicates(string.Join("\n", kept));
  }

  private static bool TryGetTelegramTimestamp(string line, out string timestamp) {
    timestamp = string.Empty;

    var match = TelegramTimestampLineRegex.Match(line);

    if (!match.Success)
      return false;

    timestamp = match.Groups["time"].Value.Trim();
    return true;
  }

  private static bool TryGetEditedTimestamp(string line, out string timestamp) {
    timestamp = string.Empty;

    var match = EditedTimestampRegex.Match(line);

    if (!match.Success)
      return false;

    timestamp = match.Groups["time"].Value.Trim();
    return true;
  }

  private static bool TryRemoveTrailingTimestamp(
      string line,
      out string cleanedLine,
      out string timestamp) {
    cleanedLine = line;
    timestamp = string.Empty;

    var match = TrailingTimestampRegex.Match(line);

    if (!match.Success)
      return false;

    timestamp = match.Groups["time"].Value.Trim();
    cleanedLine = TrailingTimestampRegex.Replace(line, string.Empty).Trim();

    return true;
  }

  private static bool IsIgnoredSupplierLine(string line) {
    var match = DeliveryReportRegex.Match(line);

    if (!match.Success)
      return false;

    var supplier = NormalizeText(match.Groups["supplier"].Value);

    return IgnoredSupplierNames.Contains(supplier);
  }

  private static string NormalizeDeliveryLine(string line) {
    var match = DeliveryReportRegex.Match(line);

    if (!match.Success)
      return line.Trim();

    var supplier = Regex.Replace(match.Groups["supplier"].Value.Trim(), @"\s+", " ");
    var quantity = match.Groups["quantity"].Value.Trim();
    var note = Regex.Replace(match.Groups["note"].Value.Trim(), @"\s+", " ");

    return string.IsNullOrWhiteSpace(note)
        ? $"{supplier} - {quantity}"
        : $"{supplier} - {quantity} {note}";
  }

  private static string NormalizeText(string value) {
    value = Regex.Replace(value.Trim(), @"\s+", " ");
    return value.ToLowerInvariant();
  }
}

public static class DeliveryDeduplicator
{
  // This matches the normalized output from TelegramOcrTextProcess.Clean:
  // Sigma - 77 @ 13:12
  // Startrack - 60 blackmores, pharmacare @ 13:51
  private static readonly Regex DeliveryLineRegex = new(
      @"^\s*(?<name>.+?)\s+-\s+(?<qty>\d+)\b(?<note>.*?)\s+@\s+(?<time>(?:[01]?\d|2[0-3]):[0-5]\d(?:\s?(?:AM|PM))?)\s*$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

  public static string RemoveDuplicates(string text) {
    if (string.IsNullOrWhiteSpace(text))
      return string.Empty;

    var seen = new HashSet<string>();
    var output = new List<string>();

    var lines = text
        .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
        .Select(line => line.Trim())
        .Where(line => !string.IsNullOrWhiteSpace(line));

    foreach (var line in lines) {
      var match = DeliveryLineRegex.Match(line);

      // Keep date markers or other unparseable kept lines as-is.
      if (!match.Success) {
        output.Add(line);
        continue;
      }

      var name = NormalizeText(match.Groups["name"].Value);
      var quantity = match.Groups["qty"].Value.Trim();
      var note = NormalizeText(match.Groups["note"].Value);
      var time = NormalizeText(match.Groups["time"].Value);
      var key = $"{name}|{quantity}|{note}|{time}";

      if (seen.Add(key))
        output.Add(line);
    }

    return string.Join(Environment.NewLine, output);
  }

  private static string NormalizeText(string value) {
    value = Regex.Replace(value.Trim(), @"\s+", " ");
    return value.ToLowerInvariant();
  }
}
