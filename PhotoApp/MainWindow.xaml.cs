﻿using MaterialDesignThemes.Wpf;
using PhotoApp.Dialogs;
using PhotoApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Usb.Events;

namespace PhotoApp
{
    public enum TaskType
    {
        FindFiles,
        CopyFiles
    }

    public struct WorkerResult
    {
        public WorkerResult(int code, TaskType task)
        {
            this.code = code;
            this.task = task;
        }
        public int code;
        public TaskType task;
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly string logFolder = Application.Current.Resources[Properties.Keys.LogsFolder].ToString();
        private static readonly string tmpFolder = Application.Current.Resources[Properties.Keys.TempFolder].ToString();

        public static DownloadSettings DownloadSettings { get; set; }
        public List<string> Profiles { get; set; }

        public DeviceList DeviceList { get; set; }
        static readonly IUsbEventWatcher usbEventWatcher = new UsbEventWatcher();
        public ProgressDialog progressDialog { get; private set; } = null;
        private BackgroundWorker backgroundWorker = null;
        private DateTime backupStart = new DateTime();

        //dialog result
        public const int DIALOG_SHOW_LIBRARIES = 11;
        public const int DIALOG_SHOW_CHANGELOG = 12;

        //dialog error
        public const int RESULT_ERROR = -1;
        public const int RESULT_CANCEL = 0;
        public const int RESULT_OK = 1;

        private Border normalBorder = new Border();
        Border errorBorder = new Border();
        public Style mainTextStyle { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            DataContext = this;
            DownloadSettings = new DownloadSettings();
            Profiles = new List<string>();
            DeviceList = new DeviceList();

            InitializeComponent();

            if (Properties.Settings.Default.CheckUpdateOnStartup)
            {
                CheckNewVersion();
            }

            progressDialog = new ProgressDialog(this);

            spDeviceInfo.Visibility = Visibility.Hidden;

            ListConnectedDevices();

            //udalost pripojeni/odpojeni zarizeni -> aktualizace seznamu
            usbEventWatcher.UsbDeviceAdded += (_, device) => Dispatcher.Invoke(new Action(() => DeviceAdded(device)));
            usbEventWatcher.UsbDeviceRemoved += (_, device) => Dispatcher.Invoke(new Action(() => DeviceRemoved(device)));

            Properties.Settings.Default.PropertyChanged += Settings_Changed;

            normalBorder.BorderBrush = Brushes.Transparent;
            normalBorder.BorderThickness = new Thickness(0);

            errorBorder.BorderBrush = Brushes.Red;
            errorBorder.BorderThickness = new Thickness(3);

            mainTextStyle = (Style)FindResource("MaterialDesignBody2TextBlock");

            GetProfiles();
        }

