using MaterialDesignThemes.Wpf;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.Dialogs
{
    /// <summary>
    /// Interaction logic for UsedLibraries.xaml
    /// </summary>
    public partial class UsedLibraries : UserControl
    {
        public DialogSession Session { private get; set; }
        public UsedLibraries()
        {
            InitializeComponent();
        }

        private void btnMagickNET_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/dlemstra/Magick.NET");
        }

        private void btnMaterialDesign_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit");
        }

        private void btnMetadataExtractor_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/drewnoakes/metadata-extractor-dotnet");
        }

        private void btnOctokit_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/octokit/octokit.net");
        }

        private void btnUsbEvents_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Jinjinov/Usb.Events");
        }

        private void btnMediaDevices_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Bassman2/MediaDevices");
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Session?.Close();
        }
    }
}
