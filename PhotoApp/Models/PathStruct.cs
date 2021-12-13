using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoApp.Models
{
    public class PathStruct : BaseObserveObject
    {
        private string _root = string.Empty;
        private string _folderTags = string.Empty;
        private string _fileTags = string.Empty;
        private string _backup = string.Empty;
        private string _thumbnail = string.Empty;
        public string Root
        {
            get
            {
                return _root;
            }
            set
            {
                if (value != _root)
                {
                    _root = value;
                    OnPropertyChanged();
                }
            }
        }
        public string FolderTags
        {
            get
            {
                return _folderTags;
            }
            set
            {
                if (value != _folderTags)
                {
                    _folderTags = value;
                    OnPropertyChanged();
                }
            }
        }
        public string FileTags
        {
            get
            {
                return _fileTags;
            }
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
            get
            {
                return _backup;
            }
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
            get
            {
                return _thumbnail;
            }
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
