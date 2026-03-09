using System.IO;

namespace Lines;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0 && (args[0] == "-h" || args[0] == "--help"))
        {
            Log.Info("Usage: lines [path]");
            Log.Info("Counts lines in a file or recursively in a directory.");
            return 0;
        }

        var targetPath = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        if (File.Exists(targetPath))
        {
            var lines = CountFileLines(targetPath);
            Log.Info($"{targetPath}: {lines}");
            return 0;
        }

        if (!Directory.Exists(targetPath))
        {
            Log.Error($"Path not found: {targetPath}");
            return 1;
        }

        long totalLines = 0;
        long totalFiles = 0;

        foreach (var filePath in Directory.EnumerateFiles(targetPath, "*", SearchOption.AllDirectories))
        {
            try
            {
                totalLines += CountFileLines(filePath);
                totalFiles++;
            }
            catch (IOException)
            {
                // ignore unreadable files
            }
            catch (UnauthorizedAccessException)
            {
                // ignore inaccessible files
            }
        }

        Log.Info($"Path: {targetPath}");
        Log.Info($"Files: {totalFiles}");
        Log.Info($"Lines: {totalLines}");

        return 0;
    }

    private static int CountFileLines(string filePath) => File.ReadLines(filePath).Count();
}
