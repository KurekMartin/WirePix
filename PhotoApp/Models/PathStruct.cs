using System;
using System.Collections.Generic;

namespace PhotoApp.Models
{
    public class PathStruct : BaseObserveObject
    {
        private string _root = string.Empty;
        private List<List<string>> _folderTags = new List<List<string>>();
        private List<string> _fileTags = new List<string>();
        private string _backup = string.Empty;
        private string _thumbnail = string.Empty;

        public string Root
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
        public List<List<string>> FolderTags
        {
            get => _folderTags;
            set
            {
                if (value != _folderTags)
                {
                    _folderTags = value;
                    OnPropertyChanged();
                }
            }
        }
        public List<string> FileTags
        {
            get => _fileTags;
            set
            {
                if (value != _fileTags)
                {
                    _fileTags = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Backup
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
        public string Thumbnail
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
    }
}
