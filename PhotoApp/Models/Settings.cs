using PhotoApp.Models;
using System;
using System.IO;
using System.Windows;
using System.Xml.Serialization;

namespace PhotoApp
{
    public enum DownloadSelect
    {
        lastBackup,
        dateRange
    }

    public class Settings : BaseObserveObject
    {
        public PathStruct Paths { get; set; }
        [XmlIgnore]
        public DateRange Date { get; set; }
        public Thumbnails ThumbnailSettings { get; set; }
        private bool _checkFiles = false;
        private bool _deleteFiles = false;
        private bool _backup = false;
        private bool _thumbnail = false;
        private DownloadSelect _downloadSelect = DownloadSelect.lastBackup;
        public SaveOptions SaveOptions = new SaveOptions();
        [XmlIgnore]
        private static readonly string _profilesFolder = Application.Current.Resources["profilesFolder"].ToString();

        public Settings()
        {
            Paths = new PathStruct();
            Date = new DateRange();
            ThumbnailSettings = new Thumbnails();
        }

        public void Save(SaveOptions options = null)
        {
            if (options != null)
            {
                SaveOptions = options;
            }
            Directory.CreateDirectory(_profilesFolder);

            var attributes = new XmlAttributes { XmlIgnore = true };

            var overrides = new XmlAttributeOverrides();
            if (!SaveOptions.Root) { overrides.Add(typeof(PathStruct), "Root", attributes); }
            if (!SaveOptions.FolderStruct) { overrides.Add(typeof(PathStruct), "FolderTags", attributes); }
            if (!SaveOptions.FileStruct) { overrides.Add(typeof(PathStruct), "FileTags", attributes); }
            if (!SaveOptions.Backup)
            {
                overrides.Add(typeof(Settings), "Backup", attributes);
                overrides.Add(typeof(PathStruct), "Backup", attributes);
            }
            if (!SaveOptions.Thumbnails)
            {
                overrides.Add(typeof(PathStruct), "Thumbnail", attributes);
                overrides.Add(typeof(Settings), "Thumbnail", attributes);
                overrides.Add(typeof(Settings), "ThumbnailSettings", attributes);
            }
            if (!SaveOptions.FileCheck) { overrides.Add(typeof(Settings), "CheckFiles", attributes); }
            if(!SaveOptions.DeleteFiles) { overrides.Add(typeof(Settings), "DeleteFiles", attributes); }

            XmlSerializer serializer = new XmlSerializer(typeof(Settings), overrides);
            using (FileStream fs = File.Create(Path.Combine(_profilesFolder, $"{SaveOptions.FileName}.xml")))
            {
                TextWriter writer = new StreamWriter(fs);
                serializer.Serialize(writer, this);
                writer.Close();
            }
        }

        public void Load(string profileName)
        {
            var profile = Path.Combine(_profilesFolder, $"{profileName}.xml");
            if (File.Exists(profile))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                using (Stream stream = new FileStream(profile, FileMode.Open))
                {

                    Settings s = (Settings)serializer.Deserialize(stream);
                    if (s.SaveOptions.Root) { Paths.Root = s.Paths.Root; }
                    if (s.SaveOptions.FolderStruct) { Paths.FolderTags = s.Paths.FolderTags; }
                    if (s.SaveOptions.FileStruct) { Paths.FileTags = s.Paths.FileTags; }
                    if (s.SaveOptions.Backup) { 
                        Paths.Backup = s.Paths.Backup;
                        Backup = s.Backup;
                    }
                    if (s.SaveOptions.Thumbnails){
                        Paths.Thumbnail = s.Paths.Thumbnail;
                        Thumbnail = s.Thumbnail;
                        ThumbnailSettings = s.ThumbnailSettings;
                    }
                    if (s.SaveOptions.FileCheck) { _checkFiles = s._checkFiles; }
                    if (s.SaveOptions.DeleteFiles) { _deleteFiles = s._deleteFiles; }
                    SaveOptions = s.SaveOptions;
                    SaveOptions.FileName = profileName;
                }
            }
        }

        public static void Delete(string profileName)
        {
            var profile = Path.Combine(_profilesFolder, $"{profileName}.xml");
            if (File.Exists(profile))
            {
                File.Delete(profile);
            }
        }

        public bool CheckFiles
        {
            get => _checkFiles;
            set
            {
                if (value != _checkFiles)
                {
                    _checkFiles = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DeleteFiles
        {
            get => _deleteFiles;
            set
            {
                if (value != _deleteFiles)
                {
                    _deleteFiles = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Backup
        {
            get => _backup;
            set
            {
                if (value != _backup)
                {
                    _backup = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Thumbnail
        {
            get => _thumbnail;
            set
            {
                if (value != _thumbnail)
                {
                    _thumbnail = value;
                    OnPropertyChanged();
                }
            }
        }
        [XmlIgnore]
        public DownloadSelect DownloadSelect
        {
            get => _downloadSelect;
            set
            {
                if (value != _downloadSelect)
                {
                    _downloadSelect = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
