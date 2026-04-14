using OpenCvSharp;

namespace Jenian.Infrastructure.Services.AI
{
  public class OcrPreprocess
  {
    /// <summary>
    /// Straighten a roster photo:
    /// 1) optional perspective correction
    /// 2) deskew by rotation
    /// 3) light blur / resize
    /// Input: image stream
    /// Returns PNG bytes ready for OCR.
    /// </summary>
    public static async Task<byte[]> PhotoCleanUpAsync(
      Stream photoInputStream,
      double scale = 1.25,
      bool perspective = true,
      CancellationToken cancellationToken = default) {
      if (photoInputStream == null)
        throw new ArgumentNullException(nameof(photoInputStream));

      if (!photoInputStream.CanRead)
        throw new ArgumentException("Input stream must be readable.", nameof(photoInputStream));

      // Read stream into byte[] because Cv2.ImDecode expects bytes
      using var memoryStream = new MemoryStream();
      await photoInputStream.CopyToAsync(memoryStream, cancellationToken);

      var photoInputBytes = memoryStream.ToArray();

      using var srcColor = Cv2.ImDecode(photoInputBytes, ImreadModes.Color);
      if (srcColor.Empty())
        throw new InvalidOperationException("Invalid image.");

      using var srcGray = new Mat();
      Cv2.CvtColor(srcColor, srcGray, ColorConversionCodes.BGR2GRAY);

      // First try to flatten the page perspective.
      using var alignedColor = perspective && TryFindPageQuad(srcGray, out var quad)
        ? WarpToTopDown(srcColor, quad, marginPct: 0.01)
        : srcColor.Clone();

      using var alignedGray = new Mat();
      Cv2.CvtColor(alignedColor, alignedGray, ColorConversionCodes.BGR2GRAY);

      // Estimate skew after perspective correction.
      var angle = EstimateSkewAngle(alignedGray);

      // Rotate opposite to the measured skew.
      using var rotatedColor = RotateKeepContent(alignedColor, -angle);
      using var rotatedGray = RotateKeepContent(alignedGray, -angle);

      using var outMat = new Mat();
      Cv2.GaussianBlur(rotatedColor, outMat, new Size(3, 3), 0);

      if (Math.Abs(scale - 1.0) > 0.01) {
        using var resized = new Mat();
        Cv2.Resize(outMat, resized, new Size(), scale, scale, InterpolationFlags.Cubic);
        return resized.ImEncode(".png");
      }

      return outMat.ImEncode(".png");
    }

    // ---------- Skew detection (rotation) ----------

    private static double EstimateSkewAngle(Mat gray) {
      using var norm = new Mat();
      using var bin = new Mat();
      using var inv = new Mat();
      using var edges = new Mat();

      Cv2.GaussianBlur(gray, norm, new Size(3, 3), 0);

      Cv2.AdaptiveThreshold(
        norm,
        bin,
        255,
        AdaptiveThresholdTypes.GaussianC,
        ThresholdTypes.Binary,
        31,
        15);

      Cv2.BitwiseNot(bin, inv);
      Cv2.Canny(inv, edges, 50, 150);

      var lines = Cv2.HoughLinesP(
        edges,
        1,
        Math.PI / 180,
        threshold: 100,
        minLineLength: Math.Max(gray.Cols / 4, 100),
        maxLineGap: 20);

      if (lines.Length == 0)
        return 0;

      var angles = new List<double>(lines.Length);

      foreach (var l in lines) {
        var dx = l.P2.X - l.P1.X;
        var dy = l.P2.Y - l.P1.Y;
        if (dx == 0)
          continue;

        var angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        var normalized = angle > 90 ? angle - 180 : (angle < -90 ? angle + 180 : angle);

        if (Math.Abs(normalized) <= 15)
          angles.Add(normalized);
      }

      if (angles.Count == 0)
        return 0;

      angles.Sort();
      var median = angles[angles.Count / 2];

      return Math.Max(-10.0, Math.Min(10.0, median));
    }

    private static Mat RotateKeepContent(Mat src, double angleDeg) {
      if (src == null || src.Empty())
        throw new ArgumentException("Source image is null or empty.", nameof(src));

      if (Math.Abs(angleDeg) < 0.2)
        return src.Clone();

      var center = new Point2f(src.Cols / 2f, src.Rows / 2f);
      using var rot = Cv2.GetRotationMatrix2D(center, angleDeg, 1.0);

      var absCos = Math.Abs(rot.Get<double>(0, 0));
      var absSin = Math.Abs(rot.Get<double>(0, 1));

      var boundW = (int)Math.Ceiling(src.Rows * absSin + src.Cols * absCos);
      var boundH = (int)Math.Ceiling(src.Rows * absCos + src.Cols * absSin);

      rot.Set(0, 2, rot.Get<double>(0, 2) + boundW / 2.0 - center.X);
      rot.Set(1, 2, rot.Get<double>(1, 2) + boundH / 2.0 - center.Y);

      var dst = new Mat();
      Cv2.WarpAffine(
        src,
        dst,
        rot,
        new Size(boundW, boundH),
        InterpolationFlags.Linear,
        BorderTypes.Constant,
        Scalar.White);

      return dst;
    }

