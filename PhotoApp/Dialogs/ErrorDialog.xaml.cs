using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PhotoApp.Dialogs
{
    /// <summary>
    /// Interakční logika pro ErrorDialog.xaml
    /// </summary>
    public partial class ErrorDialog : UserControl
    {
        private MainWindow mainWindow;
        public ErrorDialog(MainWindow window,string msg)
        {
            InitializeComponent();
            mainWindow = window;
            lblMessage.Text = msg;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.DialogClose(this,null);
        }
    }
}
