using MediaDevices;
using PhotoApp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PhotoApp
{

    public class DeviceFileInfo : BaseObserveObject
    {
        private struct BaseFileInfo
        {
            public BaseFileInfo(string PersistentUniqueId, string fullPath)
            {
                this.PersistentUniqueId = PersistentUniqueId;
                this.FullPath = fullPath;
            }
            public string PersistentUniqueId;
            public string FullPath;
        }
        private readonly MediaDevice _device;
        private List<BaseFileInfo> _allFilesInfo = new List<BaseFileInfo>();
        private List<string> _allFileTypes = new List<string>();
        private IEnumerable<string> _images;
        private IEnumerable<string> _videos;
        private IEnumerable<string> _others;

        private bool _cancelOperation = false;

        public DeviceFileInfo(MediaDevice device)
        {
            _device = device;
        }

        public void SetFiles(IEnumerable<MediaFileInfo> mediaFiles)
        {
            foreach (var file in mediaFiles)
            {
                if (!_cancelOperation)
                {
                    _allFilesInfo.Add(new BaseFileInfo(file.PersistentUniqueId, file.FullName));
                }
            }

            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                OnPropertyChanged(nameof(AllFilesCount));
            }));
        }

        public void CancelOperation()
        {
            _cancelOperation = true;
        }

        public int AllFilesCount
        {
            get
            {
                return _allFilesInfo.Count;
            }
        }

        public List<string> AllFileTypes
        {
            get
            {
                if (AllFilesCount > 0 && _allFileTypes.Count == 0)
                {
                    AllFileTypes = _allFilesInfo.Select(f => Path.GetExtension(f.FullPath).ToUpper().TrimStart('.')).Distinct().OrderBy(f => f).ToList();
                }
                return _allFileTypes;
            }
            private set
            {
                _allFileTypes = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<string> Images
        {
            get
            {
                if (_images == null)
                {
                    _images = AllFileTypes.Where(f => FileType.IsImage(f));
                }
                return _images;
            }
        }
        public IEnumerable<string> Videos
        {
            get
            {
                if (_videos == null)
                {
                    _videos = AllFileTypes.Where(f => FileType.IsVideo(f));
                }
                return _videos;
            }
        }
        public IEnumerable<string> Others
        {
            get
            {
                if (_others == null)
                {
                    _others = AllFileTypes.Except(_images.Concat(_videos));
                }
                return _others;
            }
        }
    }
}
