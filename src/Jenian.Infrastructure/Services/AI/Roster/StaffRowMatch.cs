namespace Jenian.Infrastructure.Services.AI.Roster
{
  // Represents a successful match of a staff name token to a row of tokens in the roster OCR output.
  public class StaffRowMatch
  {
    public OcrRosterToken NameToken { get; init; } = default!;
    public List<OcrRosterToken> RowTokens { get; set; } = [];
    //public IReadOnlyList<OcrRosterToken> RowTokens { get; init; } = Array.Empty<OcrRosterToken>();
  }
}
