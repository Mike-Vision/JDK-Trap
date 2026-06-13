using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JDKTrap.Utility
{
    internal static class Filesystem
    {
        internal static long GetFreeDiskSpace(string path)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                // https://github.com/Bloxstraplabs/Bloxstrap/issues/1648#issuecomment-2192571030
                if (path.ToUpperInvariant().StartsWith(drive.Name.ToUpperInvariant()))
                    return drive.AvailableFreeSpace;
            }

            return -1;
        }

        internal static void AssertReadOnly(string filePath)
        {
            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists || !fileInfo.IsReadOnly)
                return;

            fileInfo.IsReadOnly = false;
            App.Logger.WriteLine("Filesystem::AssertReadOnly", $"The following file was set as read-only: {filePath}");
        }

        internal static void CopyAppFiles(string sourceDir, string destDir)
        {
            if (string.IsNullOrEmpty(sourceDir) || string.IsNullOrEmpty(destDir))
                return;

            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.EnumerateFiles(sourceDir, "*.*"))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".exe" || ext == ".dll" || ext == ".json")
                {
                    string destFile = Path.Combine(destDir, Path.GetFileName(file));
                    try
                    {
                        AssertReadOnly(destFile);
                        File.Copy(file, destFile, true);
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine("Filesystem::CopyAppFiles", $"Failed to copy {file} to {destFile}: {ex.Message}");
                    }
                }
            }
        }

        internal static void AssertReadOnlyDirectory(string directoryPath)
        {
            var directory = new DirectoryInfo(directoryPath);

            if (!directory.Exists)
                return;
            directory.Attributes = FileAttributes.Normal;

            foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                try
                {
                    info.Attributes = FileAttributes.Normal;
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine("Filesystem::AssertReadOnlyDirectory",
                        $"Failed to change attributes for {info.FullName}: {ex.Message}");
                }
            }

            App.Logger.WriteLine("Filesystem::AssertReadOnlyDirectory",
                $"Removed read-only attributes from directory: {directoryPath}");
        }
    }
}
