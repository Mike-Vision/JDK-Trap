using System;
using System.IO;
using System.Text.Json;
using JDKTrap;

public static class JDKTrapRobloxSettingsManager // lowk didnt know what tf to name this file
{
    public class JDKTrapRobloxSettings
    {
        public int MemoryCleanerIntervalSeconds { get; set; }
    }

    private static readonly string FolderPath = Paths.Base;

    private static readonly string FilePath =
        Path.Combine(FolderPath, "JDKTrapRobloxSaves.json");

    public static JDKTrapRobloxSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new JDKTrapRobloxSettings();

            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<JDKTrapRobloxSettings>(json)
                   ?? new JDKTrapRobloxSettings();
        }
        catch
        {
            return new JDKTrapRobloxSettings();
        }
    }

    public static void Save(JDKTrapRobloxSettings settings)
    {
        try
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(FilePath, json);
        }
        catch
        {
        }
    }
}
