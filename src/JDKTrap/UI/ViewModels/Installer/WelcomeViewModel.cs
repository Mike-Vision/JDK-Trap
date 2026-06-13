namespace JDKTrap.UI.ViewModels.Installer
{
    public class WelcomeViewModel : NotifyPropertyChangedViewModel
    {
        // formatting is done here instead of in xaml, it's just a bit easier
        public string MainText => String.Format(
            Strings.Installer_Welcome_MainText,
            $"https://github.com/{App.ProjectRepository.Trim('/')}",
            "https://boisterous-souffle-33150b.netlify.app"
        );

        public bool CanContinue { get; set; } = false;
    }
}
