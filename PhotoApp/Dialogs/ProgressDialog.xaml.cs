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

namespace PhotoApp
{
    /// <summary>
    /// Interakční logika pro ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : UserControl
    {
        private MainWindow mainWindow;
        public ProgressDialog(MainWindow window)
        {
            InitializeComponent();
            mainWindow = window;
        }

        public void SetCurrentTask(string taskName)
        {
            lblCurrentTask.Text = taskName;
        }

        public void SetCurrentProgress(string progressMessage, int progress, TimeSpan time = new TimeSpan())
        {
            pbProgress.IsIndeterminate = false;
            lblProgress.Text = progressMessage;
            pbProgress.Value = progress;

            SetTimeRemain(time);
        }

        public void SetProgressMessage(string message)
        {
            lblProgress.Text = message;
        }

        public void SetCurrentDir(string dirName)
        {
            lblCurrentFile.Text = dirName;
        }

        public void SetIndeterminateProgress()
        {
            pbProgress.IsIndeterminate = true;
            lblTime.Visibility = Visibility.Collapsed;
        }

        public void SetTimeRemain(TimeSpan time)
        {
            lblTime.Visibility = Visibility.Visible;
            if(time.Ticks == 0)
            {
                lblTime.Text = "Počítám zbývající čas";
            }
            else
            {
                lblTime.Text = "Zbývá ";
                if (time.Days>0)
                {
                    lblTime.Text += time.Days + "d ";
                }
                if(time.Hours>0)
                {
                    lblTime.Text += time.Hours + "h ";
                }
                if(time.Minutes>0)
                {
                    lblTime.Text += time.Minutes + "m ";
                }
                lblTime.Text += time.Seconds + "s ";
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.worker_Cancel();
            btnCancel.Content = new CircularProgress();
            btnCancel.IsEnabled = false;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            btnCancel.IsEnabled = true;
        }
    }
}
