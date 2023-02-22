using MaterialDesignThemes.Wpf;
using Octokit;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.Dialogs
{
    /// <summary>
    /// Interaction logic for UpdateDialog.xaml
    /// </summary>
    public partial class UpdateDialog : UserControl
    {
        public DialogSession Session { private get; set; }
        public UpdateDialog(Release release)
        {
            InitializeComponent();
            var version = Version.Parse(release.TagName.Replace("v", ""));
            ucUpdate.VersionInfo = $"{Properties.Resources.Update_NewVersionAvailable} ({version})";
            ucUpdate.Release = release;
            ucUpdate.DownloadingChanged += UcUpdate_DownloadingChanged;
        }

        private void UcUpdate_DownloadingChanged(object sender, EventArgs e)
        {
            if (ucUpdate.Downloading)
            {
                btnOK.IsEnabled = false;
            }
            else
            {
                btnOK.IsEnabled = true;
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Session?.Close();
        }
    }
}
