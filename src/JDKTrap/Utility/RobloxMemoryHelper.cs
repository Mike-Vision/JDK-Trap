using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using JDKTrap;

public static class RobloxMemoryCleaner
{
    #region Native

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
        // Tránh gọi EmptyWorkingSet và SetProcessWorkingSetSize để không bị đẩy dữ liệu RAM ra pagefile gây stuttering cho game Roblox.
        // Trình quản lý bộ nhớ của Windows sẽ tự động điều phối RAM tối ưu hơn.
    }

    #region Standby List Cleanup

    [DllImport("ntdll.dll")]
    private static extern int NtSetSystemInformation(int infoClass, ref int info, int length);

    private const int SystemMemoryListInformation = 80;
    private const int MemoryPurgeStandbyList = 4;

    /// <summary>
    /// Giải phóng standby list hệ thống 1 lần trước khi khởi chạy Roblox.
    /// Standby list chứa các trang bộ nhớ đã cache nhưng không còn cần thiết.
    /// Giải phóng chúng cho phép Roblox sử dụng RAM vật lý ngay lập tức.
    /// CHỈ gọi 1 lần duy nhất — gọi lặp lại sẽ phản tác dụng.
    /// </summary>
    public static void FreeSystemStandbyList()
    {
        try
        {
            int command = MemoryPurgeStandbyList;
            int result = NtSetSystemInformation(SystemMemoryListInformation, ref command, sizeof(int));
            if (result == 0)
            {
                App.Logger.WriteLine("MemoryCleaner", "System standby list cleared successfully.");
            }
            else
            {
                // Lỗi thường gặp: STATUS_PRIVILEGE_NOT_HELD (0xC0000061) — cần quyền Admin
                App.Logger.WriteLine("MemoryCleaner",
                    $"Failed to clear standby list (NTSTATUS: 0x{result:X8}). Admin rights may be required.");
            }
        }
        catch (Exception ex)
        {
            App.Logger.WriteLine("MemoryCleaner", $"FreeSystemStandbyList error: {ex.Message}");
        }
    }

    #endregion
}