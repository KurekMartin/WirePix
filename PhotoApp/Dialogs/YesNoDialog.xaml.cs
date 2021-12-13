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
    /// Interakční logika pro YesNoDialog.xaml
    /// </summary>
    public partial class YesNoDialog : UserControl
    {
        private int _requestCode;
        public static int RESULT_YES = 1;
        public static int RESULT_NO = 0;
        private MainWindow _mainWindow;
        public YesNoDialog(MainWindow window, string text,int requestCode)
        {
            InitializeComponent();
            tbMain.Text = text;
            _requestCode = requestCode;
            _mainWindow = window;
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.DialogClose(this, RESULT_YES,_requestCode);
        }
    }
}
