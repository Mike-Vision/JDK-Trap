using System.Windows;
using System.Windows.Navigation;
using JDKTrap.UI.ViewModels.Installer;

namespace JDKTrap.UI.Elements.Installer.Pages
{
    /// <summary>
    /// Interaction logic for WelcomePage.xaml
    /// </summary>
    public partial class WelcomePage
    {
        private readonly WelcomeViewModel _viewModel = new();

        public WelcomePage()
        {
                if (Window.GetWindow(this) is MainWindow window)
                    window.SetButtonEnabled("next", true);

            DataContext = _viewModel;
            InitializeComponent();
        }

        private void UiPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow window)
                window.SetNextButtonText(Strings.Common_Navigation_Next);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        private void DonateButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://boisterous-souffle-33150b.netlify.app/donate/donate") { UseShellExecute = true });
        }
        private void ContributorsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://boisterous-souffle-33150b.netlify.app/contributors/contributors") { UseShellExecute = true });
        }
        private void WebsiteCard_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://boisterous-souffle-33150b.netlify.app/home") { UseShellExecute = true });
        }
        private void ModsCard_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://boisterous-souffle-33150b.netlify.app/mods/mods") { UseShellExecute = true });
        }
        private void CrosshairsCard_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://boisterous-souffle-33150b.netlify.app/crosshairs/crosshairs") { UseShellExecute = true });
        }
        private void DocumentationCard_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://boisterous-souffle-33150b.netlify.app/documentation/documentation") { UseShellExecute = true });
        }
    }
}
