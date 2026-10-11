using System;
using System.IO;
using SkiaSharp;

namespace FunctionsV2.Sys_StudentPhoto
{
    public static class ImageResizeHelper
    {
        public static byte[] ResizeJpeg(byte[] source, int maxWidth = 160, int quality = 65)
        {
            if (source == null || source.Length == 0)
                throw new ArgumentException("Image source is empty.", nameof(source));

            quality = Math.Clamp(quality, 1, 100);

            using var input = new MemoryStream(source);
            using var managedStream = new SKManagedStream(input);
            using var codec = SKCodec.Create(managedStream) ?? throw new InvalidOperationException("Invalid image data.");

            var info = codec.Info;
            var bitmap = new SKBitmap(info);
            var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels());

            if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
                throw new InvalidOperationException($"Unable to decode image. Codec result: {result}");

            var targetWidth = bitmap.Width > maxWidth ? maxWidth : bitmap.Width;
            var ratio = (double)targetWidth / bitmap.Width;
            var targetHeight = Math.Max(1, (int)Math.Round(bitmap.Height * ratio));

            var targetInfo = new SKImageInfo(targetWidth, targetHeight, bitmap.ColorType, bitmap.AlphaType);

            using var scaledBitmap = new SKBitmap(targetInfo);
            var scaled = bitmap.ScalePixels(scaledBitmap, new SKSamplingOptions(SKCubicResampler.Mitchell));
            if (!scaled)
                throw new InvalidOperationException("Image resize failed.");

            using var image = SKImage.FromBitmap(scaledBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            if (data == null)
                throw new InvalidOperationException("JPEG encode failed.");

            return data.ToArray();
        }
    }
}