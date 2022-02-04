using Octokit;
using System;
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

        private async void btnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(btnCheckUpdate, true);
            var currentVersion = Version.Parse(((App)System.Windows.Application.Current).Version);

            try
            {
                var github = new GitHubClient(new ProductHeaderValue("MartinKurek"));
                var release = await github.Repository.Release.GetLatest("Bassman2", "MediaDevices"); //změnit
                var latestVersion = Version.Parse(release.TagName);
               
                if (latestVersion > currentVersion)
                {
                    tbVersionInfo.Text = "Je dostupná novější verze programu";
                    //System.Diagnostics.Process.Start(release.HtmlUrl);
                }
                else
                {
                    tbVersionInfo.Text = "Máte nejnovější verzi programu";
                }
            }
            catch
            {
                tbVersionInfo.Text = "Nebylo možné získat informace o dostupnosti novější verze";
            }
            tbVersionInfo.Visibility = Visibility.Visible;
            MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(btnCheckUpdate, false);

            

            //Console.WriteLine(release.TagName);

        }
    }
}
