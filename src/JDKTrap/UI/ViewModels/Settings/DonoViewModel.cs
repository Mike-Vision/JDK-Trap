using System;
using System.Diagnostics;
using System.Windows.Input;
using JDKTrap.UI.ViewModels.ContextMenu;

namespace JDKTrap.UI.Elements.Settings.Pages
{
    public class DonoPageViewModel
    {
        public ICommand OpenUrlCommand { get; }

        public DonoPageViewModel()
        {
            OpenUrlCommand = new RelayCommand(OpenUrl);
        }

        private void OpenUrl(object parameter)
        {
            if (parameter is string url)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
        }
    }
}
