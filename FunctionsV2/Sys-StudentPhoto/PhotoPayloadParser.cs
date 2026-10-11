using System;
using System.Linq;
using System.Text;

namespace FunctionsV2.Sys_StudentPhoto
{
    public static class PhotoPayloadParser
    {
        public static byte[] ParseToImageBytes(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Photo payload is empty.");

            var s = input.Trim();

            // 1) Hex format: 0xFFD8 / or FFD8
            if (IsHex(s))
            {
                if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    s = s[2..];

                var bytes = Convert.FromHexString(s);
                return TryDecodeLegacyBase64Bytes(bytes);
            }

            // 2) Base64 format: /9j/4AAQ...
            try
            {
                var bytes = Convert.FromBase64String(s);
                return TryDecodeLegacyBase64Bytes(bytes);
            }
            catch
            {
                // continue
            }

            throw new FormatException("Unsupported photo format. Provide base64 image, or hex (0x...).");
        }

        private static byte[] TryDecodeLegacyBase64Bytes(byte[] bytes)
        {
            // If this is already JPEG/PNG, return as-is
            if (IsImage(bytes)) return bytes;

            // Legacy: bytes are UTF8 text that itself is base64 image
            var text = Encoding.UTF8.GetString(bytes).Trim();
            if (IsBase64(text))
            {
                try
                {
                    var decoded = Convert.FromBase64String(text);
                    if (IsImage(decoded)) return decoded;
                }
                catch { }
            }

            return bytes;
        }

        private static bool IsImage(byte[] b)
        {
            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return true; // JPEG
            if (b.Length >= 8 &&
                b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 &&
                b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A) return true; // PNG
            return false;
        }

        private static bool IsHex(string s)
        {
            var x = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s[2..] : s;
            return x.Length >= 8 && x.Length % 2 == 0 && x.All(Uri.IsHexDigit);
        }

        private static bool IsBase64(string s)
        {
            s = s.Trim();
            if (s.Length < 16 || s.Length % 4 != 0) return false;
            return s.All(c => char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '=');
        }
    }
}