    // ---------- Perspective correction ----------

    private static bool TryFindPageQuad(Mat gray, out Point2f[] quad) {
      const int maxDim = 1200;
      double scale = 1.0;

      using var small = ResizeKeepingAspect(gray, maxDim, ref scale);
      using var blur = new Mat();
      using var edges = new Mat();
      using var dil = new Mat();

      Cv2.GaussianBlur(small, blur, new Size(5, 5), 0);
      Cv2.Canny(blur, edges, 50, 150);

      using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
      Cv2.Dilate(edges, dil, kernel);

      Cv2.FindContours(dil, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

      double bestArea = 0;
      Point2f[]? best = null;

      foreach (var contour in contours) {
        var peri = Cv2.ArcLength(contour, true);
        var approx = Cv2.ApproxPolyDP(contour, 0.02 * peri, true);

        if (approx.Length != 4 || !Cv2.IsContourConvex(approx))
          continue;

        var area = Math.Abs(Cv2.ContourArea(approx));
        if (area < (small.Rows * small.Cols) * 0.10)
          continue;

        if (area > bestArea) {
          bestArea = area;
          best = approx
            .Select(p => new Point2f(p.X / (float)scale, p.Y / (float)scale))
            .ToArray();
        }
      }

      if (best is null) {
        quad = Array.Empty<Point2f>();
        return false;
      }

      quad = OrderCorners(best);
      return true;
    }

    private static Mat WarpToTopDown(Mat src, Point2f[] quad, double marginPct) {
      float wA = Distance(quad[2], quad[3]);
      float wB = Distance(quad[1], quad[0]);
      float hA = Distance(quad[1], quad[2]);
      float hB = Distance(quad[0], quad[3]);

      float maxW = Math.Max(wA, wB);
      float maxH = Math.Max(hA, hB);

      var mw = (float)(marginPct * maxW);
      var mh = (float)(marginPct * maxH);

      var dstW = Math.Max(1, (int)Math.Round(maxW + 2 * mw));
      var dstH = Math.Max(1, (int)Math.Round(maxH + 2 * mh));

      var srcPts = new[] { quad[0], quad[1], quad[2], quad[3] };
      var dstPts = new[]
      {
        new Point2f(mw, mh),
        new Point2f(dstW - 1 - mw, mh),
        new Point2f(dstW - 1 - mw, dstH - 1 - mh),
        new Point2f(mw, dstH - 1 - mh)
      };

      using var matrix = Cv2.GetPerspectiveTransform(srcPts, dstPts);

      var warped = new Mat();
      Cv2.WarpPerspective(
        src,
        warped,
        matrix,
        new Size(dstW, dstH),
        InterpolationFlags.Linear,
        BorderTypes.Constant,
        Scalar.White);

      return warped;
    }

    private static Point2f[] OrderCorners(Point2f[] pts) {
      if (pts == null || pts.Length != 4)
        throw new ArgumentException("Exactly 4 points are required.", nameof(pts));

      var sum = pts.Select(p => p.X + p.Y).ToArray();
      var diff = pts.Select(p => p.X - p.Y).ToArray();

      var tl = pts[Array.IndexOf(sum, sum.Min())];
      var br = pts[Array.IndexOf(sum, sum.Max())];
      var tr = pts[Array.IndexOf(diff, diff.Max())];
      var bl = pts[Array.IndexOf(diff, diff.Min())];

      return new[] { tl, tr, br, bl };
    }

    private static Mat ResizeKeepingAspect(Mat src, int maxDim, ref double scale) {
      var h = src.Rows;
      var w = src.Cols;
      var maxCurrent = Math.Max(h, w);

      if (maxCurrent <= maxDim) {
        scale = 1.0;
        return src.Clone();
      }

      scale = (double)maxDim / maxCurrent;

      var dst = new Mat();
      Cv2.Resize(
        src,
        dst,
        new Size((int)Math.Round(w * scale), (int)Math.Round(h * scale)),
        0,
        0,
        InterpolationFlags.Area);

      return dst;
    }

    private static float Distance(Point2f a, Point2f b)
      => (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
  }
}