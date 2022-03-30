using Octokit;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace PhotoApp.Dialogs
{
    /// <summary>
    /// Interakční logika pro ErrorDialog.xaml
    /// </summary>
    public partial class AppInfoDialog : UserControl
    {
        private MainWindow mainWindow;
        private Release newRelease;
        private bool downloading = false;
        private static readonly string _tmpFolder = System.Windows.Application.Current.Resources[Properties.Keys.TempFolder].ToString();
        public AppInfoDialog(MainWindow window)
        {
            InitializeComponent();
            mainWindow = window;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.DialogClose(this, null);
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
                    tbVersionInfo.Text = "Je dostupná novější verze programu";
                    newRelease = release;
                    spDownloadUpdate.Visibility = Visibility.Visible;
                }
                else
                {
                    tbVersionInfo.Text = "Máte nejnovější verzi programu";
                    spDownloadUpdate.Visibility = Visibility.Hidden;
                }
            }
            catch
            {
                tbVersionInfo.Text = "Nebylo možné získat informace o dostupnosti novější verze";
            }
            tbVersionInfo.Visibility = Visibility.Visible;
            MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(btnCheckUpdate, false);
        }

        private async void btnAutoUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (newRelease != null && !downloading)
            {
                downloading = true;
                var asset = newRelease.Assets.FirstOrDefault(a => a.Name.EndsWith(".msi"));
                string installerPath = Path.Combine(_tmpFolder, asset.Name);

                WebClient webClient = new WebClient();
                MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(btnAutoUpdate, true);
                btnOK.IsEnabled = btnCheckUpdate.IsEnabled = false; //disable buttons
                await webClient.DownloadFileTaskAsync(new Uri(asset.BrowserDownloadUrl), installerPath);
                downloading = false;
                MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(btnAutoUpdate, false);

                Process.Start(installerPath);
                System.Windows.Application.Current.Shutdown();
            }

        }

    private void btnManualUpdate_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(newRelease.HtmlUrl);
    }
}
}
