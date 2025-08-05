using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;


namespace JenianAPI.Helpers
{
  public class ImageResizeHelper
  {
    /// <summary>
    /// Resize image proportionally (no crop) and compress to JPEG.
    /// </summary>
    /// <param name="imageBytes">Input image byte array</param>
    /// <param name="targetWidth">Target width in pixels (e.g., 1024)</param>
    /// <param name="quality">JPEG Quality (1-100)</param>
    /// <returns>Base64 Data URL string</returns>
    public static string ResizeCompressAndGetDataUrl(string base64Image, int targetWidth = 1024, int quality = 75) {
      // Remove prefix if exists (data:image/jpeg;base64,...)
      var base64Data = base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image;

      // Convert Base64 string to byte[]
      byte[] imageBytes = Convert.FromBase64String(base64Data);

      // Resize & Compress
      using var inputStream = new MemoryStream(imageBytes);
      using var image = Image.Load(inputStream);

      var resizeOptions = new ResizeOptions {
        Mode = ResizeMode.Max,
        Size = new Size(targetWidth, 0) // Maintain aspect ratio
      };

      image.Mutate(x => x.Resize(resizeOptions));

      var encoder = new JpegEncoder {
        Quality = quality
      };

      using var outputStream = new MemoryStream();
      image.Save(outputStream, encoder);

      // Convert back to Base64
      var compressedBytes = outputStream.ToArray();
      var compressedBase64 = Convert.ToBase64String(compressedBytes);

      // Return Data URL format (for OpenAI Vision)
      return $"data:image/jpeg;base64,{compressedBase64}";
    }
  }
}

