using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.Dialogs
{
    /// <summary>
    /// Interakční logika pro ErrorDialog.xaml
    /// </summary>
    public partial class ErrorDialog : UserControl
    {
public DialogSession Session { private get; set; }
        public ErrorDialog(string msg)
        {
            InitializeComponent();
            lblMessage.Text = msg;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Session?.Close();
        }
    }
}
