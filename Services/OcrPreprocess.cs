using OpenCvSharp;

namespace JenianAPI.Services
{
  public class OcrPreprocess
  {

    /// <summary>
    /// Straighten a roster photo: auto-deskew (rotation) and optional perspective warp.
    /// Returns PNG bytes ready for OCR.
    /// </summary>
    public static byte[] PhotoCleanUp(byte[] photoInput, double scale = 1.25, bool perspective = true) {
      using var srcColor = Cv2.ImDecode(photoInput, ImreadModes.Color);
      if (srcColor.Empty()) throw new InvalidOperationException("Invalid image");

      // 1) Grayscale (for detection work)
      using var gray = new Mat();
      Cv2.CvtColor(srcColor, gray, ColorConversionCodes.BGR2GRAY);

      // 2) Deskew (rotation) — robust median from HoughLinesP (prefers long horizontals)
      var angle = EstimateSkewAngle(gray);
      using var rotatedColor = RotateKeepContent(srcColor, (float)angle);
      using var rotatedGray = RotateKeepContent(gray, (float)angle); // for optional page detect

      // 3) Optional perspective correction (keystone → top-down)
      Mat alignedColor = rotatedColor; // default to rotated
      if (perspective && TryFindPageQuad(rotatedGray, out var quad)) {
        alignedColor = WarpToTopDown(rotatedColor, quad, marginPct: 0.01); // returns a NEW Mat
      } else {
        alignedColor = rotatedColor.Clone(); // own the buffer since we dispose rotatedColor at end
      }

      // 4) (Minimal) enhancement: light contrast + resize for OCR
      using var outMat = new Mat();
      Cv2.GaussianBlur(alignedColor, outMat, new Size(3, 3), 0);
      if (Math.Abs(scale - 1.0) > 0.01)
        Cv2.Resize(outMat, outMat, new Size(), scale, scale, InterpolationFlags.Cubic);

      // 5) Encode to PNG
      var png = outMat.ImEncode(".png");

      // cleanup (we cloned alignedColor above if needed)
      alignedColor.Dispose();
      return png;
    }

    // ---------- Skew detection (rotation) ----------

    private static double EstimateSkewAngle(Mat gray) {
      // Edge + thinning to highlight long lines
      using var edges = new Mat();
      Cv2.Canny(gray, edges, 50, 150);

      // Probabilistic Hough — easier to filter short vs long segments
      var lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, threshold: 100, minLineLength: gray.Cols / 4, maxLineGap: 20);
      if (lines.Length == 0) return 0;

      // Keep almost-horizontal segments only; compute angle in degrees
      var angles = new List<double>(lines.Length);
      foreach (var l in lines) {
        var dx = l.P2.X - l.P1.X;
        var dy = l.P2.Y - l.P1.Y;
        if (dx == 0) continue;
        var a = Math.Atan2(dy, dx) * 180.0 / Math.PI; // [-180..180]
        var na = a > 90 ? a - 180 : (a < -90 ? a + 180 : a); // normalize to [-90..90]
        if (Math.Abs(na) <= 15) angles.Add(na); // near-horizontal only
      }

      if (angles.Count == 0) return 0;

      // Median is robust to outliers
      angles.Sort();
      var median = angles[angles.Count / 2];

      // Clamp to avoid wild rotations
      return Math.Max(-10, Math.Min(10, median));
    }

    private static Mat RotateKeepContent(Mat src, float angleDeg) {
      if (Math.Abs(angleDeg) < 0.3) return src.Clone();
      var c = new Point2f(src.Cols / 2f, src.Rows / 2f);
      var m = Cv2.GetRotationMatrix2D(c, angleDeg, 1.0);
      var box = new RotatedRect(c, new Size2f(src.Cols, src.Rows), angleDeg).BoundingRect();

      // shift so the whole image fits
      m.Set(0, 2, m.Get<double>(0, 2) + box.Width / 2 - c.X);
      m.Set(1, 2, m.Get<double>(1, 2) + box.Height / 2 - c.Y);

      var dst = new Mat();
      Cv2.WarpAffine(src, dst, m, box.Size, InterpolationFlags.Cubic, BorderTypes.Constant, Scalar.White);
      return dst; // caller disposes
    }

