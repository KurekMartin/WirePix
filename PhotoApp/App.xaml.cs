using System;
using System.IO;
using System.Windows;

namespace PhotoApp
{
    /// <summary>
    /// Interakční logika pro App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Resources.Add("tmpFolder", Path.Combine(Path.GetTempPath(), "PhotoApp"));
            Resources.Add("logFolder", Path.Combine(appData, "PhotoApp", "Logs"));
            Resources.Add("profilesFolder", Path.Combine(appData, "PhotoApp", "Profiles"));
            Resources.Add("dataFolder", Path.Combine(appData, "PhotoApp", "Data"));
        }
    }
}
