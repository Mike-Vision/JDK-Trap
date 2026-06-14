using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using JDKTrap.Integrations;
using JDKTrap.UI.Elements.Dialogs;
using JDKTrap.UI.ViewModels.Settings;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace JDKTrap
{
    public static class LaunchHandler
    {
        public static void ProcessNextAction(NextAction action, bool isUnfinishedInstall = false)
        {
            const string LOG_IDENT = "LaunchHandler::ProcessNextAction";

            switch (action)
            {
                case NextAction.LaunchSettings:
                    App.Logger.WriteLine(LOG_IDENT, "Opening settings");
                    LaunchSettings();
                    break;

                case NextAction.LaunchRoblox:
                    App.Logger.WriteLine(LOG_IDENT, "Opening Roblox");
                    LaunchRoblox(LaunchMode.Player);
                    break;

                case NextAction.LaunchRobloxStudio:
                    App.Logger.WriteLine(LOG_IDENT, "Opening Roblox Studio");
                    LaunchRoblox(LaunchMode.Studio);
                    break;

                default:
                    App.Logger.WriteLine(LOG_IDENT, "Closing");
                    App.Terminate(isUnfinishedInstall ? ErrorCode.ERROR_INSTALL_USEREXIT : ErrorCode.ERROR_SUCCESS);
                    break;
            }
        }

        public static void ProcessLaunchArgs()
        {
            const string LOG_IDENT = "LaunchHandler::ProcessLaunchArgs";
            if (App.LaunchSettings.UninstallFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening uninstaller");
                LaunchUninstaller();
            }
            else if (App.LaunchSettings.MenuFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening settings");
                LaunchSettings();
            }
            else if (App.LaunchSettings.WatcherFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening watcher");
                LaunchWatcher();
            }
            else if (App.LaunchSettings.RobloxLaunchMode != LaunchMode.None)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Opening bootstrapper ({App.LaunchSettings.RobloxLaunchMode})");
                LaunchRoblox(App.LaunchSettings.RobloxLaunchMode);
            }
            else if (App.LaunchSettings.BloxshadeFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening Bloxshade");
                LaunchBloxshadeConfig();
            }
            else if (!App.LaunchSettings.QuietFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening menu");
                LaunchMenu();
            }
            else
            {
                App.Logger.WriteLine(LOG_IDENT, "Closing - quiet flag active");
                App.Terminate();
            }
        }

        public static void LaunchInstaller()
        {
            var interlock = new InterProcessLock("Installer");

            try
            {
                if (!interlock.IsAcquired)
                {
                    Frontend.ShowMessageBox(Strings.Dialog_AlreadyRunning_Installer, MessageBoxImage.Stop);
                    App.Terminate();
                    return;
                }

                if (App.LaunchSettings.UninstallFlag.Active)
                {
                    Frontend.ShowMessageBox(Strings.Bootstrapper_FirstRunUninstall, MessageBoxImage.Error);
                    App.Terminate(ErrorCode.ERROR_INVALID_FUNCTION);
                    return;
                }

                if (App.LaunchSettings.QuietFlag.Active)
                {
                    var installer = new Installer();

                    if (!installer.CheckInstallLocation())
                        App.Terminate(ErrorCode.ERROR_INSTALL_FAILURE);

                    installer.DoInstall();
                    interlock.Dispose();

                    ProcessLaunchArgs();
                }
                else
                {
#if QA_BUILD
                    Frontend.ShowMessageBox(
                        "You are about to install a QA build of JDKTrap. The red window border indicates that this is a QA build.\n\n" +
                        "QA builds are handled completely separately of your standard installation, like a virtual environment.",
                        MessageBoxImage.Information);
#endif

                    new LanguageSelectorDialog().ShowDialog();

                    var installer = new UI.Elements.Installer.MainWindow();
                    installer.ShowDialog();
                    interlock.Dispose();

                    ProcessNextAction(installer.CloseAction, !installer.Finished);
                }
            }
            finally
            {
                interlock.Dispose();
            }
        }

        public static void LaunchUninstaller()
        {
            using var interlock = new InterProcessLock("Uninstaller");

            if (!interlock.IsAcquired)
            {
                Frontend.ShowMessageBox(Strings.Dialog_AlreadyRunning_Uninstaller, MessageBoxImage.Stop);
                App.Terminate();
                return;
            }

            bool confirmed;
            bool keepData = true;

            if (App.LaunchSettings.QuietFlag.Active)
            {
                confirmed = true;
            }
            else
            {
                var dialog = new UninstallerDialog();
                dialog.ShowDialog();

                confirmed = dialog.Confirmed;
                keepData = dialog.KeepData;
            }

            if (!confirmed)
            {
                App.Terminate();
                return;
            }

            Installer.DoUninstall(keepData);

            Frontend.ShowMessageBox(Strings.Bootstrapper_SuccessfullyUninstalled, MessageBoxImage.Information);
            App.Terminate();
        }

        public static void LaunchSettings()
        {
            const string LOG_IDENT = "LaunchHandler::LaunchSettings";

            using var interlock = new InterProcessLock("Settings");

            if (interlock.IsAcquired)
            {
                bool showAlreadyRunningWarning = false;
                var runningProcs = Process.GetProcessesByName(App.ProjectName);
                try
                {
                    showAlreadyRunningWarning = runningProcs.Length > 1;
                }
                finally
                {
                    foreach (var p in runningProcs) p.Dispose();
                }

                var window = new UI.Elements.Settings.MainWindow(showAlreadyRunningWarning);
                window.ShowDialog();
            }
            else
            {
                App.Logger.WriteLine(LOG_IDENT, "Found an already existing menu window");

                var processes = Utilities.GetProcessesSafe();
                try
                {
                    var process = processes.FirstOrDefault(x => x.MainWindowTitle == Strings.Menu_Title);
                    if (process is not null && process.MainWindowHandle != IntPtr.Zero)
                    {
                        PInvoke.SetForegroundWindow(new HWND(process.MainWindowHandle));
                    }
                }
                finally
                {
                    foreach (var p in processes) p.Dispose();
                }

                App.Terminate();
            }
        }

        public static void LaunchMenu()
        {
            var dialog = new LaunchMenuDialog();
            dialog.ShowDialog();

            ProcessNextAction(dialog.CloseAction);
        }

        public static void LaunchRoblox(LaunchMode launchMode)
        {
            const string LOG_IDENT = "LaunchHandler::LaunchRoblox";
            const string GlobalMutexName = @"Global\ROBLOX_singletonMutex";
            const string LocalMutexName = "ROBLOX_singletonMutex"; // fallback idk, was cuz someone had a issue with this so added a fallback

            if (launchMode == LaunchMode.None)
                throw new InvalidOperationException("No Roblox launch mode set");

            if (!File.Exists(Path.Combine(Paths.System, "mfplat.dll")))
            {
                Frontend.ShowMessageBox(Strings.Bootstrapper_WMFNotFound, MessageBoxImage.Error);

                if (!App.LaunchSettings.QuietFlag.Active)
                {
                    Utilities.ShellExecute(
                        "https://support.microsoft.com/en-us/topic/media-feature-pack-list-for-windows-n-editions-c1c6fffa-d052-8338-7a79-a4bb980a700a");
                }

                App.Terminate(ErrorCode.ERROR_FILE_NOT_FOUND);
                return;
            }

            bool robloxRunning = false;
            try
            {
                robloxRunning = Mutex.TryOpenExisting(GlobalMutexName, out _);
            }
            catch (UnauthorizedAccessException)
            {
                robloxRunning = false;
            }
            catch
            {
                robloxRunning = false;
            }

            if (!robloxRunning)
            {
                try
                {
                    robloxRunning = Mutex.TryOpenExisting(LocalMutexName, out _);
                }
                catch
                {
                    robloxRunning = false;
                }
            }

            if (App.Settings.Prop.ConfirmLaunches
                && robloxRunning
                && !(App.Settings.Prop.IsGameEnabled && !string.IsNullOrWhiteSpace(App.Settings.Prop.LaunchGameID)))
            {
                var result = Frontend.ShowMessageBox(
                    Strings.Bootstrapper_ConfirmLaunch,
                    MessageBoxImage.Warning,
                    MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                {
                    App.Terminate();
                    return;
                }
            }

            App.Logger.WriteLine(LOG_IDENT, "Initializing bootstrapper");
            App.Bootstrapper = new Bootstrapper(launchMode);

            IBootstrapperDialog? dialog = null;
            if (!App.LaunchSettings.QuietFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Initializing bootstrapper dialog");
                dialog = App.Settings.Prop.BootstrapperStyle.GetNew();
                App.Bootstrapper.Dialog = dialog;
                dialog.Bootstrapper = App.Bootstrapper;
            }

            Mutex? mutex = null;
            EventWaitHandle? singletonEvent = null;
            Mutex? localMutex = null;
            EventWaitHandle? localEvent = null;

            if (App.Settings.Prop.MultiInstance)
            {
                try
                {
                    var mutexSecurity = new MutexSecurity();
                    mutexSecurity.AddAccessRule(new MutexAccessRule(
                        new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                        MutexRights.FullControl,
                        AccessControlType.Deny
                    ));

                    var eventSecurity = new EventWaitHandleSecurity();
                    eventSecurity.AddAccessRule(new EventWaitHandleAccessRule(
                        new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                        EventWaitHandleRights.FullControl,
                        AccessControlType.Deny
                    ));

                    // Block global singleton mutex (prevents Roblox from detecting existing instances)
                    mutex = MutexAcl.Create(true, @"Global\ROBLOX_singletonMutex", out _, mutexSecurity);

                    // Block global singleton event (prevents second Roblox from signaling the first to take over)
                    singletonEvent = EventWaitHandleAcl.Create(true, EventResetMode.AutoReset, @"Global\ROBLOX_singletonEvent", out _, eventSecurity);

                    // Also block local (non-Global) variants as fallback
                    try { localMutex = MutexAcl.Create(true, "ROBLOX_singletonMutex", out _, mutexSecurity); } catch { }
                    try { localEvent = EventWaitHandleAcl.Create(true, EventResetMode.AutoReset, "ROBLOX_singletonEvent", out _, eventSecurity); } catch { }

                    App.Logger.WriteLine(LOG_IDENT, "Created Multi-Instance Mutex and EventWaitHandle with Deny Everyone DACL (Global + Local).");
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to create Multi-Instance sync objects: {ex.Message}");
                }
            }

            if (App.Settings.Prop.ExclusiveFullscreen)
            {
                _ = Task.Run(RobloxFullscreen.WaitAndTriggerFullscreen); // redid https://github.com/Mike-Vision/JDK-Trap/pull/362/changes/f0177af4ec39475a5b5c8ea5adc365dcdba0b0d9#diff-ed77fad50af3a8225af6d4c3e81af6095905805d31369da2b5d54f0c2382180e
            }

            Task.Run(App.Bootstrapper.Run).ContinueWith(t =>
            {
                App.Logger.WriteLine(LOG_IDENT, "Bootstrapper task has finished");

                try
                {
                    if (t.IsFaulted)
                    {
                        App.Logger.WriteLine(LOG_IDENT, "An exception occurred when running the bootstrapper");

                        if (t.Exception is not null)
                            App.FinalizeExceptionHandling(t.Exception);
                    }

                    if (mutex != null)
                    {
                        string processName = App.RobloxPlayerAppName.Split('.')[0];
                        App.Logger.WriteLine(LOG_IDENT, $"Resolved Roblox name {processName}.exe, running in background.");

                        while (true)
                        {
                            var procs = Process.GetProcessesByName(processName);
                            bool any = procs.Any();
                            foreach (var p in procs) p.Dispose();
                            if (!any) break;
                            Thread.Sleep(5000);
                        }

                        App.Logger.WriteLine(LOG_IDENT, "Every Roblox instance is closed, terminating the process");
                    }
                }
                finally
                {
                    if (mutex != null)
                    {
                        try { mutex.ReleaseMutex(); } catch { }
                        mutex.Dispose();
                    }

                    if (singletonEvent != null)
                    {
                        try { singletonEvent.Dispose(); } catch { }
                    }

                    if (localMutex != null)
                    {
                        try { localMutex.ReleaseMutex(); } catch { }
                        localMutex.Dispose();
                    }

                    if (localEvent != null)
                    {
                        try { localEvent.Dispose(); } catch { }
                    }

                    App.Terminate();
                }
            });

            dialog?.ShowBootstrapper();
            App.Logger.WriteLine(LOG_IDENT, "Exiting");
        }

        public static void LaunchWatcher()
        {
            const string LOG_IDENT = "LaunchHandler::LaunchWatcher";

            var watcher = new Watcher();

            Task.Run(watcher.Run).ContinueWith(t =>
            {
                App.Logger.WriteLine(LOG_IDENT, "Watcher task has finished");

                watcher.Dispose();

                if (t.IsFaulted)
                {
                    App.Logger.WriteLine(LOG_IDENT, "An exception occurred when running the watcher");

                    if (t.Exception is not null)
                        App.FinalizeExceptionHandling(t.Exception);
                }

                if (App.Settings.Prop.CleanerOptions != CleanerOptions.Never)
                    Cleaner.DoCleaning();

                App.Terminate();
            });
        }

        public static void LaunchBloxshadeConfig()
        {
            const string LOG_IDENT = "LaunchHandler::LaunchBloxshade";

            App.Logger.WriteLine(LOG_IDENT, "Showing unsupported warning");

            new BloxshadeDialog().ShowDialog();
            App.SoftTerminate();
        }
    }
}
