using ImageMagick;

namespace PhotoApp
{
    internal class FileType
    {
        public static bool IsImage(string fileName)
        {
            var format = MagickFormatInfo.Create(fileName);
            return format != null && (format.ModuleFormat == MagickFormat.Dng || (format.MimeType != null && format.MimeType.Contains("image")));
        }
        public static bool IsVideo(string fileName)
        {
            var format = MagickFormatInfo.Create(fileName);
            return format != null && (format.MimeType != null && format.MimeType.Contains("video") || format.Description.ToLower().Contains("video"));
        }
    }
}
