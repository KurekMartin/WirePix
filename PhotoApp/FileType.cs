using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
