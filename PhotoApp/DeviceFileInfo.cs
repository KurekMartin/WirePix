using MediaDevices;
using PhotoApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            public BaseFileInfo(string persistentUniqueId, string fullPath, DateTime creationTime, DateTime lastWriteTime, ulong size)
            {
                PersistentUniqueId = persistentUniqueId;
                FullPath = fullPath;
                CreationTime = creationTime;
                LastWriteTime = lastWriteTime;
                Size = size;
            }
            public string PersistentUniqueId;
            public string FullPath;
            public DateTime CreationTime;
            public DateTime LastWriteTime;
            public ulong Size;
        }
        private readonly MediaDevice _device;
        private ObservableCollection<BaseFileInfo> _allFilesInfo = new ObservableCollection<BaseFileInfo>();
        private ObservableCollection<string> _allFileTypes = new ObservableCollection<string>();
        private IEnumerable<string> _images;
        private IEnumerable<string> _videos;
        private IEnumerable<string> _others;

        private bool _cancelOperation = false;


        public DeviceFileInfo(MediaDevice device)
        {
            _device = device;
            _allFilesInfo.CollectionChanged += AllFilesInfo_CollectionChanged;
            _allFileTypes.CollectionChanged += AllFileTypes_CollectionChanged;
        }

        private void AllFileTypes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(AllFileTypes));
            });
        }

        private void AllFilesInfo_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(AllFilesCount));
                OnPropertyChanged(nameof(EstimateDateNum));
            });
        }

        public async Task<int> AddFiles(IEnumerable<MediaFileInfo> mediaFiles)
        {
            int count = 0;
            await Task.Run(() =>
            {
                try
                {
                    foreach (var file in mediaFiles)
                    {
                        if (!_cancelOperation)
                        {
                            BaseFileInfo fileInfo = new BaseFileInfo(
                                file.PersistentUniqueId,
                                file.FullName,
                                (DateTime)file.DateAuthored,
                                (DateTime)file.LastWriteTime,
                                file.Length);
                            _allFilesInfo.Add(fileInfo);

                            string extenstion = Path.GetExtension(file.Name).ToUpper().TrimStart('.');
                            if (!AllFileTypes.Contains(extenstion))
                            {
                                int index = AllFileTypes.ToList().FindLastIndex(e => string.Compare(e, extenstion) < 0);
                                AllFileTypes.Insert(index + 1, extenstion);
                            }
                            count++;
                        }
                    }

                }
                catch (Exception ex)
                {
                    _cancelOperation = true;
                }
            });
            return count;
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

        public int EstimateDateNum
        {
            get
            {
                return _allFilesInfo.Where(f => f.CreationTime != DateTime.MinValue).Count();
            }
        }

        public ObservableCollection<string> AllFileTypes
        {
            get => _allFileTypes;
            private set
            {
                if (_allFileTypes != value)
                {
                    _allFileTypes = value;
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

        public IEnumerable<BaseFileInfo> FilterByType(FileTypeSelection selection, IEnumerable<BaseFileInfo> fileList = null)
        {
            IEnumerable<BaseFileInfo> files = _allFilesInfo;
            if (fileList != null)
            {
                files = fileList;
            }
            if (MainWindow.DownloadSettings.FileTypeSelectMode == FileTypeSelectMode.selection)
            {
                if (selection.Mode == ListMode.whitelist)
                {
                    return files.Where(f => selection.FileTypes.Contains(Path.GetExtension(f.FullPath).TrimStart('.').ToUpper()));
                }
                else
                {
                    return files.Where(f => !selection.FileTypes.Contains(Path.GetExtension(f.FullPath).TrimStart('.').ToUpper()));
                }
            }
            return files;
        }
        public IEnumerable<BaseFileInfo> FilterByDate(IEnumerable<BaseFileInfo> fileList = null)
        {
            IEnumerable<BaseFileInfo> files = _allFilesInfo;
            if (fileList != null)
            {
                files = fileList;
            }

            if (MainWindow.DownloadSettings.DownloadSelect == DownloadSelect.dateRange)
            {
                DateRange dateRange = MainWindow.DownloadSettings.Date;
                return files.Where(f =>
                {
                    if (f.CreationTime.Date == DateTime.MinValue)
                    {
                        return f.CreationTime.Date >= dateRange.Start.Date && f.CreationTime.Date <= dateRange.End.Date;
                    }
                    else
                    {
                        return f.LastWriteTime.Date >= dateRange.Start.Date && f.CreationTime.Date <= dateRange.End.Date;
                    }
                });
            }
            return files;
        }
    }
}
