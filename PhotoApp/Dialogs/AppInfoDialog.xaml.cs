using Octokit;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using MaterialDesignThemes.Wpf;

namespace PhotoApp.Dialogs
{
    /// <summary>
    /// Interakční logika pro ErrorDialog.xaml
    /// </summary>
    public partial class AppInfoDialog : UserControl
    {
        public DialogSession Session { private get; set; }
        public AppInfoDialog()
        {
            InitializeComponent();
        }

        private async void btnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(btnCheckUpdate, true);
            var currentVersion = Version.Parse(((App)System.Windows.Application.Current).Version);

            try
            {
                var github = new GitHubClient(new ProductHeaderValue("WirePix"));
                var release = await github.Repository.Release.GetLatest("KurekMartin", "WirePix");
                var latestVersion = Version.Parse(release.TagName.Replace("v", ""));

                if (latestVersion > currentVersion)
                {
                    ucUpdate.VersionInfo = $"{Properties.Resources.Update_NewVersionAvailable} ({latestVersion})";
                    ucUpdate.Release = release;
                }
                else
                {
                    ucUpdate.VersionInfo = Properties.Resources.Update_NoNewVerison;
                }
            }
            catch
            {
                ucUpdate.VersionInfo = Properties.Resources.Update_Error;
            }
            MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(btnCheckUpdate, false);
        }

        private void ucUpdate_DownloadingChanged(object sender, EventArgs e)
        {
            btnCheckUpdate.IsEnabled = btnOK.IsEnabled = !ucUpdate.Downloading;
        }

        private void btnShowLicense_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/KurekMartin/WirePix/blob/master/LICENSE");
        }

        private void btnShowCode_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/KurekMartin/WirePix");
        }

        private void tbEmail_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Clipboard.SetText(tbEmail.Text);
            SnackBar.MessageQueue.Enqueue(Properties.Resources.EmailCopied);
        }

        private void btnLibraries_Click(object sender, RoutedEventArgs e)
        {
            Session?.Close(MainWindow.DIALOG_SHOW_LIBRARIES);
        }

        private void btnShowChangelog_Click(object sender, RoutedEventArgs e)
        {
            Session?.Close(MainWindow.DIALOG_SHOW_CHANGELOG);
        }

        private void btnShowAppdata_Click(object sender, RoutedEventArgs e)
        {
            string dataFolder = System.Windows.Application.Current.Resources[Properties.Keys.MainFolder].ToString();
            System.Diagnostics.Process.Start("explorer.exe", dataFolder);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Session?.Close();
        }
    }
}
