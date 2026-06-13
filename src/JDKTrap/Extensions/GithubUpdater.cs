using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using JDKTrap;

public static class GithubUpdater
{
    private static readonly HttpClient http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "JDKTrap-Updater" } }
    };

    private static async Task<T> ExecuteWithRetry<T>(Func<Task<T>> action)
    {
        int retries = 3;
        int delayMs = 1000;
        for (int i = 0; i < retries; i++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                App.Logger.WriteLine("GitHubUpdater::ExecuteWithRetry", $"Transient error on updater download, attempt {i + 1}: {ex.Message}");
                if (i == retries - 1)
                    throw;

                await Task.Delay(delayMs);
                delayMs *= 2;
            }
        }
        throw new InvalidOperationException("Unreachable");
    }

    public static async Task<string?> GetLatestVersionTagAsync()
    {
        try
        {
            string url = "https://api.github.com/repos/Mike-Vision/JDK-Trap/releases/latest";
            string response = await ExecuteWithRetry(() => http.GetStringAsync(url));
            using var doc = JsonDocument.Parse(response);
            return doc.RootElement.GetProperty("tag_name").GetString();
        }
        catch (Exception ex)
        {
            App.Logger.WriteLine("GitHubUpdater", $"Failed to get latest release tag: {ex}");
            return null;
        }
    }

    public static async Task<bool> DownloadAndInstallUpdate(string tag)
    {
        try
        {
            string url = "https://api.github.com/repos/Mike-Vision/JDK-Trap/releases/latest";
            string response = await ExecuteWithRetry(() => http.GetStringAsync(url));
            using var doc = JsonDocument.Parse(response);
            var assets = doc.RootElement.GetProperty("assets");

            foreach (var asset in assets.EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? "";
                string downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";

                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    return await UpdateExe(downloadUrl, name);

                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    return await UpdateZip(downloadUrl, name);
            }

            App.Logger.WriteLine("GitHubUpdater", "No valid .exe or .zip asset found.");
            return false;
        }
        catch (Exception ex)
        {
            App.Logger.WriteLine("GitHubUpdater", $"Update failed: {ex}");
            return false;
        }
    }

    private static async Task<bool> UpdateExe(string url, string name)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "JDKTrap_Update");
        Directory.CreateDirectory(tempDir);

        string exePath = Path.Combine(tempDir, name);
        var bytes = await ExecuteWithRetry(() => http.GetByteArrayAsync(url));
        await File.WriteAllBytesAsync(exePath, bytes);

        string currentExe = Environment.ProcessPath!;
        string backupExe = currentExe + ".old";
        if (File.Exists(backupExe)) File.Delete(backupExe);
        File.Move(currentExe, backupExe);
        File.Copy(exePath, currentExe, true);

        RestartAfterUpdate(currentExe);
        return true;
    }

    private static async Task<bool> UpdateZip(string url, string name)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "JDKTrap_Update");
        Directory.CreateDirectory(tempDir);

        string zipPath = Path.Combine(tempDir, name);
        var bytes = await ExecuteWithRetry(() => http.GetByteArrayAsync(url));
        await File.WriteAllBytesAsync(zipPath, bytes);

        string extractPath = Path.Combine(tempDir, "Extracted");
        if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
        ZipFile.ExtractToDirectory(zipPath, extractPath, true);

        string currentDir = AppContext.BaseDirectory;
        string currentExe = Environment.ProcessPath!;
        foreach (string file in Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(extractPath, file);
            string dest = Path.Combine(currentDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            if (dest.Equals(currentExe, StringComparison.OrdinalIgnoreCase))
            {
                string backupExe = currentExe + ".old";
                if (File.Exists(backupExe)) File.Delete(backupExe);
                File.Move(currentExe, backupExe);
            }

            File.Copy(file, dest, true);
        }

        string mainExe = Path.Combine(currentDir, "JDKTrap.exe");
        RestartAfterUpdate(mainExe);
        return true;
    }

    private static void RestartAfterUpdate(string exePath)
    {
        Task.Delay(800).ContinueWith(_ =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });
            Environment.Exit(0);
        });
    }
}
