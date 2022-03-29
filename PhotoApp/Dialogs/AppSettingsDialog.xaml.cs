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
            InitializeComponent();
            mainWindow = window;
            cbDarkMode.IsChecked = Properties.Settings.Default.DarkMode;
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
    }
}
