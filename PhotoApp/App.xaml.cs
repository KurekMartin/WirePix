using System;
using System.Deployment.Application;
using System.IO;
using System.Reflection;
using System.Windows;

namespace PhotoApp
{
    /// <summary>
    /// Interakční logika pro App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string mainFolder = "PhotoManager";
            Resources.Add("tmpFolder", Path.Combine(Path.GetTempPath(), mainFolder));
            Resources.Add("logFolder", Path.Combine(appData, mainFolder, "Logs"));
            Resources.Add("profilesFolder", Path.Combine(appData, mainFolder, "Profiles"));
            Resources.Add("dataFolder", Path.Combine(appData, mainFolder, "Data"));

            var keys = Current.Resources.Keys.GetEnumerator();
            while (keys.MoveNext())
            {
                string key = keys.Current.ToString();
                if (key.Contains("Folder"))
                {
                    Directory.CreateDirectory(Current.Resources[key].ToString());
                }

            }


            //var github = new GitHubClient(new ProductHeaderValue("MartinKurek"));
            //var release = await github.Repository.Release.GetLatest("octokit", "octokit.net");
            //Console.WriteLine(release.TagName);
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
    }
}
