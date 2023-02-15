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
        public struct BaseFileInfo
        {
            public BaseFileInfo(string persistentUniqueId, string fullPath, DateTime creationTime, ulong size)
            {
                PersistentUniqueId = persistentUniqueId;
                FullPath = fullPath;
                CreationTime= creationTime;
                Size = size;
            }
            public string PersistentUniqueId;
            public string FullPath;
            public DateTime CreationTime;
            public ulong Size;
        }
        private readonly MediaDevice _device;
        private List<BaseFileInfo> _allFilesInfo = new List<BaseFileInfo>();
        private List<string> _allFileTypes = new List<string>();
        private IEnumerable<string> _images;
        private IEnumerable<string> _videos;
        private IEnumerable<string> _others;

        private bool _cancelOperation = false;
        private bool _isUpdated = false;

        public DeviceFileInfo(MediaDevice device)
        {
            _device = device;
        }

        public async Task AddFiles(IEnumerable<MediaFileInfo> mediaFiles)
        {
            await Task.Run(() =>
            {
                try
                {
                    foreach (var file in mediaFiles)
                    {
                        if (!_cancelOperation)
                        {
                            _allFilesInfo.Add(new BaseFileInfo(file.PersistentUniqueId, file.FullName, (DateTime)file.CreationTime, file.Length));
                        }
                    }

                }
                catch (Exception ex)
                {
                    _cancelOperation = true;
                }
            });
            _isUpdated = false;
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
                if (AllFilesCount > 0 && !_isUpdated)
                {
                    AllFileTypes = _allFilesInfo.Select(f => Path.GetExtension(f.FullPath).ToUpper().TrimStart('.')).Distinct().OrderBy(f => f).ToList();
                }
                return _allFileTypes;
            }
            private set
            {
                if (_allFileTypes != value)
                {
                    _allFileTypes = value;
                    _isUpdated = true;
                    OnPropertyChanged();
                }
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

        public IEnumerable<BaseFileInfo> FilterByType(FileTypeSelection selection)
        {
            if (selection.Mode == ListMode.whitelist)
            {
                return _allFilesInfo.Where(f => selection.FileTypes.Contains(Path.GetExtension(f.FullPath).TrimStart('.').ToUpper()));
            }
            else
            {
                return _allFilesInfo.Where(f => !selection.FileTypes.Contains(Path.GetExtension(f.FullPath).TrimStart('.').ToUpper()));
            }
        }
    }
}