        private void Settings_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Properties.Settings.Default.TagLanguage))
            {
                icFolderTags.Items.Refresh();
                icFileTags.Items.Refresh();
            }
        }

        private async void dhDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Version currentVersion = Version.Parse(((App)Application.Current).Version);
            Version lastRunVerison = Version.Parse(Properties.Settings.Default.LastVersion);
            if (currentVersion > lastRunVerison)
            {
                await ShowChangelogDialog();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void ListBoxDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBoxDevices.SelectedItem != null)
            {
                tbSelectDeviceError.Visibility = Visibility.Collapsed;
                spDeviceInfo.Visibility = Visibility.Visible;
                ListBoxDevices.BorderBrush = Brushes.Black;
                ListBoxDevices.BorderThickness = new Thickness(1);

                //zmena vybraneho zarizeni
                DeviceList.SelectDeviceByID(((Device)ListBoxDevices.SelectedItem).ID);
                DownloadSettings.FileTypeSelection = new FileTypeSelection();
            }
            else
            {
                spDeviceInfo.Visibility = Visibility.Collapsed;
                ListBoxDevices.IsEnabled = true;
            }
        }

        private void DeviceAdded(UsbDevice device)
        {
            Debug.WriteLine(device.SerialNumber + " added");
            ListConnectedDevices();
        }
        private void DeviceRemoved(UsbDevice device)
        {
            Debug.WriteLine(device.SerialNumber + " removed");
            ListConnectedDevices();
        }

        //nalezeni a vypis vsech zarizeni vyuzivajicich MTP
        private void ListConnectedDevices()
        {
            ListBoxDevices.SelectedIndex = DeviceList.UpdateDevices(DeviceList.SelectedDevice?.ID);
            if (IsLoaded)
            {
                FindAllFiles();
            }
        }

        private void GetProfiles(string SelectProfile = "")
        {
            var profilesPath = Application.Current.Resources[Properties.Keys.ProfilesFolder].ToString();
            string selectedProfile = SelectProfile;
            if (selectedProfile.Length == 0 && cbProfiles.SelectedItem != null)
            {
                selectedProfile = cbProfiles.SelectedItem.ToString();
            }

            Profiles = Directory.GetFiles(profilesPath, "*.xml").Select(f => Path.GetFileNameWithoutExtension(f)).Where(x => DownloadSettings.IsValid(x)).ToList();

            OnPropertyChanged("Profiles");

            cbProfiles.SelectedIndex = Profiles.IndexOf(selectedProfile);

            if (Profiles.Count > 0 && selectedProfile.Length > 0)
            {
                btnDeleteProfile.IsEnabled = true;
            }
            else
            {
                btnDeleteProfile.IsEnabled = false;
            }
        }

        private void CopyFiles(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            DeviceList.SelectedDevice.CopyFiles(worker, e, DownloadSettings);
        }


        //obnovit seznam pripojenych zarizeni
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            ListConnectedDevices();
        }

        public async void FindAllFiles(string deviceID = "")
        {
            var devices = DeviceList.Devices;
            Device device;

            foreach (var dev in DeviceList.Devices.Where(d => d.FileSearchStatus == Device.DEVICE_FILES_NOT_SEARCHED ||
                                                              d.ID == deviceID))
            {
                dev.FileSearchStatus = Device.DEVICE_FILES_WAITING;
            }

            if (!devices.Any(d => d.FileSearchStatus == Device.DEVICE_FILES_SEARCHING))
            {
                if (!string.IsNullOrEmpty(deviceID))
                {
                    device = devices.First(d => d.ID == deviceID);
                }
                else
                {
                    device = devices.FirstOrDefault(d => d.FileSearchStatus == Device.DEVICE_FILES_NOT_SEARCHED ||
                                                         d.FileSearchStatus == Device.DEVICE_FILES_WAITING);
                }
                if (device != null)
                {
                    await device.GetAllFiles();
                    FindAllFiles();
                }
            }
        }

        private void CopyFiles_Click(object sender, RoutedEventArgs e)
        {
            if (CheckSettings())
            {
                backupStart = DateTime.Now;
                RunWorker(sender, TaskType.CopyFiles, true);
            }
        }

        private void RemoveAllErrors()
        {
            ListBoxDevices.BorderBrush = Brushes.Black;
            ListBoxDevices.BorderThickness = new Thickness(1);
            IEnumerable<TextBlock> errorBlocks = FindVisualChildren<TextBlock>(this).Where(x => x.Name.StartsWith("tb") && x.Name.EndsWith("Error"));
            foreach (TextBlock tb in errorBlocks)
            {
                tb.Visibility = Visibility.Collapsed;
            }
            IEnumerable<Button> errorButtons = FindVisualChildren<Button>(this).Where(x => x.BorderBrush == errorBorder.BorderBrush);
            foreach (Button btn in errorButtons)
            {
                btn.BorderBrush = normalBorder.BorderBrush;
                btn.BorderThickness = normalBorder.BorderThickness;
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private bool CheckSettings()
        {
            bool correct = true;

            correct = CheckDeviceSelected();

            if (DeviceList.SelectedDevice.Name == null || DeviceList.SelectedDevice.Name.Length == 0)
            {
                SetErrorMessage(tbDeviceNameError, Properties.Resources.DeviceNameEmpty);
                correct = false;
            }

            if (DownloadSettings.Paths.Root == null || DownloadSettings.Paths.Root.Length == 0)
            {
                BtnError(btnMainFolder);
                SetErrorMessage(tbRootError, Properties.Resources.NoDownloadFolder);
                correct = false;
            }
            else if (!Directory.Exists(DownloadSettings.Paths.Root))
            {
                BtnError(btnMainFolder);
                SetErrorMessage(tbRootError, Properties.Resources.FolderDoesNotExist);
                correct = false;
            }

            if (DownloadSettings.Backup)
            {
                if (DownloadSettings.Paths.Backup == null || DownloadSettings.Paths.Backup.Length == 0)
                {
                    BtnError(btnChooseBackupDest);
                    SetErrorMessage(tbBackupError, Properties.Resources.NoBackupFolder);
                    correct = false;
                }
                else if (!Directory.Exists(DownloadSettings.Paths.Backup))
                {
                    BtnError(btnChooseBackupDest);
                    SetErrorMessage(tbBackupError, Properties.Resources.FolderDoesNotExist);
                    correct = false;
                }
            }


            if (DownloadSettings.Paths.FolderTags == null || DownloadSettings.Paths.FolderTags.Count == 0)
            {
                BtnError(btnFolderStruct);
                SetErrorMessage(tbFolderStructError, Properties.Resources.NoFolderStructure);
                correct = false;
            }

            if (DownloadSettings.Paths.FileTags == null || DownloadSettings.Paths.FileTags.Count == 0)
            {
                BtnError(btnFileStruct);
                SetErrorMessage(tbFileStructError, Properties.Resources.NoFileStructure);
                correct = false;
            }

            if (DownloadSettings.Thumbnail)
            {
                if (DownloadSettings.Paths.Thumbnail == null || DownloadSettings.Paths.Thumbnail.Length == 0)
                {
                    BtnError(btnChooseThumbDest);
                    SetErrorMessage(tbThumbnailDestError, Properties.Resources.NoThumbnailFolder);
                    correct = false;
                }
                else if (!Directory.Exists(DownloadSettings.Paths.Thumbnail))
                {
                    BtnError(btnChooseThumbDest);
                    SetErrorMessage(tbThumbnailDestError, Properties.Resources.FolderDoesNotExist);
                    correct = false;
                }

                if (DownloadSettings.ThumbnailSettings.Value == 0)
                {
                    SetErrorMessage(tbThumbnailError, Properties.Resources.CannotBeZero);
                    correct = false;
                }
            }

            if ((bool)cbDateRange.IsChecked && DownloadSettings.Date.Start > DownloadSettings.Date.End)
            {
                SetErrorMessage(tbDateRangeError, Properties.Resources.DateRange_StartGreater);
                correct = false;
            }


            return correct;
        }

        private bool CheckDeviceSelected()
        {
            bool correct = true;
            if (DeviceList.SelectedDevice == null)
            {
                ListBoxDevices.BorderBrush = errorBorder.BorderBrush;
                ListBoxDevices.BorderThickness = errorBorder.BorderThickness;
                SetErrorMessage(tbSelectDeviceError, Properties.Resources.SelectDevice);
                correct = false;
            }
            return correct;
        }

        private void SetErrorMessage(TextBlock tb, string message)
        {
            tb.Text = message;
            tb.Visibility = Visibility.Visible;
        }
        //spusteni prace na pozadi podle typu ulohy
        private void RunWorker(object sender, TaskType task, bool showProgress, string deviceID = "")
        {
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.WorkerReportsProgress = showProgress;
            backgroundWorker.ProgressChanged += worker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += worker_RunWorkerCompleted;

            if (showProgress)
            {
                DialogHost.Show(progressDialog, "RootDialog");
                progressDialog.btnCancel.Content = Properties.Resources.Cancel;
            }

            switch (task)
            {
                case TaskType.CopyFiles:
                    backgroundWorker.DoWork += CopyFiles;
                    break;
            }
            if (!backgroundWorker.IsBusy)
            {
                backgroundWorker.RunWorkerAsync();
            }
        }


        //backgroundWorker update progress
        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var progress = (ProgressUpdateArgs)e.UserState;

            progressDialog.SetCurrentTask(progress.taskName);

            if (progress.indeterminateTask)
            {
                progressDialog.SetIndeterminateProgress();
                progressDialog.SetProgressMessage(progress.progressText);
            }
            else
            {
                progressDialog.SetCurrentProgress(progress.progressText, e.ProgressPercentage, progress.timeRemain);
            }
            progressDialog.SetCurrentDir(progress.currentTask);

        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dhDialog.IsOpen = false;
            lblResult.Visibility = Visibility.Visible;
            if (e.Cancelled)
            {
                lblResult.Text = Properties.Resources.ResultCanceled;
                return;
            }

            WorkerResult result = (WorkerResult)e.Result;
            if (result.code == RESULT_ERROR)
            {
                ShowErrorDialog(Properties.Resources.FileCheckError);
                ListConnectedDevices();
            }
            else if (result.code == RESULT_OK)
            {
                int total = DeviceList.SelectedDevice.FilesTotal;
                int downloaded = DeviceList.SelectedDevice.FilesDoneCount;
                int toDownload = DeviceList.SelectedDevice.FilesToCopyCount;
                int errors = DeviceList.SelectedDevice.Errors;
                lblResult.Text = $"{Properties.Resources.FilesFoundTotal}: {total}\n";
                if (result.task == TaskType.CopyFiles)
                {
                    lblResult.Text += $"{Properties.Resources.FilesDownloadedTotal}: {downloaded}/{toDownload}\n" +
                                      $"{Properties.Resources.FilesDownloadErrorTotal}: {errors}";
                    if (DownloadSettings.DownloadSelect == DownloadSelect.newFiles && downloaded == toDownload)
                    {
                        DeviceList.SelectedDevice.LastBackup = backupStart;
                    }
                    OnPropertyChanged("SelectedDeviceInfo");
                    DeviceList.Save();
                }
                else if (result.task == TaskType.FindFiles)
                {
                    lblResult.Text += $"{Properties.Resources.FilesToDownload}: {toDownload}";
                }

                if (errors > 0)
                {
                    btnShowLog.Visibility = Visibility.Visible;
                }
                else
                {
                    btnShowLog.Visibility = Visibility.Collapsed;
                }
            }
        }

        public void worker_Cancel()
        {
            backgroundWorker.CancelAsync();
        }


        //dialog struktura slozky
        private async void btnFolderStruct_Click(object sender, RoutedEventArgs e)
        {
            BtnNormal(btnFolderStruct);
            tbFolderStructError.Visibility = Visibility.Collapsed;
            List<ButtonGroupStruct> buttonGroups = new List<ButtonGroupStruct>()
            {
                new ButtonGroupStruct(
                    Properties.Resources.TagGroup_Date,
                    new List<string>(){
                        Properties.TagCodes.YearLong,
                        Properties.TagCodes.Month,
                        Properties.TagCodes.Day},
                    new Point(0,0)),

                new ButtonGroupStruct(
                    Properties.Resources.TagGroup_Custom,
                    new List<string>(){
                        Properties.TagCodes.CustomText},
                    new Point(1,0)),

                new ButtonGroupStruct(
                    Properties.Resources.TagGroup_Device,
                    new List<string>(){
                        Properties.TagCodes.DeviceName,
                        Properties.TagCodes.DeviceManuf},
                    new Point(0,1)),

                new ButtonGroupStruct(
                    Properties.Resources.TagGroup_Folder,
                    new List<string>(){
                        Properties.TagCodes.NewFolder},
                    new Point(0,2)),

                new ButtonGroupStruct(
                    Properties.Resources.TagGroup_Separator,
                    new List<string>(){
                        Properties.TagCodes.Hyphen,
                        Properties.TagCodes.Underscore},
                    new Point(1,1)),

                new ButtonGroupStruct(
                    Properties.Resources.TagGroup_File,
                    new List<string>(){
                        Properties.TagCodes.FileType},
                    new Point(1,2))
            };

            FolderStructDialog folderDialog = new FolderStructDialog(buttonGroups, DownloadSettings.Paths.FolderTags);
            DialogResultWithData result = (DialogResultWithData)await DialogHost.Show(folderDialog, "RootDialog", delegate (object o, DialogOpenedEventArgs args)
            {
                folderDialog.Session = args.Session;
            });
            if (result.ResultCode == DialogResultWithData.RESULT_OK)
            {
                DownloadSettings.Paths.FolderTags = (List<List<string>>)result.Data;
            }
        }

        //vytvoreni a zobrazeni dialogu pro nazev souboru
        private async void btnFileStruct_Click(object sender, RoutedEventArgs e)
        {
            BtnNormal(btnFileStruct);
            tbFileStructError.Visibility = Visibility.Collapsed;
            List<ButtonGroupStruct> buttonGroups = new List<ButtonGroupStruct>()
            {
                new ButtonGroupStruct(
                    Properties.Resources.TagGroup_Date,
                    new List<string>(){
                        Properties.TagCodes.YearLong,
                        Properties.TagCodes.Month,
                        Properties.TagCodes.Day},
                    new Point(0,0)),

                new ButtonGroupStruct(
                    Properties.Resources.TagGroup_Other,
                    new List<string>(){
                        Properties.TagCodes.CustomText,
                        Properties.TagCodes.FileName},
                    new Point(1,0)),

                new ButtonGroupStruct(
                    Properties.Resources.TagGroup_Device,
                    new List<string>(){
                        Properties.TagCodes.DeviceName,
                        Properties.TagCodes.DeviceManuf},
                    new Point(0,1)),

                // new ButtonGroupStruct(
                //     "Číslování",
                //     new List<string>(){ 
                //         Properties.TagCodes.SequenceNum},
                //     new Point(0,2)),

                new ButtonGroupStruct(
                    Properties.Resources.TagGroup_Separator,
                    new List<string>(){
                        Properties.TagCodes.Hyphen,
                        Properties.TagCodes.Underscore},
                    new Point(1,1))
            };

            FileStructDialog fileDialog = new FileStructDialog(buttonGroups, DownloadSettings.Paths.FileTags);
            DialogResultWithData result = (DialogResultWithData)await DialogHost.Show(fileDialog, "RootDialog", delegate (object o, DialogOpenedEventArgs args)
            {
                fileDialog.Session = args.Session;
            });
            if (result.ResultCode == DialogResultWithData.RESULT_OK)
            {
                DownloadSettings.Paths.FileTags = (List<string>)result.Data;
            }
        }

        // vyber slozek
        private void btnMainFolder_Click(object sender, RoutedEventArgs e)
        {
            BtnNormal(btnMainFolder);
            tbRootError.Visibility = Visibility.Collapsed;
            string result = ChooseDirectory(DownloadSettings.Paths.Root);
            if (result != string.Empty)
            {
                DownloadSettings.Paths.Root = result;
            }
        }

        private void btnChooseBackupDest_Click(object sender, RoutedEventArgs e)
        {
            BtnNormal(btnChooseBackupDest);
            tbBackupError.Visibility = Visibility.Collapsed;
            string result = ChooseDirectory(DownloadSettings.Paths.Backup);
            if (result != string.Empty)
            {
                DownloadSettings.Paths.Backup = result;
            }
        }

        private void btnChooseThumbDest_Click(object sender, RoutedEventArgs e)
        {
            BtnNormal(btnChooseThumbDest);
            tbThumbnailDestError.Visibility = Visibility.Collapsed;
            string result = ChooseDirectory(DownloadSettings.Paths.Thumbnail);
            if (result != string.Empty)
            {
                DownloadSettings.Paths.Thumbnail = result;
            }
        }
        private string ChooseDirectory(string initDir)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = DownloadSettings.Paths.Thumbnail;
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result.ToString() != string.Empty)
                return dialog.SelectedPath;
            else return string.Empty;
        }


        // zmena stavu tlacitek (error, normal)
        private void BtnError(Button button)
        {
            button.BorderBrush = errorBorder.BorderBrush;
            button.BorderThickness = errorBorder.BorderThickness;
        }

        private void BtnNormal(Button button)
        {
            button.BorderBrush = normalBorder.BorderBrush;
            button.BorderThickness = normalBorder.BorderThickness;
        }

        private void dpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DatePicker dp = sender as DatePicker;
            //if (dp.SelectedDate == null)
            //{
            //    dp.SelectedDate = DateTime.Now;
            //}

            if (DownloadSettings.Date.Start > DownloadSettings.Date.End)
            {
                SetErrorMessage(tbDateRangeError, Properties.Resources.DateRange_StartGreater);
            }
            else if (tbDateRangeError != null)
            {
                tbDateRangeError.Visibility = Visibility.Collapsed;
            }

            //SetDatePickerRange(dp); //disable date range
        }

        private void SetDatePickerRange(DatePicker dp)
        {
            var dateRange = DownloadSettings.Date;
            if (dp == dpDateFrom && dpDateTo != null)
            {
                dpDateTo.BlackoutDates.Clear();
                var end = new DateTime(dateRange.Start.Ticks).AddDays(-1);
                var range = new CalendarDateRange(DateTime.MinValue, end);
                dpDateTo.BlackoutDates.Add(range);
            }
            else if (dp == dpDateTo && dpDateFrom != null)
            {
                dpDateFrom.BlackoutDates.Clear();
                var start = new DateTime(dateRange.End.Ticks).AddDays(1);
                var range = new CalendarDateRange(start, DateTime.MaxValue);
                dpDateFrom.BlackoutDates.Add(range);
            }
        }

        private void tbValue_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]+");
            e.Handled = !regex.IsMatch(e.Text);
        }



        // pokud by pole melo byt prazdne -> chyba data bindingu (potreba hondota int)
        private void tbValue_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text.Length == 1 && (e.Key == System.Windows.Input.Key.Back || e.Key == System.Windows.Input.Key.Delete))
            {
                textBox.Text = "0";
                textBox.SelectionStart = 1;
                textBox.SelectionLength = 0;
                e.Handled = true;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cbProfiles.SelectedIndex == -1)
            {
                btnSaveAs_Click(sender, e);

            }
            else
            {
                DownloadSettings.Save();
            }

        }
        private async void btnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveDialog saveDialog = new SaveDialog(DownloadSettings.SaveOptions);
            DialogResultWithData result = (DialogResultWithData)await DialogHost.Show(saveDialog, "RootDialog", delegate (object o, DialogOpenedEventArgs args)
            {
                saveDialog.Session = args.Session;
            });
            if (result.ResultCode == DialogResultWithData.RESULT_OK)
            {
                SaveOptions options = (SaveOptions)result.Data;
                DownloadSettings.Save(options);
                GetProfiles(options.FileName); // načte nový profil
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            GetProfiles();
        }

        private void btnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedProfile(string.Format(Properties.Resources.DeleteProfilePrompt, cbProfiles.SelectedItem));
        }

        private void cbProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb.SelectedItem != null)
            {

                if (!DownloadSettings.Load(cb.SelectedItem.ToString()))
                {
                    DeleteSelectedProfile(string.Format(Properties.Resources.Profile_Load_Error, cb.SelectedItem));
                }
                OnPropertyChanged("DownloadSettings");

                RemoveAllErrors();
                CheckSettings();

                btnDeleteProfile.IsEnabled = true;
            }
            else
            {
                btnDeleteProfile.IsEnabled = false;
            }
        }

        private async void DeleteSelectedProfile(string message)
        {
            var result = await ShowYesNoDialog(message);
            if (result)
            {
                DownloadSettings.Delete(cbProfiles.SelectedItem.ToString());
                GetProfiles();
                OnPropertyChanged(nameof(Profiles));
            }
        }

        public static StackPanel CreateIconPanel(string text, PackIconKind iconKind, int level, Style textStyle)
        {
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            sp.Margin = new Thickness(level * 10, 0, 0, 0);

            PackIcon icon = new PackIcon();
            icon.Kind = iconKind;

            sp.Children.Add(icon);

            TextBlock textBlock = new TextBlock();
            textBlock.Style = textStyle;
            textBlock.FontWeight = FontWeights.SemiBold;
            textBlock.Margin = new Thickness(5, 0, 0, 0);
            textBlock.Text = text;
            sp.Children.Add(textBlock);

            return sp;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            DeviceList.Save();
            ClearTemp();
        }

        private void tbValue_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DownloadSettings.ThumbnailSettings.Value == 0)
            {
                SetErrorMessage(tbThumbnailError, Properties.Resources.CannotBeZero);
            }
        }

        private void tbValue_GotFocus(object sender, RoutedEventArgs e)
        {
            tbThumbnailError.Visibility = Visibility.Collapsed;
        }

        private void btnShowLog_Click(object sender, RoutedEventArgs e)
        {
            var directory = new DirectoryInfo(logFolder);
            var lastLog = directory.GetFiles().OrderByDescending(f => f.LastWriteTime).First();
            Process.Start("explorer.exe", lastLog.FullName);
        }

        public static void ClearTemp()
        {
            Parallel.ForEach(Directory.EnumerateFiles(tmpFolder).Where(x => !x.EndsWith(".msi")), (file) =>
              {
                  File.Delete(file);
              });
        }

        private void lblDeviceName_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (/*DeviceList.SelectedDeviceIndex != -1 && */tb.Text.Length == 0)
            {
                SetErrorMessage(tbDeviceNameError, Properties.Resources.DeviceNameEmpty);
                ListBoxDevices.IsEnabled = false;
            }
            else
            {
                tbDeviceNameError.Visibility = Visibility.Collapsed;
                ListBoxDevices.IsEnabled = true;
            }
        }

        private void btnInfoClick(object sender, RoutedEventArgs e)
        {
            ShowInfoDialog();
        }

        private async void ShowInfoDialog()
        {
            var AppInfoDialog = new AppInfoDialog();
            var result = await DialogHost.Show(AppInfoDialog, "RootDialog", delegate (object o, DialogOpenedEventArgs args)
            {
                AppInfoDialog.Session = args.Session;
            });
            switch (result)
            {
                case DIALOG_SHOW_LIBRARIES:
                    await ShowLibrariesDialog();
                    ShowInfoDialog();
                    break;
                case DIALOG_SHOW_CHANGELOG:
                    await ShowChangelogDialog();
                    ShowInfoDialog();
                    break;
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsDialog();
        }

        private async void ShowSettingsDialog()
        {
            var AppSettingsDialog = new AppSettingsDialog();
            await DialogHost.Show(AppSettingsDialog, "RootDialog", delegate (object o, DialogOpenedEventArgs args)
            {
                AppSettingsDialog.Session = args.Session;
            });
        }

        private void btnFeedback_Click(object sender, RoutedEventArgs e)
        {
            ShowFeedbackDialog();
        }

        private async void ShowFeedbackDialog()
        {
            var AppFeedbackDialog = new AppFeedbackDialog(this);
            await DialogHost.Show(AppFeedbackDialog, "RootDialog", delegate (object o, DialogOpenedEventArgs args)
            {
                AppFeedbackDialog.Session = args.Session;
            });
        }

        private async void ShowUpdateDialog(Octokit.Release release)
        {
            var UpdateDialog = new UpdateDialog(release);
            await DialogHost.Show(UpdateDialog, "RootDialog", delegate (object o, DialogOpenedEventArgs args)
            {
                UpdateDialog.Session = args.Session;
            });
        }

        private async Task<bool> ShowYesNoDialog(string message)
        {
            YesNoDialog ynDialog = new YesNoDialog(message);
            var result = await DialogHost.Show(ynDialog, "RootDialog", delegate (object o, DialogOpenedEventArgs args)
            {
                ynDialog.Session = args.Session;
            });
            return (bool)result;
        }

        public async Task ShowLibrariesDialog()
        {
            var LibrariesDialog = new UsedLibraries();
            await DialogHost.Show(LibrariesDialog, "RootDialog", delegate (object o, DialogOpenedEventArgs args)
            {
                LibrariesDialog.Session = args.Session;
            });
        }

        public async Task ShowChangelogDialog()
        {
            var ChangelogDialog = new ChangelogDialog(ActualHeight, ActualWidth);
            await DialogHost.Show(ChangelogDialog, "RootDialog", delegate (object o, DialogOpenedEventArgs args)
            {
                ChangelogDialog.Session = args.Session;
            });
        }
        public void ShowErrorDialog(string message)
        {
            ErrorDialog errorDialog = new ErrorDialog(message);
            DialogHost.Show(errorDialog, "RootDialog", delegate (object o, DialogOpenedEventArgs args)
            {
                errorDialog.Session = args.Session;
            });
        }

        private async void CheckNewVersion()
        {
            var currentVersion = Version.Parse(((App)Application.Current).Version);
            try
            {
                var github = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("WirePix"));
                var release = await github.Repository.Release.GetLatest("KurekMartin", "WirePix");
                var latestVersion = Version.Parse(release.TagName.Replace("v", ""));

                if (latestVersion > currentVersion)
                {
                    SnackBar.MessageQueue.Enqueue(Properties.Resources.Update_NewVersionAvailable, Properties.Resources.Show.ToUpper(), () => ShowUpdateDialog(release));
                }
            }
            catch (Exception ex) { }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FindAllFiles();
        }

        private void btnFilesAction_Click(object sender, RoutedEventArgs e)
        {
            var status = DeviceList.SelectedDevice.FileSearchStatus;
            if (status == Device.DEVICE_FILES_NOT_SEARCHED ||
                status == Device.DEVICE_FILES_CANCELED ||
                status == Device.DEVICE_FILES_ERROR ||
                status == Device.DEVICE_FILES_READY)
            {
                DeviceList.SelectedDevice.FileSearchStatus = Device.DEVICE_FILES_WAITING;
                FindAllFiles(DeviceList.SelectedDevice.ID);
            }
            else if (status == Device.DEVICE_FILES_WAITING ||
                     status == Device.DEVICE_FILES_SEARCHING)
            {
                DeviceList.SelectedDevice.CancelCurrentTask();
            }
        }
    }
}


