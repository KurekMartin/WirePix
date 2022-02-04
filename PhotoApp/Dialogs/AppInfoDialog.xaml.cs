using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.Dialogs
{
    /// <summary>
    /// Interakční logika pro ErrorDialog.xaml
    /// </summary>
    public partial class AppInfoDialog : UserControl
    {
        private MainWindow mainWindow;
        public AppInfoDialog(MainWindow window)
        {
            InitializeComponent();
            mainWindow = window;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.DialogClose(this, null);
        }
    }
}
