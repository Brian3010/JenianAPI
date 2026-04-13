namespace Jenian.Infrastructure.Services.AI.Roster
{

  // Simple helper to convert 3-letter day abbreviations to an order for sorting.
  // This is used in the RosterParser to ensure we always display days in the correct order,
  // even if the OCR text extraction is jumbled.
  // If the day string is unrecognized, we return a large number (999) to sort it at the end.
  // Note: This is a bit brittle (relies on exact "MON", "TUE", etc.), but it's sufficient for our controlled roster formats.
  public static class RosterDayOrder
  {
    public static int GetOrder(string day) => day switch {
      "MON" => 1,
      "TUE" => 2,
      "WED" => 3,
      "THU" => 4,
      "FRI" => 5,
      "SAT" => 6,
      "SUN" => 7,
      _ => 999
    };
  }
}
