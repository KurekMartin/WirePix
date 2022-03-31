﻿using Octokit;
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

                if (latestVersion < currentVersion)
                {
                    ucUpdate.VersionInfo = "Je dostupná novější verze programu";
                    ucUpdate.Release = release;
                }
                else
                {
                    ucUpdate.VersionInfo = "Máte nejnovější verzi programu";
                }
            }
            catch
            {
                ucUpdate.VersionInfo = "Nebylo možné získat informace o dostupnosti novější verze";
            }
            MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(btnCheckUpdate, false);
        }

        private void ucUpdate_DownloadingChanged(object sender, EventArgs e)
        {
            btnCheckUpdate.IsEnabled = btnOK.IsEnabled = !ucUpdate.Downloading;
        }
    }
}
