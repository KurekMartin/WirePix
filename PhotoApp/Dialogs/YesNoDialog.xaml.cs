using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.Dialogs
{
    /// <summary>
    /// Interakční logika pro YesNoDialog.xaml
    /// </summary>
    public partial class YesNoDialog : UserControl
    {
        public DialogSession Session { private get; set; }
        private int _requestCode;
        public YesNoDialog(string text)
        {
            InitializeComponent();
            tbMain.Text = text;
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            Session?.Close(true);
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            Session?.Close(false);
        }
    }
}
