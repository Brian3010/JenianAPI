namespace Jenian.Infrastructure.Services.AI.Roster
{
  // Represents a single OCR token with its text and bounding box coordinates.
  // The bounding box is defined by four points (X1,Y1), (X2,Y2), (X3,Y3), (X4,Y4).
  public class OcrRosterToken
  {
    public string Text { get; set; } = string.Empty;

    public int X1 { get; init; }
    public int Y1 { get; init; }
    public int X2 { get; init; }
    public int Y2 { get; init; }
    public int X3 { get; init; }
    public int Y3 { get; init; }
    public int X4 { get; init; }
    public int Y4 { get; init; }

    // Convenience properties to get the center of the bounding box and its extents.
    // The center is the average of the four corner points.
    // The extents can be calculated as the width and height of the bounding box.
    // Note: This assumes the bounding box is a quadrilateral and not necessarily axis-aligned.
    public double XCenter => (X1 + X2 + X3 + X4) / 4.0;
    public double YCenter => (Y1 + Y2 + Y3 + Y4) / 4.0;

    public int MinX => Math.Min(Math.Min(X1, X2), Math.Min(X3, X4));
    public int MaxX => Math.Max(Math.Max(X1, X2), Math.Max(X3, X4));
    public int MinY => Math.Min(Math.Min(Y1, Y2), Math.Min(Y3, Y4));
    public int MaxY => Math.Max(Math.Max(Y1, Y2), Math.Max(Y3, Y4));
  }
}
