using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.Dialogs
{
    /// <summary>
    /// Interakční logika pro ErrorDialog.xaml
    /// </summary>
    public partial class AppSettingsDialog : UserControl
    {
        private MainWindow mainWindow;
        public AppSettingsDialog(MainWindow window)
        {
            DataContext = Properties.Settings.Default;
            InitializeComponent();
            mainWindow = window;
        }

        private void cbDarkMode_Changed(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            App.SetThemeMode((bool)checkBox.IsChecked);
        }
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.DialogClose(this, null);
        }

        private void cbCheckNewVersion_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            Properties.Settings.Default.CheckUpdateOnStartup = (bool)checkBox.IsChecked;
            Properties.Settings.Default.Save();
        }
    }
}
