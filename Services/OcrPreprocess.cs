using OpenCvSharp;

namespace JenianAPI.Services
{
  public class OcrPreprocess
  {

    public static byte[] PhotoCleanUp(byte[] photoInput, double scale = 1.25) {
      using var src = Cv2.ImDecode(photoInput, ImreadModes.Color);
      if (src.Empty()) throw new InvalidOperationException("Invalid image");

      // grayscale
      using var gray = new Mat();
      Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

      // mild deskew using Hough lines
      var angle = EstimateSkewAngle(gray); // returns ~[-5°, +5°]
      using var rot = RotateKeepContent(gray, angle);

      // contrast normalize (CLAHE) + denoise
      using var clahe = Cv2.CreateCLAHE(clipLimit: 3.0, tileGridSize: new Size(8, 8));
      using var norm = new Mat(); clahe.Apply(rot, norm);
      using var de = new Mat(); Cv2.BilateralFilter(norm, de, 9, 75, 75);

      // adaptive threshold to crisp text
      using var bin = new Mat();
      Cv2.AdaptiveThreshold(de, bin, 255,
          AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 31, 10);

      // remove grid lines (rosters!)
      using var clean = RemoveGridLines(bin);

      if (Math.Abs(scale - 1.0) > 0.01) {
        using var up = new Mat();
        Cv2.Resize(gray, up, new Size(), scale, scale, InterpolationFlags.Cubic);
        return up.ImEncode(".png");   // crisp, OCR-friendly
      }
      return gray.ImEncode(".png");

    }

    static float EstimateSkewAngle(Mat gray) {
      using var edges = new Mat();
      Cv2.Canny(gray, edges, 50, 150);
      var lines = Cv2.HoughLines(edges, 1, Math.PI / 180, 200);
      if (lines.Length == 0) return 0;

      var degs = lines
          .Select(l => l.Theta * 180.0 / Math.PI)
          .Where(a => a < 20 || a > 160)                // near-horizontal
          .Select(a => a > 90 ? a - 180 : a)            // to [-90,90]
          .ToArray();

      var avg = degs.Length == 0 ? 0 : degs.Average();
      return (float)Math.Max(-5, Math.Min(5, avg));            // clamp outliers
    }

    static Mat RotateKeepContent(Mat src, float angleDeg) {
      if (Math.Abs(angleDeg) < 0.5) return src.Clone();
      var c = new Point2f(src.Cols / 2f, src.Rows / 2f);
      var m = Cv2.GetRotationMatrix2D(c, angleDeg, 1.0);
      var box = new RotatedRect(c, new Size2f(src.Cols, src.Rows), angleDeg).BoundingRect();
      m.Set(0, 2, m.Get<double>(0, 2) + box.Width / 2 - c.X);
      m.Set(1, 2, m.Get<double>(1, 2) + box.Height / 2 - c.Y);
      var dst = new Mat();
      Cv2.WarpAffine(src, dst, m, box.Size, InterpolationFlags.Cubic, BorderTypes.Constant, Scalar.White);
      return dst;
    }

    static Mat RemoveGridLines(Mat bin) {
      using var inv = new Mat(); Cv2.BitwiseNot(bin, inv);

      int h = Math.Max(10, inv.Cols / 30);
      using var hk = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(h, 1));
      using var hLines = new Mat(); Cv2.Erode(inv, hLines, hk); Cv2.Dilate(hLines, hLines, hk);

      int v = Math.Max(10, inv.Rows / 30);
      using var vk = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, v));
      using var vLines = new Mat(); Cv2.Erode(inv, vLines, vk); Cv2.Dilate(vLines, vLines, vk);

      using var lines = new Mat(); Cv2.BitwiseOr(hLines, vLines, lines);
      using var linesInv = new Mat(); Cv2.BitwiseNot(lines, linesInv);

      var outBin = new Mat();
      Cv2.BitwiseAnd(bin, linesInv, outBin);
      return outBin;
    }
  }
}
