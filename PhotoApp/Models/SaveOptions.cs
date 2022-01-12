using System.Xml.Serialization;

namespace PhotoApp.Models
{
    public class SaveOptions : BaseObserveObject
    {
        private string _fileName = string.Empty;
        private bool _root { get; set; } = false;
        private bool _folderStruct { get; set; } = false;
        private bool _fileStruct { get; set; } = false;
        private bool _backup { get; set; } = false;
        private bool _thumbnails { get; set; } = false;
        private bool _fileCheck { get; set; } = false;
        private bool _deleteFiles { get; set; } = false;

        [XmlIgnore]
        public string FileName
        {
            get => _fileName;
            set
            {
                if (value != _fileName)
                {
                    _fileName = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool Root
        {
            get => _root;
            set
            {
                if (value != _root)
                {
                    _root = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool FolderStruct
        {
            get => _folderStruct;
            set
            {
                if (value != _folderStruct)
                {
                    _folderStruct = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool FileStruct
        {
            get => _fileStruct;
            set
            {
                if (value != _fileStruct)
                {
                    _fileStruct = value;
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
        public bool Thumbnails
        {
            get => _thumbnails;
            set
            {
                if (value != _thumbnails)
                {
                    _thumbnails = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool FileCheck
        {
            get => _fileCheck;
            set
            {
                if (value != _fileCheck)
                {
                    _fileCheck = value;
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
    }
}
