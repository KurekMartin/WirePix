using MediaDevices;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;


namespace PhotoApp
{
    static class FileExif
    {
        // některé soubory nemusí mít exif data - nutné použít TRY
        public static DateTime GetDateTimeOriginal(string path)
        {
            DateTime dt = new DateTime();
            try
            {
                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(path);
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                var date = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);

                if (date != null)
                {
                    dt = DateTime.ParseExact(date, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
                }
            }
            catch { }
            return dt;
        }

        public static (DateTime date, bool isExact) GetCreationDateTime(MediaDevice device, string fileID)
        {
            bool isExact = false;
            bool isConnected = device.IsConnected;
            if (!isConnected) { device.Connect(); }
            MediaFileSystemInfo file = device.GetFileSystemInfoFromPersistentUniqueId(fileID);
            DateTime? dt = file.DateAuthored;
            if (dt != null && dt != DateTime.MinValue)
            {
                isExact= true;
            }
            else
            {
                dt = file.CreationTime;
                if (dt == null || dt == DateTime.MinValue)
                {
                    dt = file.LastWriteTime;
                }
                if (dt == null || dt == DateTime.MinValue)
                {
                    dt = new DateTime();
                }
            }

            if (!isConnected) { device.Disconnect(); }

            return ((DateTime)dt,isExact);
        }

        public static string GetManufacturer(string path)
        {
            string manufacturer = string.Empty;
            try
            {
                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(path);
                var ifd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                manufacturer = ifd0Directory?.GetDescription(ExifDirectoryBase.TagMake);
            }
            catch { }
            return manufacturer;
        }

        public static string GetModel(string path)
        {
            string model = string.Empty;
            try
            {
                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(path);
                var ifd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                model = ifd0Directory?.GetDescription(ExifDirectoryBase.TagModel);
            }
            catch { }
            return model;
        }
    }
}
