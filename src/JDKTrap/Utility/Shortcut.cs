using System.Windows;
using JDKTrap.Resources;

namespace JDKTrap.Utility
{
    internal static class Shortcut
    {
        private static GenericTriState _loadStatus = GenericTriState.Unknown;

        public static void Create(string exePath, string exeArgs, string lnkPath)
        {
            const string LOG_IDENT = "Shortcut::Create";

            try
            {
                if (File.Exists(lnkPath))
                {
                    Filesystem.AssertReadOnly(lnkPath);
                    File.Delete(lnkPath);
                }

                var shortcut = ShellLink.Shortcut.CreateShortcut(exePath, exeArgs, exePath, 0);
                if (shortcut.StringData == null)
                    shortcut.StringData = new ShellLink.Structures.StringData();
                
                shortcut.StringData.WorkingDir = Path.GetDirectoryName(exePath) ?? "";
                shortcut.WriteToFile(lnkPath);

                if (_loadStatus != GenericTriState.Successful)
                    _loadStatus = GenericTriState.Successful;
            }
            catch (FileNotFoundException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to create a shortcut for {lnkPath}!");
                App.Logger.WriteException(LOG_IDENT, ex);

                if (_loadStatus == GenericTriState.Failed)
                    return;

                _loadStatus = GenericTriState.Failed;

                Frontend.ShowMessageBox(Strings.Dialog_CannotCreateShortcuts, MessageBoxImage.Information);
            }
        }
    }
}
