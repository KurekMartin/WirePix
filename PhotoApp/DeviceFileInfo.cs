using MediaDevices;
using PhotoApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public enum FilterType
        {
            All, NewFiles, FileTypes, DateRange
        }
        private readonly MediaDevice _device;
        private ObservableCollection<BaseFileInfo> _allFilesInfo = new ObservableCollection<BaseFileInfo>();
        private ObservableCollection<string> _allFileTypes = new ObservableCollection<string>();

        private List<BaseFileInfo> _filesFilterByDateRange;
        private List<BaseFileInfo> _filesFilterByNew;
        private List<BaseFileInfo> _filesFilterByType;

        private IEnumerable<string> _images;
        private IEnumerable<string> _videos;
        private IEnumerable<string> _others;

        private bool _cancelOperation = false;
        private bool _isFilterByDateRangeValid = false;
        private bool _isFilterByNewValid = false;
        private bool _isFilterByTypeValid = false;


        public DeviceFileInfo(MediaDevice device)
        {
            _device = device;
            _allFilesInfo.CollectionChanged += AllFilesInfo_CollectionChanged;
            _allFileTypes.CollectionChanged += AllFileTypes_CollectionChanged;
            MainWindow.DownloadSettings.Date.PropertyChanged += Date_PropertyChanged;
        }

        private void Date_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _isFilterByDateRangeValid = false;
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
                                (DateTime)file.CreationTime,
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
                            InvalidateFilters(DeviceFileInfo.FilterType.All);
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
                return _allFilesInfo.Where(f => f.CreationTime == DateTime.MinValue).Count();
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

        public void InvalidateFilters(FilterType type)
        {
            switch (type)
            {
                case FilterType.NewFiles:
                    _isFilterByNewValid = false;
                    break;
                case FilterType.DateRange:
                    _isFilterByDateRangeValid = false;
                    break;
                case FilterType.FileTypes:
                    _isFilterByTypeValid = false;
                    break;
                case FilterType.All:
                    _isFilterByDateRangeValid = _isFilterByNewValid = _isFilterByTypeValid = false;
                    break;
            }
        }

        public async Task<IEnumerable<BaseFileInfo>> FilterFiles(FileTypeSelection selection)
        {
            IEnumerable<BaseFileInfo> fileFilterByDate = Enumerable.Empty<BaseFileInfo>();
            switch (MainWindow.DownloadSettings.DownloadSelect)
            {
                case DownloadSelect.newFiles:
                    fileFilterByDate = await FilterNewFiles();
                    break;
                case DownloadSelect.dateRange:
                    fileFilterByDate = await FilterByDateRange();
                    break;
            }
            IEnumerable<BaseFileInfo> fileFilterByType = await FilterByType(selection);
            return fileFilterByDate.Intersect(fileFilterByType);
        }

        public async Task<IEnumerable<BaseFileInfo>> FilterByType(FileTypeSelection selection)
        {
            IEnumerable<BaseFileInfo> files = _allFilesInfo;

            if (MainWindow.DownloadSettings.FileTypeSelectMode == FileTypeSelectMode.selection)
            {
                if (_filesFilterByType == null || !_isFilterByTypeValid)
                {
                    Debug.WriteLine("Filtering files by type");
                    if (selection.Mode == ListMode.whitelist)
                    {
                        _filesFilterByType = await Task.FromResult(files.Where(f => selection.FileTypes.Contains(Path.GetExtension(f.FullPath).TrimStart('.').ToUpper())).ToList());
                    }
                    else
                    {
                        _filesFilterByType = await Task.FromResult(files.Where(f => !selection.FileTypes.Contains(Path.GetExtension(f.FullPath).TrimStart('.').ToUpper())).ToList());
                    }
                    _isFilterByTypeValid = true;
                }
            }
            else if (MainWindow.DownloadSettings.FileTypeSelectMode == FileTypeSelectMode.all)
            {
                return files.ToList();
            }
            return _filesFilterByType;
        }

        private async Task<IEnumerable<BaseFileInfo>> FilterByDateRange()
        {
            IEnumerable<BaseFileInfo> files = _allFilesInfo;
            if (_filesFilterByDateRange == null || !_isFilterByDateRangeValid)
            {
                Debug.WriteLine("Filtering files by date range");
                DateRange dateRange = MainWindow.DownloadSettings.Date;
                _filesFilterByDateRange = await Task.FromResult(files.Where(f =>
                {
                    if (f.CreationTime.Date != DateTime.MinValue)
                    {
                        return f.CreationTime.Date >= dateRange.Start.Date && f.CreationTime.Date <= dateRange.End.Date;
                    }
                    else
                    {
                        return f.LastWriteTime.Date >= dateRange.Start.Date && f.LastWriteTime.Date <= dateRange.End.Date;
                    }
                }).ToList());
                _isFilterByDateRangeValid = true;
            }
            return _filesFilterByDateRange;
        }

        private async Task<IEnumerable<BaseFileInfo>> FilterNewFiles()
        {
            IEnumerable<BaseFileInfo> files = _allFilesInfo;
            if (_filesFilterByNew == null || !_isFilterByNewValid)
            {
                Debug.WriteLine("Filtering files by new");
                _filesFilterByNew = await Task.FromResult(files.Where(f => !Database.FileDownloaded(f.PersistentUniqueId, f.FullPath)).ToList());
                _isFilterByNewValid = true;
            }
            return _filesFilterByNew;
        }
    }
}
