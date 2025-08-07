using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace JenianAPI.Helpers
{
  public class ImageHelper
  {
    public static async Task<MemoryStream> CompressImageInStream(MemoryStream imageStream) {

      using Image image = await Image.LoadAsync(imageStream);
      // Step 3: Compress by lowering JPEG Quality
      var compressedStream = new MemoryStream();
      var encoder = new JpegEncoder {
        Quality = 70  // <-- Lower this to compress more (range: 0-100)
      };

      await image.SaveAsJpegAsync(compressedStream, encoder);
      return compressedStream;
    }

    public static async Task<byte[]> CompressImageInBytes(byte[] imageBytes, int quality = 70) {

      using var inputStream = new MemoryStream(imageBytes);
      using var image = await Image.LoadAsync(inputStream);

      var outputStream = new MemoryStream();

      var encoder = new JpegEncoder {
        Quality = quality // Lower = smaller file
      };

      await image.SaveAsJpegAsync(outputStream, encoder);

      return outputStream.ToArray(); // Return compressed byte[]
    }
  }
}

