using ImageMagick;
using ImageMagick.Formats;
using MediaDevices;
using PhotoApp.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static PhotoApp.DeviceFileInfo;

namespace PhotoApp
{
    public class Device : BaseObserveObject
    {
        private struct CopyItem
        {
            public CopyItem(string tmpFile, string fullDestPath, byte[] origHash, string origFile, string persistentUID, DateTime lastWriteTime)
            {
                this.tmpFile = tmpFile;
                this.fullDestPath = fullDestPath;
                this.origHash = origHash;
                this.origFile = origFile;
                PersistentUID = persistentUID;
                LastWriteTime = lastWriteTime;
            }
            public string tmpFile;
            public string fullDestPath;
            public byte[] origHash;
            public string origFile;
            public string PersistentUID;
            public DateTime LastWriteTime;
        }

        public int FilesDoneCount { get; private set; } = 0;
        public int FilesTotal { get; private set; } = 0;
        public int Errors { get; private set; } = 0;

        private readonly MediaDevice _device;
        private List<MediaDirectoryInfo> _mediaDirList;
        private double[] _space;
        private DeviceFileInfo _deviceFileInfo;
        private string _customName = "";
        private DeviceType _customType;
        private bool _useCustomType = false;

        public int FilesToCopyCount { get; private set; } = 0;
        private IEnumerable<BaseFileInfo> _filesToDownloadList = Enumerable.Empty<BaseFileInfo>();
        private double sizeToProcess;
        private DownloadSettings _lastSettings;
        public FileTypeSelection FileTypeSelection { get; set; } = new FileTypeSelection();

        private bool fileCheckDone = false;
        private bool searchingFiles = false;
        private bool cancelRequest = false;

        private const int maxAttempts = 5;

        //device status
        public const int DEVICE_UNKNOWN_STATUS = -1;
        public const int DEVICE_CANNOT_CONNECT = 0;
        public const int DEVICE_READY = 1;

        //files status
        public const int DEVICE_FILES_READY = 3;
        public const int DEVICE_FILES_SEARCHING = 2;
        public const int DEVICE_FILES_WAITING = 1;
        public const int DEVICE_FILES_NOT_SEARCHED = 0;
        public const int DEVICE_FILES_CANCELED = -1;
        public const int DEVICE_FILES_ERROR = -2;

        public event EventHandler FileSearchStatusChanged;

        private int _fileSearchStatus = 0;
        private DateTime _lastBackup = new DateTime();

        private readonly Log _log = new Log();

        private readonly object _lockLog = new object();
        private readonly object _lockReport = new object();

        public Device(MediaDevice mediaDevice)
        {
            _device = mediaDevice;
            DeviceFileInfo = new DeviceFileInfo(mediaDevice);
            MainWindow.DownloadSettings.Date.PropertyChanged += DateRange_PropertyChanged;
            MainWindow.DownloadSettings.PropertyChanged += DownloadSettings_PropertyChanged;
            FileTypeSelection.FileTypes.CollectionChanged += FileTypes_CollectionChanged;
            DeviceFileInfo.PropertyChanged += DeviceFileInfo_PropertyChanged;
            LoadFromDatabase();
        }

        private void LoadFromDatabase()
        {
            if (!Database.DeviceExists(origName: Name, manufacturer: Manufacturer, serialNum: SerialNumber))
            {
                DBDeviceInfo deviceInfo = new DBDeviceInfo()
                {
                    OrigName = Name,
                    Manufacturer = Manufacturer,
                    SerialNum = SerialNumber,
                    CustomName = CustomName
                };
                Database.InsertDevice(deviceInfo);
            }
            else
            {
                DBDeviceInfo deviceData = Database.GetDevice(origName: Name, manufacturer: Manufacturer, serialNum: SerialNumber);
                CustomName = deviceData.CustomName;
                LastBackup = deviceData.LastBackup;
                if (deviceData.CustomType != string.Empty)
                {
                    _useCustomType = Enum.TryParse(deviceData.CustomType, out _customType);
                }
            }
        }

