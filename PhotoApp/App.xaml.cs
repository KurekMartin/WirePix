using MaterialDesignThemes.Wpf;
using System;
using System.Deployment.Application;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Linq;

namespace PhotoApp
{
    /// <summary>
    /// Interakční logika pro App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("cs");
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string mainFolder = "WirePix";
            Resources.Add(PhotoApp.Properties.Keys.TempFolder, Path.Combine(Path.GetTempPath(), mainFolder));
            Resources.Add(PhotoApp.Properties.Keys.LogsFolder, Path.Combine(appData, mainFolder, "Logs"));
            Resources.Add(PhotoApp.Properties.Keys.ProfilesFolder, Path.Combine(appData, mainFolder, "Profiles"));
            Resources.Add(PhotoApp.Properties.Keys.DataFolder, Path.Combine(appData, mainFolder, "Data"));
            Resources.Add(PhotoApp.Properties.Keys.CrashReportsFolder, Path.Combine(appData, mainFolder, "Crash Reports"));

            var keys = Current.Resources.Keys.GetEnumerator();
            while (keys.MoveNext())
            {
                string key = keys.Current.ToString();
                if (key.Contains("Folder"))
                {
                    Directory.CreateDirectory(Current.Resources[key].ToString());
                }
            }

            var files = Directory.GetFiles(Current.Resources[PhotoApp.Properties.Keys.TempFolder].ToString()).Where(x => x.EndsWith(".msi"));
            foreach(var file in files)
            {
                File.Delete(file);
            }

            SetThemeMode(PhotoApp.Properties.Settings.Default.DarkMode);
        }

        public string Version
        {
            get
            {
                try
                {
                    return ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString(3);
                }
                catch (Exception)
                {
                    return Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
                }
            }
        }

        public static void SetThemeMode(bool darkMode)
        {
            var paletteHelper = new PaletteHelper();
            //Retrieve the app's existing theme
            ITheme theme = paletteHelper.GetTheme();
            if (darkMode)
            {
                theme.SetBaseTheme(Theme.Dark);
            }
            else
            {
                theme.SetBaseTheme(Theme.Light);
            }
            paletteHelper.SetTheme(theme);
            if (darkMode != PhotoApp.Properties.Settings.Default.DarkMode)
            {
                PhotoApp.Properties.Settings.Default.DarkMode = darkMode;
                PhotoApp.Properties.Settings.Default.Save();
            }
        }
    }
}