    // ---------- Perspective correction (page quad -> top-down) ----------

    private static bool TryFindPageQuad(Mat gray, out Point2f[] quad) {
      // downscale for stability
      var maxDim = 1200;
      double scale = 1.0;
      using var small = ResizeKeepingAspect(gray, maxDim, ref scale);

      using var blur = new Mat(); Cv2.GaussianBlur(small, blur, new Size(5, 5), 0);
      using var edges = new Mat(); Cv2.Canny(blur, edges, 50, 150);
      using var dil = new Mat(); Cv2.Dilate(edges, dil, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)));

      Cv2.FindContours(dil, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

      double bestArea = 0;
      Point2f[]? best = null;
      foreach (var c in contours) {
        var peri = Cv2.ArcLength(c, true);
        var approx = Cv2.ApproxPolyDP(c, 0.02 * peri, true);
        if (approx.Length != 4 || !Cv2.IsContourConvex(approx)) continue;

        var area = Math.Abs(Cv2.ContourArea(approx));
        if (area < (small.Rows * small.Cols) * 0.10) continue; // ignore tiny quads
        if (area > bestArea) {
          bestArea = area;
          best = approx.Select(p => new Point2f(p.X / (float)scale, p.Y / (float)scale)).ToArray();
        }
      }

      if (best is null) { quad = Array.Empty<Point2f>(); return false; }
      quad = OrderCorners(best);
      return true;
    }

    private static Mat WarpToTopDown(Mat src, Point2f[] quad, double marginPct) {
      // compute target size from opposite sides
      float wA = Distance(quad[2], quad[3]), wB = Distance(quad[1], quad[0]);
      float hA = Distance(quad[1], quad[2]), hB = Distance(quad[0], quad[3]);
      float maxW = Math.Max(wA, wB), maxH = Math.Max(hA, hB);

      var mw = (float)(marginPct * maxW);
      var mh = (float)(marginPct * maxH);
      var dstW = (int)(maxW + 2 * mw);
      var dstH = (int)(maxH + 2 * mh);

      var srcPts = new[] { quad[0], quad[1], quad[2], quad[3] }; // TL,TR,BR,BL
      var dstPts = new[]
      {
            new Point2f(mw,        mh),
            new Point2f(dstW - mw, mh),
            new Point2f(dstW - mw, dstH - mh),
            new Point2f(mw,        dstH - mh)
        };

      using var M = Cv2.GetPerspectiveTransform(srcPts, dstPts);
      var warped = new Mat();
      Cv2.WarpPerspective(src, warped, M, new Size(dstW, dstH), InterpolationFlags.Cubic, BorderTypes.Constant, Scalar.White);
      return warped;
    }

    private static Point2f[] OrderCorners(Point2f[] pts) {
      var sum = pts.Select(p => p.X + p.Y).ToArray();
      var diff = pts.Select(p => p.X - p.Y).ToArray();

      var tl = pts[Array.IndexOf(sum, sum.Min())];
      var br = pts[Array.IndexOf(sum, sum.Max())];
      var tr = pts[Array.IndexOf(diff, diff.Max())];
      var bl = pts[Array.IndexOf(diff, diff.Min())];

      return new[] { tl, tr, br, bl };
    }

    private static Mat ResizeKeepingAspect(Mat src, int maxDim, ref double scale) {
      var (h, w) = (src.Rows, src.Cols);
      var maxCurrent = Math.Max(h, w);
      if (maxCurrent <= maxDim) { scale = 1.0; return src.Clone(); }
      scale = (double)maxDim / maxCurrent;
      var dst = new Mat();
      Cv2.Resize(src, dst, new Size((int)(w * scale), (int)(h * scale)), 0, 0, InterpolationFlags.Area);
      return dst;
    }

    private static float Distance(Point2f a, Point2f b)
        => (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
  }
}