        private void DownloadSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            DownloadSettings settings = sender as DownloadSettings;
            string property = e.PropertyName;
            if (property == nameof(DownloadSettings.DownloadSelect) ||
                property == nameof(DownloadSettings.FileTypeSelectMode))
            {
                Debug.WriteLine("DownloadSettings changed");
                GetAsyncFilesToDownloadList();
            }
        }

        private void DeviceFileInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            DeviceFileInfo.InvalidateFilters(DeviceFileInfo.FilterType.All);
            Debug.WriteLine("DeviceFileInfo changed");
            GetAsyncFilesToDownloadList();
        }

        private void FileTypes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DeviceFileInfo.InvalidateFilters(DeviceFileInfo.FilterType.FileTypes);
            Debug.WriteLine("FileTypes changed");
            GetAsyncFilesToDownloadList();
        }

        private void DateRange_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            DeviceFileInfo.InvalidateFilters(DeviceFileInfo.FilterType.DateRange);
            Debug.WriteLine("DateRange changed");
            GetAsyncFilesToDownloadList();
        }

        public string ID
        {
            get
            {
                if (_device != null)
                {
                    return _device.DeviceId;
                }
                return string.Empty;
            }
        }

        public string SerialNumber
        {
            get
            {
                if (_device != null)
                {
                    _device.Connect();
                    var sn = _device.SerialNumber;
                    Disconnect();
                    return sn;
                }
                return string.Empty;
            }
        }
        public int ConnectionStatus
        {
            get
            {
                if (_device != null)
                {
                    try
                    {
                        _device.Connect();
                    }
                    catch
                    {
                        return DEVICE_CANNOT_CONNECT;
                    }
                    Disconnect();
                    return DEVICE_READY;
                }
                return DEVICE_UNKNOWN_STATUS;
            }
        }

        public DeviceType DeviceType
        {
            get
            {
                bool connected = _device.IsConnected;
                if (!connected) { _device.Connect(); }
                DeviceType type = _useCustomType ? _customType : _device.DeviceType;
                if (!connected) { _device.Disconnect(); }
                return type;
            }
        }

        public int FileSearchStatus
        {
            get { return _fileSearchStatus; }
            set
            {
                if (_fileSearchStatus != value)
                {
                    _fileSearchStatus = value;
                    OnPropertyChanged();
                    FileSearchStatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string Manufacturer
        {
            get
            {
                string manufacturer = _device.Manufacturer;
                return manufacturer;
            }
        }

        public string Name
        {
            get
            {
                if (_device != null)
                {
                    return _device.Description;                    
                }
                else
                    return string.Empty;
            }
        }
        public string CustomName
        {
            get
            {
                if (_customName != string.Empty)
                {
                    return _customName;
                }
                else if (_device.FriendlyName != string.Empty)
                {
                    return _device.FriendlyName;
                }
                else
                {
                    return Name;
                }
            }
            set
            {
                if (value != _customName)
                {
                    _customName = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastBackup
        {
            get => _lastBackup;
            set
            {
                if (_lastBackup != value)
                {
                    _lastBackup = value;
                    OnPropertyChanged();
                }
            }
        }

        //uloziste [0]-celkove [1]-volne [2]-obsazene
        public double[] Space
        {
            get
            {
                if (_space == null)
                {
                    _device.Connect();
                    _space = new double[3];
                    var drives = _device.GetDrives();
                    foreach (MediaDriveInfo drive in drives)
                    {
                        _space[0] += drive.TotalSize / (double)1073741824; //prevod na GB
                        _space[1] += drive.AvailableFreeSpace / (double)1073741824;
                    }
                    Disconnect();
                    _space = _space.Select(x => Math.Round(x, 2)).ToArray();
                    _space[2] = Math.Round(_space[0] - _space[1], 2);
                }
                return _space;
            }
        }

        public int FilesToDownloadCount
        {
            get => FilesToDownloadList.Count();
        }

        private IEnumerable<BaseFileInfo> FilesToDownloadList
        {
            get => _filesToDownloadList;
            set
            {
                if (_filesToDownloadList != value)
                {
                    _filesToDownloadList = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FilesToDownloadCount));
                }
            }
        }

        private async void GetAsyncFilesToDownloadList()
        {
            //var files = await DeviceFileInfo.FilterByType(FileTypeSelection);
            //files = await DeviceFileInfo.FilterByDate(fileList: files);
            var files = await DeviceFileInfo.FilterFiles(FileTypeSelection);
            FilesToDownloadList = files;
        }


        //hledani DCIM
        public List<string> MediaDirectories
        {
            get
            {
                if (_mediaDirList == null || _mediaDirList.Count == 0)
                {
                    _device.Connect();
                    var drives = _device.GetDrives();
                    _mediaDirList = new List<MediaDirectoryInfo>();
                    foreach (MediaDriveInfo drive in drives)
                    {
                        MediaDirectoryInfo root = drive.RootDirectory;
                        if (root != null)
                        {
                            GetMediaDirectory(_device, root, _mediaDirList);
                        }
                    }
                    Disconnect();
                    if (_mediaDirList.Count > 0)
                    {
                        OnPropertyChanged(nameof(MediaDirectories));
                    }
                }
                return _mediaDirList.Select(dir => { return dir.FullName; }).ToList();
            }
        }

        public DeviceFileInfo DeviceFileInfo
        {
            get
            {
                return _deviceFileInfo;
            }
            private set
            {
                _deviceFileInfo = value;
                OnPropertyChanged();
            }
        }

        private void Disconnect()
        {
            if (_device.IsConnected && !searchingFiles)
            {
                _device.Disconnect();
            }
        }
        public void CancelCurrentTask()
        {
            if (_device.IsConnected)
            {
                _device.Cancel();
                cancelRequest = true;
                DeviceFileInfo.CancelOperation();
                _device.Disconnect();
            }
        }

        private void GetMediaDirectory(MediaDevice device, MediaDirectoryInfo dir, List<MediaDirectoryInfo> mediaDirList)
        {
            var dirs = dir.EnumerateDirectories();
            var mediaDir = dirs.FirstOrDefault(d => d.Name == "DCIM");

            //pokud nalezeno, dale nehledat
            if (mediaDir != null)
            {
                mediaDirList.Add(mediaDir);
                return;
            }

            //pokud nenalezeno a aktualni adresar obsahuje dalsi slozky, hledat dale
            if (dirs != null)
            {
                foreach (MediaDirectoryInfo d in dirs)
                {
                    GetMediaDirectory(device, d, mediaDirList);
                }
            }
            return;
        }

        public async Task GetAllFiles()
        {
            cancelRequest = false;
            bool success = false;
            if (!searchingFiles)
            {
                searchingFiles = true;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FileSearchStatus = DEVICE_FILES_SEARCHING;
                    DeviceFileInfo = new DeviceFileInfo(_device);
                });

                _device.Connect();
                success = await GetAllFilesFromDirs(MediaDirectories);
                searchingFiles = false;

                if (success && !cancelRequest)
                {
                    FileSearchStatus = DEVICE_FILES_READY;
                }
                else if (cancelRequest)
                {
                    FileSearchStatus = DEVICE_FILES_CANCELED;
                    DeviceFileInfo = new DeviceFileInfo(_device);
                }
                else
                {
                    FileSearchStatus = DEVICE_FILES_ERROR;
                    DeviceFileInfo = new DeviceFileInfo(_device);
                }
            }
            //return success;
        }

        private async Task<bool> GetAllFilesFromDirs(List<string> mediaDirs)
        {
            try
            {
                foreach (string mediaDir in mediaDirs)
                {
                    if (cancelRequest) { break; }
                    MediaDirectoryInfo dirInfo = _device.GetDirectoryInfo(mediaDir);
                    await GetAllFilesRecursive(dirInfo);
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        private async Task GetAllFilesRecursive(MediaDirectoryInfo directoryInfo)
        {
            var folders = new Stack<MediaDirectoryInfo>();
            folders.Push(directoryInfo);

            while (!cancelRequest && folders.Count > 0)
            {
                try
                {
                    var currentFolder = folders.Pop();
                    var count = await DeviceFileInfo.AddFiles(currentFolder.EnumerateFiles());

                    if (count > 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            OnPropertyChanged(nameof(DeviceFileInfo));
                            OnPropertyChanged(nameof(FilesToDownloadCount));
                        });
                    }

                    var dirs = await Task.FromResult(currentFolder.EnumerateDirectories().Where(d => !d.Name.StartsWith(".")));

                    foreach (var dir in dirs)
                    {
                        if (cancelRequest) { break; }
                        folders.Push(dir);
                    }
                }
                catch { cancelRequest = true; }
            }
        }

        public async void CopyFiles(BackgroundWorker worker, DoWorkEventArgs e, DownloadSettings settings)
        {
            List<string> filesDone = new List<string>();
            object _lockFilesDone = new object();

            IEnumerable<BaseFileInfo> filesToDownload = FilesToDownloadList;
            FilesToCopyCount = filesToDownload.Count();
            sizeToProcess = filesToDownload.Select(f => f.Size).Aggregate((a, b) => a + b) / (double)1048576;

            var st = new StackTrace();
            var sf = st.GetFrame(0);
            var currentMethodName = sf.GetMethod();


            ProgressUpdateArgs progressArgs = new ProgressUpdateArgs();
            FilesDoneCount = 0;
            byte[] origHash = null;
            double sizeCopied = 0;
            string tmpFolder = Application.Current.Resources[Properties.Keys.TempFolder].ToString();
            string tmpFile = "";
            BlockingCollection<CopyItem> filesCollection = new BlockingCollection<CopyItem>();

            if (!_log.Running) { _log.Start(); }

            Directory.CreateDirectory(tmpFolder);

            DateTime start = DateTime.Now;
            FilesDoneCount = 0;
            Errors = 0;

            MainWindow.ClearTemp();

            var addItems = Task.Run(() =>
            {
                _device.Connect();
                int i = 0;
                foreach (BaseFileInfo file in filesToDownload)
                {
                    try
                    {
                        progressArgs.taskName = Properties.Resources.DeviceCopyingFiles;
                        progressArgs.indeterminateTask = false;


                        lock (_lockReport)
                        {
                            progressArgs.progressText = string.Format(Properties.Resources.DeviceFilesDoneCount, FilesDoneCount, FilesToCopyCount);
                            progressArgs.currentTask = string.Format(Properties.Resources.DeviceDownloadingFile, file.FullPath);
                            worker.ReportProgress(FilesDoneCount * 100 / FilesToCopyCount, progressArgs);
                        }

                        //timer.Start();
                        if (worker.CancellationPending)
                        {
                            e.Cancel = true;
                            filesCollection.CompleteAdding();
                            Disconnect();
                            return;
                        }
                        else
                        {

                            // stažení souboru do složky tmp -> nutné stáhnout pouze 1x (stahovani ze zarizeni je pomalé)
                            // stažení je nutné - přes knihovnu MediaDevices není možné číst Exif data
                            // info o souboru z knihovny MediaDevices poskytuje CreationTime 
                            // - !neodpovídá času pořízení fotky (EXIF) při delším zpracování fotografie (dlouhá expozice nebo kontinuální snímání)
                            string GUID = Guid.NewGuid().ToString();
                            tmpFile = Path.Combine(tmpFolder, GUID);
                            if (settings.CheckFiles)
                            {
                                origHash = GetFileHash(_device.OpenReadFromPersistentUniqueId(file.PersistentUniqueId));
                            }

                            for (i = 0; i < maxAttempts; i++)
                            {
                                _device.DownloadFileFromPersistentUniqueId(file.PersistentUniqueId, tmpFile);
                                var thread = Thread.CurrentThread;
                                Console.WriteLine($"[{thread.ManagedThreadId}] downloaded file {Path.GetFileName(file.FullPath)}");

                                if (!settings.CheckFiles || CheckFileHash(origHash, tmpFile))
                                {
                                    break;
                                }
                            }

                            // soubor se nepodařilo stáhnout ze zařízení -> další soubor
                            if (i == maxAttempts)
                            {
                                string message = $"could not download file {file.FullPath}";
                                lock (_lockLog)
                                {
                                    _log.Add(message, LogType.ERROR, currentMethodName.Name);
                                }

                                if (File.Exists(tmpFile)) { File.Delete(tmpFile); }
                                continue;
                            }

                            if (!settings.CheckFiles) //hash pro ověření náhledu
                            {
                                origHash = GetFileHash(File.OpenRead(tmpFile));
                            }

                            MediaFileSystemInfo fileInfo = _device.GetFileSystemInfoFromPersistentUniqueId(file.PersistentUniqueId);
                            // dosazení hodnot za tagy
                            string folder = Tags.TagsToValues(settings.Paths.FolderTags, this, fileInfo, tmpFile);
                            string fileName = Tags.TagsToValues(settings.Paths.FileTags, this, fileInfo, tmpFile);
                            fileName += Path.GetExtension(file.FullPath);
                            string destFullName = Path.Combine(folder, fileName);

                            CopyItem item = new CopyItem(tmpFile, destFullName, origHash, file.FullPath, file.PersistentUniqueId, file.LastWriteTime); //přidat číslo (pořadí) souboru
                            filesCollection.Add(item);

                            i = 0;
                            int wait = 1000;
                            while (!_device.IsConnected && i < maxAttempts)
                            {
                                _device.Connect();
                                Thread.Sleep(wait);
                                wait *= 2;
                            }

                            if (!_device.IsConnected)
                            {
                                e.Result = new WorkerResult(MainWindow.RESULT_ERROR, TaskType.CopyFiles);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (_lockLog)
                        {
                            Errors++;
                            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
                            string message = $"File {file.FullPath}\n\t{ex.ToString().Replace(Environment.NewLine, Environment.NewLine + "\t")}";
                            _log.Add(ex.ToString(), LogType.ERROR, currentMethodName.Name);
                            _log.Stop();
                        }
                        if (File.Exists(tmpFile)) { File.Delete(tmpFile); }
                    }
                }
                filesCollection.CompleteAdding();
            });

            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling(Environment.ProcessorCount * 1.0)) };

            //zpracování stažených souborů
            Parallel.ForEach(filesCollection.GetConsumingEnumerable(), options, (item, state) =>
              {
                  var thread = Thread.CurrentThread;
                  if (worker.CancellationPending)
                  {
                      e.Cancel = true;
                      state.Stop();
                  }

                  if (settings.Thumbnail)
                  {
                      lock (_lockReport)
                      {
                          progressArgs.currentTask = string.Format(Properties.Resources.DeviceGeneratingThumbnail, item.origFile);
                          worker.ReportProgress(FilesDoneCount * 100 / FilesToCopyCount, progressArgs);
                      }
                      GenerateThumbnail(settings, item.fullDestPath, item.tmpFile, item.origFile, BitConverter.ToString(item.origHash).Replace("-", string.Empty).ToLowerInvariant());
                  }


                  lock (_lockReport)
                  {
                      progressArgs.currentTask = string.Format(Properties.Resources.DeviceSortingFile, item.origFile);
                      worker.ReportProgress(FilesDoneCount * 100 / FilesToCopyCount, progressArgs);
                  }
                  bool successSave = SaveFiles(settings, item.fullDestPath, item.tmpFile, item.origFile, item.origHash);

                  if (successSave)
                  {
                      if (Database.FileInfoExists(item.PersistentUID, item.origFile))
                      {
                          DBFileInfo fileInfo = new DBFileInfo()
                          {
                              PersistentUID = item.PersistentUID,
                              DateTaken = FileExif.GetDateTimeOriginal(item.tmpFile),
                              LastWriteTime = item.LastWriteTime,
                              DevicePath = item.origFile,
                              Hash = Convert.ToBase64String(item.origHash),
                              Downloaded = true

                          };
                          Database.InsertFileInfo(fileInfo);
                      }

                      if (settings.DeleteFiles)
                      {
                          lock (_lockFilesDone)
                          {
                              filesDone.Add(item.origFile);
                              Console.WriteLine($"[{thread.ManagedThreadId}] file {item.origFile} added to delete");
                          }
                      }
                  }


                  double size = 0;
                  if (File.Exists(item.tmpFile))
                  {
                      size = new FileInfo(item.tmpFile).Length / 1048576;
                      File.Delete(item.tmpFile);
                  }

                  DateTime end = DateTime.Now;
                  TimeSpan timeTotal = end - start;

                  lock (_lockReport)
                  {
                      sizeCopied += size;
                      double sizeRemain = sizeToProcess - sizeCopied;
                      FilesDoneCount++;
                      progressArgs.progressText = string.Format(Properties.Resources.DeviceFilesDoneCount, FilesDoneCount, FilesToCopyCount);
                      TimeSpan timeRemain = TimeSpan.FromTicks((long)(timeTotal.Ticks / sizeCopied) * (long)sizeRemain);
                      progressArgs.timeRemain = timeRemain;
                      worker.ReportProgress(FilesDoneCount * 100 / FilesToCopyCount, progressArgs);
                  }
              });

            await addItems;

            DeleteFiles(filesDone, worker);

            _log.Stop();
            Disconnect();
            e.Result = new WorkerResult(MainWindow.RESULT_OK, TaskType.CopyFiles);
        }

        private void DeleteFiles(List<string> files, BackgroundWorker worker)
        {
            DateTime start = DateTime.Now;
            int totalCount = files.Count();
            int doneCount = 0;
            int progressPercent = 0;
            ProgressUpdateArgs progressArgs = new ProgressUpdateArgs
            {
                taskName = Properties.Resources.DeviceDeletingFilesTask,
                progressText = string.Format(Properties.Resources.DeviceDeletingProgress, doneCount, totalCount)
            };
            worker.ReportProgress(progressPercent, progressArgs);
            foreach (string file in files)
            {
                progressArgs.currentTask = string.Format(Properties.Resources.DeviceDeletingFile, file);
                worker.ReportProgress(progressPercent, progressArgs);
                _device.DeleteFile(file);
                Console.WriteLine($"file {file} deleted");
                Console.WriteLine($"file {file} exists: {_device.FileExists(file)}");

                doneCount++;
                progressPercent = doneCount * 100 / totalCount;
                DateTime end = DateTime.Now;
                TimeSpan timeTotal = end - start;
                TimeSpan timeRemain = TimeSpan.FromTicks((timeTotal.Ticks / doneCount) * (totalCount - doneCount));

                progressArgs.progressText = string.Format(Properties.Resources.DeviceDeletingProgress, doneCount, totalCount);
                progressArgs.timeRemain = timeRemain;
                worker.ReportProgress(progressPercent, progressArgs);
            }

        }

        private void GenerateThumbnail(DownloadSettings settings, string filePath, string tmpFile, string origFile, string origHash, int attempt = 0, int wait = 2000)
        {

            var st = new StackTrace();
            var sf = st.GetFrame(0);
            var currentMethodName = sf.GetMethod();

            if (!FileType.IsImage(origFile))
            {
                string message = $"Could not generate thumbnail for [{origFile}]. Not supported image format.";
                lock (_lockLog)
                {
                    _log.Add(message, LogType.INFO, currentMethodName.Name);
                }
                return;
            }

            try
            {
                // filename in format: filename(ORIG_EXT).jpg
                string outFile = Path.Combine(settings.Paths.Thumbnail, filePath);
                outFile = Path.Combine(Path.GetDirectoryName(outFile), $"{Path.GetFileNameWithoutExtension(outFile)}({Path.GetExtension(outFile).Remove(0, 1)}).jpg");

                bool cont = false;
                if (File.Exists(outFile))
                {
                    using (MagickImage image = new MagickImage(outFile))
                    {
                        if (image.Comment == origHash)
                        {
                            switch (settings.ThumbnailSettings.Selected)
                            {
                                case ThumbnailSelect.longerSide:
                                    if (settings.ThumbnailSettings.Value == Math.Max(image.Width, image.Height)) { cont = true; }
                                    break;
                                case ThumbnailSelect.shorterSide:
                                    if (settings.ThumbnailSettings.Value == Math.Min(image.Width, image.Height)) { cont = true; }
                                    break;
                            }
                        }
                        else
                        {
                            string newFileName = GetUniqueFileName(outFile, tmpFile, true);
                            if (newFileName == outFile)
                            {
                                cont = true;
                            }
                            else
                            {
                                outFile = newFileName;
                            }
                        }
                    }
                }

                if (cont)
                {
                    string message = $"Thumbnail already exists for file [{origFile}] in [{outFile}]";
                    lock (_lockLog)
                    {
                        _log.Add(message, LogType.INFO, currentMethodName.Name);
                    }
                    return;
                }

                var size = new MagickGeometry(settings.ThumbnailSettings.Value);
                switch (settings.ThumbnailSettings.Selected)
                {
                    case ThumbnailSelect.longerSide:
                        size.FillArea = false;
                        break;
                    case ThumbnailSelect.shorterSide:
                        size.FillArea = true;
                        break;
                }

                var readSettings = new MagickReadSettings
                {
                    Defines = new DngReadDefines
                    {
                        ReadThumbnail = true,
                        UseCameraWhitebalance = true,
                    }
                };

                bool thumbnailCreated = false;
                using (var image = new MagickImage())
                {
                    image.Ping(tmpFile, readSettings); //read only metadata
                    var profile = image.GetProfile("dng:thumbnail");

                    if (profile != null)
                    {
                        using (var jpgThumbnail = new MagickImage(profile.GetData())) //use embedded thumbnail
                        {
                            if (IsLargerResolution(settings.ThumbnailSettings, jpgThumbnail.Width, jpgThumbnail.Height))
                            {
                                jpgThumbnail.AutoOrient(); // Correct the image orientation
                                jpgThumbnail.Thumbnail(size);
                                Directory.CreateDirectory(Path.GetDirectoryName(outFile));
                                jpgThumbnail.Write(outFile);
                                thumbnailCreated = true;
                            }
                        }
                    }
                }

                if (!thumbnailCreated)
                {
                    using (MagickImage image = new MagickImage(tmpFile, readSettings))
                    {
                        image.Thumbnail(size);
                        image.TransformColorSpace(ColorProfile.AdobeRGB1998);
                        image.AutoLevel();
                        image.Comment = origHash;
                        Directory.CreateDirectory(Path.GetDirectoryName(outFile));
                        image.Write(outFile, MagickFormat.Jpg);
                    }
                }


                var thread = Thread.CurrentThread;
                Console.WriteLine($"[{thread.ManagedThreadId}] generated thumbnail for file {Path.GetFileName(origFile)}");

            }
            catch (MagickException ex)
            {
                attempt++;
                if (attempt < maxAttempts)
                {
                    Thread.Sleep(wait);
                    wait *= 2;
                    var thread = Thread.CurrentThread;
                    Console.WriteLine($"[{thread.ManagedThreadId}] thumbnail attempt [{attempt}] for file {Path.GetFileName(origFile)}");
                    GenerateThumbnail(settings, filePath, tmpFile, origFile, origHash, attempt, wait);
                }
                else
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
                    string message = $"could not create thumbnail for {origFile}\n\t{ex.ToString().Replace(Environment.NewLine, Environment.NewLine + "\t")}";
                    lock (_lockLog)
                    {
                        Errors++;
                        _log.Add(message, LogType.ERROR, currentMethodName.Name);
                    }
                }
            }
        }

        private bool IsLargerResolution(Thumbnails thumbnailSettings, int width, int height)
        {

            if (thumbnailSettings.Selected == ThumbnailSelect.longerSide)
            {
                return Math.Max(width, height) > thumbnailSettings.Value;
            }
            else
            {
                return Math.Min(width, height) > thumbnailSettings.Value;
            }
        }

        private bool SaveFiles(DownloadSettings settings, string fullName, string tmpFile, string origFile, byte[] origHash)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);
            var currentMethodName = sf.GetMethod();
            // kopirovat do vsech umisteni (vcetne zalohy)
            foreach (string root in new List<string> { settings.Paths.Root, settings.Paths.Backup })
            {
                if (root == null || root == "")
                {
                    return true;
                }

                string fullPath = Path.Combine(root, fullName);

                //pokud existuje soubor se stejným názvem, ale jiným datem pořízení -> odlišení nového souboru - pridani (n) k nazvu 
                bool cont = false;

                if (File.Exists(fullPath))
                {
                    string newFileName = GetUniqueFileName(fullPath, tmpFile);
                    if (newFileName == fullPath)
                    {
                        cont = true;
                    }
                    else
                    {
                        fullPath = newFileName;
                    }
                }

                if (cont)
                {
                    string message = $"File was already downloaded [{origFile}] to [{fullPath}]";
                    lock (_lockLog)
                    {
                        _log.Add(message, LogType.INFO, currentMethodName.Name);
                    }
                    return true;
                }

                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                int i;
                for (i = 0; i < maxAttempts; i++)
                {
                    File.Copy(tmpFile, fullPath);
                    var thread = Thread.CurrentThread;
                    Console.WriteLine($"[{thread.ManagedThreadId}] copied file {Path.GetFileName(origFile)} attempt {i}");

                    if (!settings.CheckFiles || CheckFileHash(origHash, fullPath))
                    {
                        break;
                    }
                }

                // soubor se nepodařilo zkopírovat -> další soubor
                if (i == maxAttempts)
                {
                    File.Delete(fullPath);
                    string message = $"could not copy file {origFile}";
                    lock (_lockLog)
                    {
                        Errors++;
                        _log.Add(message, LogType.ERROR, currentMethodName.Name);
                    }
                    return false;
                }
            }
            return true;
        }

        private byte[] GetFileHash(Stream s)
        {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(s);
            s.Close();
            return hash;
        }

        private bool CheckFileHash(byte[] origHash, string filePath)
        {
            return origHash.SequenceEqual(GetFileHash(File.OpenRead(filePath)));
        }

        private string GetUniqueFileName(string newFile, string tempFile, bool isThumbnail = false)
        {
            int i = 1;
            try
            {
                while (File.Exists(newFile))
                {
                    if (isThumbnail || !CheckFileHash(GetFileHash(File.OpenRead(tempFile)), newFile))
                    {
                        string ext = Path.GetExtension(newFile);
                        newFile = newFile.Replace(ext, $"({i}){ext}");
                    }
                    else { break; }
                    i++;
                }
            }
            catch { }

            return newFile;
        }
    }
}
