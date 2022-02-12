using MaterialDesignThemes.Wpf;
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
        CopyFiles,
        GetFileTypes
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
        private readonly string logFolder = Application.Current.Resources["logFolder"].ToString();
        private static readonly string tmpFolder = Application.Current.Resources["tmpFolder"].ToString();

        public Settings Settings { get; set; }
        public List<string> Profiles { get; set; }

        public DeviceList DeviceList { get; set; }
        static readonly IUsbEventWatcher usbEventWatcher = new UsbEventWatcher();
        public ProgressDialog progressDialog { get; private set; } = null;
        private BackgroundWorker backgroundWorker = null;
        private DateTime backupStart = new DateTime();


        //dialog error
        public static int RESULT_ERROR = -1;
        public static int RESULT_OK = 0;

        private static int REQUEST_PROFILE_DELETE = 10;

        private Border normalBorder = new Border();
        Border errorBorder = new Border();

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            Settings = new Settings();
            Profiles = new List<string>();
            DeviceList = new DeviceList();

            DeviceList.Load();
            InitializeComponent();
            DataContext = this;
            progressDialog = new ProgressDialog(this);

            spDeviceInfo.Visibility = Visibility.Hidden;

            ListConnectedDevices();

            //udalost pripojeni/odpojeni zarizeni -> aktualizace seznamu
            usbEventWatcher.UsbDeviceAdded += (_, device) => Dispatcher.Invoke(ListConnectedDevices);
            usbEventWatcher.UsbDeviceRemoved += (_, device) => Dispatcher.Invoke(ListConnectedDevices);

            normalBorder.BorderBrush = Brushes.Transparent;
            normalBorder.BorderThickness = new Thickness(0);

            errorBorder.BorderBrush = Brushes.Red;
            errorBorder.BorderThickness = new Thickness(3);

            GetProfiles();
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
                DeviceList.SelectDevice(ListBoxDevices.SelectedIndex);

                //DeviceList.SelectedDevice.FileTypes();
            }
            else
            {
                spDeviceInfo.Visibility = Visibility.Collapsed;
                ListBoxDevices.IsEnabled = true;
            }
        }


        //nalezeni a vypis vsech zarizeni vyuzivajicich MTP
        private void ListConnectedDevices()
        {
            ListBoxDevices.SelectedIndex = DeviceList.UpdateDevices();
        }

        private void GetProfiles(string SelectProfile = "")
        {
            var profilesPath = Application.Current.Resources["profilesFolder"].ToString();
            string selectedProfile = SelectProfile;
            if (selectedProfile.Length == 0 && cbProfiles.SelectedItem != null)
            {
                selectedProfile = cbProfiles.SelectedItem.ToString();
            }

            Profiles = Directory.GetFiles(profilesPath, "*.xml").Select(f => Path.GetFileNameWithoutExtension(f)).ToList();

            OnPropertyChanged("Profiles");

            cbProfiles.SelectedIndex = Profiles.IndexOf(selectedProfile);
        }

        //vyhledani vsech souboru v adresari (hleda i v podadresarich)
        private void FindFiles(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            DeviceList.SelectedDevice.GetFilesByDate(worker, e, Settings);
        }

        private void CopyFiles(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            DeviceList.SelectedDevice.CopyFiles(worker, e, Settings);
        }

        private void GetFileTypes(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            DeviceList.SelectedDevice.FileTypes(worker, e);
        }

        //obnovit seznam pripojenych zarizeni
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            ListConnectedDevices();
        }

        private void FindFiles_Click(object sender, RoutedEventArgs e)
        {
            if (CheckDeviceSelected())
            {
                RunWorker(sender, TaskType.FindFiles,true);
            }
        }

        private void CopyFiles_Click(object sender, RoutedEventArgs e)
        {
            if (CheckSettings())
            {
                backupStart = DateTime.Now;
                RunWorker(sender, TaskType.CopyFiles,true);
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

            if (DeviceList.SelectedDeviceInfo.Name == null || DeviceList.SelectedDeviceInfo.Name.Length == 0)
            {
                SetErrorMessage(tbDeviceNameError, "Zadejte název zařízení");
                correct = false;
            }

            if (Settings.Paths.Root == null || Settings.Paths.Root.Length == 0)
            {
                BtnError(btnMainFolder);
                SetErrorMessage(tbRootError, "Zvolte složku pro uložení fotografií");
                correct = false;
            }
            else if (!Directory.Exists(Settings.Paths.Root))
            {
                BtnError(btnMainFolder);
                SetErrorMessage(tbRootError, "Zvolená složka neexistuje");
                correct = false;
            }

            if (Settings.Backup)
            {
                if (Settings.Paths.Backup == null || Settings.Paths.Backup.Length == 0)
                {
                    BtnError(btnChooseBackupDest);
                    SetErrorMessage(tbBackupError, "Zvlote složku pro zálohu");
                    correct = false;
                }
                else if (!Directory.Exists(Settings.Paths.Backup))
                {
                    BtnError(btnChooseBackupDest);
                    SetErrorMessage(tbBackupError, "Zvolená složka neexistuje");
                    correct = false;
                }
            }


            if (Settings.Paths.FolderTags == null || Settings.Paths.FolderTags.Length == 0)
            {
                BtnError(btnFolderStruct);
                SetErrorMessage(tbFolderStructError, "Zvlote strukturu složek pro uložení");
                correct = false;
            }

            if (Settings.Paths.FileTags == null || Settings.Paths.FileTags.Length == 0)
            {
                BtnError(btnFileStruct);
                SetErrorMessage(tbFileStructError, "Zvlote strukturu pro název souboru");
                correct = false;
            }

            if (Settings.Thumbnail)
            {
                if (Settings.Paths.Thumbnail == null || Settings.Paths.Thumbnail.Length == 0)
                {
                    BtnError(btnChooseThumbDest);
                    SetErrorMessage(tbThumbnailDestError, "Zvolte složku pro náhledy");
                    correct = false;
                }
                else if (!Directory.Exists(Settings.Paths.Thumbnail))
                {
                    BtnError(btnChooseThumbDest);
                    SetErrorMessage(tbThumbnailDestError, "Zvolená složka neexistuje");
                    correct = false;
                }

                if (Settings.ThumbnailSettings.Value == 0)
                {
                    SetErrorMessage(tbThumbnailError, "Hodnota nemůže být 0");
                    correct = false;
                }
            }

            if ((bool)cbDateRange.IsChecked && Settings.Date.Start > Settings.Date.End)
            {
                SetErrorMessage(tbDateRangeError, "Počáteční datum nemůže být větší než koncové");
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
                SetErrorMessage(tbSelectDeviceError, "Zvolte zařízení");
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
        private void RunWorker(object sender, TaskType task, bool showProgress)
        {
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.WorkerReportsProgress = showProgress;
            backgroundWorker.ProgressChanged += worker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += worker_RunWorkerCompleted;

            if (showProgress)
            {
                DialogHost.Show(progressDialog, "RootDialog");
                progressDialog.btnCancel.Content = "Zrušit";
            }

            if ((bool)cbNewFiles.IsChecked)
            {
                Settings.Date.Start = DeviceList.SelectedDeviceInfo.LastBackup;
            }

            switch (task)
            {
                case TaskType.FindFiles:
                    backgroundWorker.DoWork += FindFiles;
                    break;
                case TaskType.CopyFiles:
                    backgroundWorker.DoWork += CopyFiles;
                    break;
                case TaskType.GetFileTypes:
                    backgroundWorker.DoWork += GetFileTypes;
                    break;
            }
            backgroundWorker.RunWorkerAsync();
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
            if (e.Cancelled)
            {
                lblResult.Text = "Zrušeno";
                return;
            }

            WorkerResult result = (WorkerResult)e.Result;
            if (result.code == RESULT_ERROR)
            {
                ErrorDialog("Při získávání souborů došlo k chybě. Prosím zkontrolujte, zda je zařízení připojené a akci zopakujte.");
                ListConnectedDevices();
            }
            else if (result.code == RESULT_OK)
            {
                int total = DeviceList.SelectedDevice.FilesTotal;
                int downloaded = DeviceList.SelectedDevice.FilesDoneCount;
                int toDownload = DeviceList.SelectedDevice.FilesToCopyCount;
                int errors = DeviceList.SelectedDevice.Errors;
                lblResult.Text = $"Souborů celkem: {total}\n";
                if (result.task == TaskType.CopyFiles)
                {
                    lblResult.Text += $"Staženo: {downloaded}/{toDownload}\n" +
                                      $"Chyby: {errors}";
                    if (Settings.DownloadSelect == DownloadSelect.lastBackup && downloaded == toDownload)
                    {
                        DeviceList.SelectedDeviceInfo.LastBackup = backupStart;
                    }
                    OnPropertyChanged("SelectedDeviceInfo");
                    DeviceList.Save();
                }
                else if (result.task == TaskType.FindFiles)
                {
                    lblResult.Text += $"Ke stažení: {toDownload}";
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
                    "Datum",
                    new List<string>(){
                        Properties.Resources.YearLong,
                        Properties.Resources.Month,
                        Properties.Resources.Day},
                    new Point(0,0)),

                new ButtonGroupStruct(
                    "Vlastní",
                    new List<string>(){
                        Properties.Resources.CustomText},
                    new Point(1,0)),

                new ButtonGroupStruct(
                    "Zařízení",
                    new List<string>(){
                        Properties.Resources.DeviceName,
                        Properties.Resources.DeviceManuf},
                    new Point(0,1)),

                new ButtonGroupStruct(
                    "Složka",
                    new List<string>(){
                        Properties.Resources.NewFolder},
                    new Point(0,2)),

                new ButtonGroupStruct(
                    "Oddělovač",
                    new List<string>(){
                        Properties.Resources.Hyphen,
                        Properties.Resources.Underscore},
                    new Point(1,1))
            };

            FolderStructDialog nameDialog = new FolderStructDialog(this, buttonGroups, Settings.Paths.FolderTags);
            await DialogHost.Show(nameDialog, "RootDialog");

        }

        //zobrazeni chybove zpravy
        public void ErrorDialog(string message)
        {
            ErrorDialog errorDialog = new ErrorDialog(this, message);
            DialogHost.Show(errorDialog, "RootDialog");
        }

        //zavreni dialogu a ziskani vracenych hodnot
        public void DialogClose(object sender, object result, int requestCode = -1)
        {
            if (sender.GetType() == typeof(FolderStructDialog))
            {
                Settings.Paths.FolderTags = (string)result;
            }
            else if (sender.GetType() == typeof(FileStructDialog))
            {
                Settings.Paths.FileTags = (string)result;
            }
            else if (sender.GetType() == typeof(SaveDialog))
            {
                SaveOptions options = (SaveOptions)result;
                Settings.Save(options);
                GetProfiles(options.FileName); // načte nový profil

            }
            else if (sender.GetType() == typeof(YesNoDialog))
            {
                if (requestCode == REQUEST_PROFILE_DELETE)
                {
                    Settings.Delete(cbProfiles.SelectedItem.ToString());
                    GetProfiles();
                    OnPropertyChanged("Profiles");
                }
            }
            DialogHost.CloseDialogCommand.Execute(null, null);

            tbStructure.Text = Tags.TagsToBlock(Settings.Paths.FolderTags, Settings.Paths.FileTags, DeviceList.SelectedDevice);
        }

        //vytvoreni a zobrazeni dialogu pro nazev souboru
        private async void btnFileStruct_Click(object sender, RoutedEventArgs e)
        {
            BtnNormal(btnFileStruct);
            tbFileStructError.Visibility = Visibility.Collapsed;
            List<ButtonGroupStruct> buttonGroups = new List<ButtonGroupStruct>()
            {
                new ButtonGroupStruct(
                    "Datum",
                    new List<string>(){
                        Properties.Resources.YearLong,
                        Properties.Resources.Month,
                        Properties.Resources.Day},
                    new Point(0,0)),

                new ButtonGroupStruct(
                    "Ostatní",
                    new List<string>(){
                        Properties.Resources.CustomText,
                        Properties.Resources.FileName},
                    new Point(1,0)),

                new ButtonGroupStruct(
                    "Zařízení",
                    new List<string>(){
                        Properties.Resources.DeviceName,
                        Properties.Resources.DeviceManuf},
                    new Point(0,1)),

                // new ButtonGroupStruct(
                //     "Číslování",
                //     new List<string>(){ 
                //         Properties.Resources.SequenceNum},
                //     new Point(0,2)),

                new ButtonGroupStruct(
                    "Oddělovač",
                    new List<string>(){
                        Properties.Resources.Hyphen,
                        Properties.Resources.Underscore},
                    new Point(1,1))
            };

            FileStructDialog nameDialog = new FileStructDialog(this, buttonGroups, Settings.Paths.FileTags);
            await DialogHost.Show(nameDialog, "RootDialog");
        }

        // vyber slozek
        private void btnMainFolder_Click(object sender, RoutedEventArgs e)
        {
            BtnNormal(btnMainFolder);
            tbRootError.Visibility = Visibility.Collapsed;
            string result = ChooseDirectory(Settings.Paths.Root);
            if (result != string.Empty)
            {
                Settings.Paths.Root = result;
            }
        }

        private void btnChooseBackupDest_Click(object sender, RoutedEventArgs e)
        {
            BtnNormal(btnChooseBackupDest);
            tbBackupError.Visibility = Visibility.Collapsed;
            string result = ChooseDirectory(Settings.Paths.Backup);
            if (result != string.Empty)
            {
                Settings.Paths.Backup = result;
            }
        }

        private void btnChooseThumbDest_Click(object sender, RoutedEventArgs e)
        {
            BtnNormal(btnChooseThumbDest);
            tbThumbnailDestError.Visibility = Visibility.Collapsed;
            string result = ChooseDirectory(Settings.Paths.Thumbnail);
            if (result != string.Empty)
            {
                Settings.Paths.Thumbnail = result;
            }
        }
        private string ChooseDirectory(string initDir)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = Settings.Paths.Thumbnail;
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
            if (dp.SelectedDate == null)
            {
                dp.SelectedDate = DateTime.Now;
            }

            if (Settings.Date.Start > Settings.Date.End)
            {
                SetErrorMessage(tbDateRangeError, "Počáteční datum nemůže být větší než koncové");
            }
            else
            {
                tbDateRangeError.Visibility = Visibility.Collapsed;
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
                Settings.Save();
            }

        }
        private async void btnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveDialog saveDialog = new SaveDialog(this);
            await DialogHost.Show(saveDialog, "RootDialog");
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            GetProfiles();
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            YesNoDialog ynDialog = new YesNoDialog(this, $"Přejete si smazat profil [{cbProfiles.SelectedItem}]?", REQUEST_PROFILE_DELETE);
            await DialogHost.Show(ynDialog, "RootDialog");
        }

        private void cbProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb.SelectedItem != null)
            {
                Settings.Load(cb.SelectedItem.ToString());
                OnPropertyChanged("Settings");
                tbStructure.Text = Tags.TagsToBlock(Settings.Paths.FolderTags, Settings.Paths.FileTags, DeviceList.SelectedDevice);

                RemoveAllErrors();
                CheckSettings();
            }
        }



        private void Window_Closing(object sender, CancelEventArgs e)
        {
            DeviceList.Save();
            ClearTemp();
        }

        private void cbNewFiles_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Date = new DateRange();
            if (DeviceList.SelectedDevice != null)
            {
                Settings.Date.Start = DeviceList.SelectedDeviceInfo.LastBackup;
            }
        }

        private void tbValue_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Settings.ThumbnailSettings.Value == 0)
            {
                SetErrorMessage(tbBackupError, "Hodnota nemůže být 0");
            }
        }

        private void tbValue_GotFocus(object sender, RoutedEventArgs e)
        {
            tbThumbnailError.Visibility = Visibility.Collapsed;
        }

        private void cbDateRange_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Date.Start = Settings.Date.Start.Date;
            if (Settings.Date.Start.Date == new DateTime().Date)
            {
                Settings.Date = new DateRange();
                OnPropertyChanged("Settings");
            }
        }

        private void btnShowLog_Click(object sender, RoutedEventArgs e)
        {
            var directory = new DirectoryInfo(logFolder);
            var lastLog = directory.GetFiles().OrderByDescending(f => f.LastWriteTime).First();
            Process.Start("explorer.exe", lastLog.FullName);
        }

        public static void ClearTemp()
        {
            Parallel.ForEach(Directory.EnumerateFiles(tmpFolder), (file) =>
            {
                File.Delete(file);
            });
        }

        private void lblDeviceName_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (DeviceList.SelectedIndex != -1 && tb.Text.Length == 0)
            {
                SetErrorMessage(tbDeviceNameError, "Zadejte název zařízení");
                ListBoxDevices.IsEnabled = false;
            }
            else
            {
                tbDeviceNameError.Visibility = Visibility.Collapsed;
                ListBoxDevices.IsEnabled = true;
            }
        }

        private async void btnInfoClick(object sender, RoutedEventArgs e)
        {
            var AppInfoDialog = new AppInfoDialog(this);
            await DialogHost.Show(AppInfoDialog, "RootDialog");
        }
    }
}


