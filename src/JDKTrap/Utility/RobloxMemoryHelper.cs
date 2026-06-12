using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using JDKTrap;

public static class RobloxMemoryCleaner
{
    #region Native

    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("kernel32.dll")]
    private static extern bool SetProcessWorkingSetSize(
        IntPtr hProcess,
        IntPtr dwMinimumWorkingSetSize,
        IntPtr dwMaximumWorkingSetSize);

    [DllImport("kernel32.dll")]
    private static extern bool SetPriorityClass(
        IntPtr hProcess,
        uint dwPriorityClass);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(
        IntPtr ProcessHandle,
        uint DesiredAccess,
        out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool LookupPrivilegeValue(
        string lpSystemName,
        string lpName,
        out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(
        IntPtr TokenHandle,
        bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState,
        uint BufferLength,
        IntPtr PreviousState,
        IntPtr ReturnLength);

    private const uint TOKEN_ADJUST_PRIVILEGES = 0x20;
    private const uint TOKEN_QUERY = 0x8;
    private const uint SE_PRIVILEGE_ENABLED = 0x2;

    private const uint BELOW_NORMAL_PRIORITY_CLASS = 0x00004000;

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    #endregion

    private static readonly string[] RobloxProcesses =
    {
        "RobloxPlayerBeta",
        "RobloxPlayer",
        "Roblox",
        "RobloxStudioBeta"
    };

    public static void EnableDebugPrivilege()
    {
        if (!OpenProcessToken(Process.GetCurrentProcess().Handle,
            TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY,
            out IntPtr token))
            return;

        try
        {
            LookupPrivilegeValue(null, "SeDebugPrivilege", out LUID luid);

            TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SE_PRIVILEGE_ENABLED
            };

            AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            CloseHandle(token);
        }
    }

    public static void CleanAllRobloxMemory()
    {
        EnableDebugPrivilege();

        var processList = new List<Process>();
        foreach (var name in RobloxProcesses)
        {
            try
            {
                var procs = Process.GetProcessesByName(name);
                if (procs != null)
                {
                    processList.AddRange(procs);
                }
            }
            catch { }
        }

        if (processList.Count == 0)
        {
            return;
        }

        foreach (var proc in processList)
        {
            try
            {
                long before = proc.WorkingSet64;

                EmptyWorkingSet(proc.Handle);
                SetProcessWorkingSetSize(proc.Handle, (IntPtr)(-1), (IntPtr)(-1));

                proc.Refresh();
                long after = proc.WorkingSet64;

                Console.WriteLine(
                    $"[{proc.ProcessName}:{proc.Id}] " +
                    $"{FormatBytes(before)} → {FormatBytes(after)}");
            }
            catch { }
            finally
            {
                proc.Dispose();
            }
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1048576) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1073741824) return $"{bytes / 1048576.0:F1} MB";
        return $"{bytes / 1073741824.0:F2} GB";
    }
}