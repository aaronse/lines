using System.Diagnostics;
using System.Text;

namespace Lines;

internal sealed class Program
{
    private const string DefaultIncludeFiles =
        // Languages
        "*.cs;*.java;*.py;*.rs;" +
        "*.c;*.cpp;*.h;*.hpp;" +
        // Web and UI
        "*.js;*.ts;*.tsx;*.css;" +
        "*.asax;*.asmx;*.aspx;*.ascx;*.master;*.skin;" +
        // Data and configuration
        "*.sql;*.sqml;*.xml;Descriptor.xml;*.config;*.yml;*.xsd;*.xslt;" +
        // Build, tooling, and scripts
        "*.wxs;*.ps1;*.sh";

    private readonly string _usage =
        "Line Count\r\n\r\n" +
        "-d <dir>\r\n" +
        "    Directory to search\r\n" +
        "-i,-include <expr>\r\n" +
        "    ; delimited file wildcard expression(s) of files to include,\r\n" +
        "    defaults to... " + DefaultIncludeFiles + "\r\n" +
        "-e,-exclude <expr>\r\n" +
        "    ; delimited expression(s) used to exclude files.\r\n" +
        "    NOTE: * wildcard characters not supported.\r\n" +
        "-x,-excludehard <expr>\r\n" +
        "    ; delimited expression(s) used to hard skip files/folders.\r\n" +
        "    NOTE: * wildcard characters not supported.\r\n" +
        "-nt\r\n" +
        "    No Totals are displayed\r\n" +
        "-max <max byte size>\r\n" +
        "    Parse files up to byte size specified\r\n" +
        "-html\r\n" +
        "    HTML output\r\n" +
        "-sdc\r\n" +
        "    Include SD changes\r\n" +
        "-v\r\n" +
        "    Verbose output\r\n" +
        "-vt\r\n" +
        "    Verbose output grouped by type\r\n" +
        "-min-date <date string>\r\n" +
        "    Exclude files older than specified date\r\n" +
        "-max-date\r\n" +
        "    Exclude files newer than specified date\r\n\r\n";

    private readonly Dictionary<string, string> _args = new(StringComparer.OrdinalIgnoreCase);
    private StringBuilder? _commandOutput;

    private static int Main(string[] args)
    {
        var program = new Program();
        try
        {
            program.Run(args);
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error("Unhandled Error, ex={0}", ex);
            return 1;
        }
    }

    private void ParseArgs(string[] args)
    {
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index].Length > 0 && args[index][0] == '-' && index + 1 < args.Length && (args[index + 1].Length == 0 || args[index + 1][0] != '-'))
            {
                _args[args[index++]] = args[index];
            }
            else
            {
                _args[args[index]] = string.Empty;
            }
        }

        if (_args.ContainsKey("-h") || _args.ContainsKey("-?") || _args.ContainsKey("/?"))
        {
            Log.Warn(_usage);
            Environment.Exit(0);
        }
    }

    private string GetArgValue(string name, string defaultValue)
    {
        return _args.TryGetValue(name, out var value) ? value : defaultValue;
    }

    private string GetArgValue(string[] names, string defaultValue)
    {
        foreach (var name in names)
        {
            if (_args.TryGetValue(name, out var value))
            {
                return value;
            }
        }

        return defaultValue;
    }

    private DateTime GetArgValueDateTime(string name, DateTime defaultValue)
    {
        return _args.TryGetValue(name, out var value) ? DateTime.Parse(value) : defaultValue;
    }

    private void Run(string[] args)
    {
        Log.LogTime = false;
        Log.LogMessageType = false;

        ParseArgs(args);

        var lineTerminator = "\n";
        var searchDirectories = GetArgValue(new[] { "-d", "-dir" }, Environment.CurrentDirectory)
            .Split(';', StringSplitOptions.RemoveEmptyEntries);
        var includePatterns = GetArgValue(new[] { "-i", "-include" }, DefaultIncludeFiles)
            .Split(';', StringSplitOptions.RemoveEmptyEntries);
        var excludeFilters = GetArgValue(new[] { "-e", "-exclude" }, string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries);
        var hardExcludeFilters = GetArgValue(new[] { "-x", "-excludehard" }, string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries);

        var maxByteSize = long.Parse(GetArgValue("-max", "1048576"));
        var verbose = _args.ContainsKey("-v");
        var verboseByType = _args.ContainsKey("-vt");
        Log.HtmlOutput = _args.ContainsKey("-html");
        var includeSdChanges = _args.ContainsKey("-sdc");
        var minDate = GetArgValueDateTime("-min-date", DateTime.MinValue);
        var maxDate = GetArgValueDateTime("-max-date", DateTime.MaxValue);

        if (!_args.ContainsKey("-nt") && verbose)
        {
            Log.Info("Searching from {0} for {1}", string.Join(";", searchDirectories), string.Join(";", includePatterns));
        }

        if (verbose)
        {
            Log.Info("{0,10} {1,10}    {2}", "Lines", "Size", "Path");
        }

        long totalIncludedLines = 0;
        long totalIncludedFiles = 0;
        long totalIncludedSize = 0;
        long totalExcludedFiles = 0;
        long totalExcludedLines = 0;
        long totalExcludedSize = 0;

        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var typeSummaries = new SortedDictionary<string, FileTypeSummary>(StringComparer.OrdinalIgnoreCase);

        foreach (var searchDirectory in searchDirectories)
        {
            foreach (var includePattern in includePatterns)
            {
                if (!Directory.Exists(searchDirectory))
                {
                    Log.Error("Directory not found: {0}", searchDirectory);
                    continue;
                }

                foreach (var file in EnumerateFiles(searchDirectory, includePattern, hardExcludeFilters))
                {
                    if (!seenFiles.Add(file))
                    {
                        continue;
                    }

                    var excluded = false;
                    foreach (var filter in excludeFilters)
                    {
                        var normalizedFilter = filter.Replace("*", string.Empty, StringComparison.Ordinal);
                        if (file.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            excluded = true;
                            break;
                        }
                    }

                    var fileInfo = new FileInfo(file);
                    var fileSize = fileInfo.Length;
                    var countInFile = 0;
                    var fileTooLarge = fileSize > maxByteSize && maxByteSize > 0;

                    if (!fileTooLarge)
                    {
                        var fileContent = File.ReadAllText(file);
                        var index = 0;
                        while (index + 1 <= fileContent.Length)
                        {
                            index = fileContent.IndexOf(lineTerminator, index + 1, StringComparison.Ordinal);
                            if (index == -1)
                            {
                                break;
                            }

                            countInFile++;
                        }
                    }

                    if (minDate != DateTime.MinValue && fileInfo.LastWriteTime < minDate)
                    {
                        excluded = true;
                    }

                    if (maxDate != DateTime.MaxValue && fileInfo.LastWriteTime > maxDate)
                    {
                        excluded = true;
                    }

                    if (fileTooLarge)
                    {
                        if (verbose)
                        {
                            Log.Debug("-{0,9} {1,10}    {2}", "too big", fileSize, file);
                        }

                        totalExcludedSize += fileSize;
                        totalExcludedFiles++;
                    }
                    else if (excluded)
                    {
                        if (verbose)
                        {
                            Log.Debug("-{0,9} {1,10}    {2}", countInFile, fileSize, file);
                        }

                        totalExcludedLines += countInFile;
                        totalExcludedSize += fileSize;
                        totalExcludedFiles++;
                    }
                    else
                    {
                        if (verbose)
                        {
                            Log.Info("{0,10} {1,10}    {2}", countInFile, fileSize, file);
                        }

                        totalIncludedLines += countInFile;
                        totalIncludedSize += fileSize;
                        totalIncludedFiles++;
                        UpdateFileTypeSummary(typeSummaries, file, countInFile, fileSize);
                    }
                }
            }

            if (includeSdChanges)
            {
                GetSdChanges(searchDirectory);
            }
        }

        if (verboseByType)
        {
            foreach (var key in typeSummaries.Keys)
            {
                var summary = typeSummaries[key];
                Log.Info("{0,10} {1,10}    {2}", summary.CountInFile, summary.FileSize, key);
                foreach (var file in summary.Files)
                {
                    Log.Info("{0,10} {1,10}    {2}", file.CountInFile, file.FileSize, file.FilePath);
                }
            }

            Log.Info(string.Empty);
            foreach (var key in typeSummaries.Keys)
            {
                var summary = typeSummaries[key];
                Log.Info("{0,10} {1,10}    {2}", summary.CountInFile, summary.FileSize, key);
            }
        }

        if (_args.ContainsKey("-nt"))
        {
            return;
        }

        Log.Info("{0} lines, {1}K, {2} files @ {3}", totalIncludedLines, totalIncludedSize / 1024L, totalIncludedFiles, Path.Combine(string.Join(";", searchDirectories), string.Join(";", includePatterns)));

        if (verbose)
        {
            Log.Debug("Excluded file(s) {0}", totalExcludedFiles);
            Log.Debug("{0,10} {1,10}    {2} exclude expr {3}", totalExcludedLines, totalExcludedSize, string.Join(";", searchDirectories), string.Join(";", excludeFilters));
        }
    }

    private static void UpdateFileTypeSummary(SortedDictionary<string, FileTypeSummary> typeSummaries, string filePath, int countInFile, long fileSize)
    {
        var extension = Path.GetExtension(filePath);
        if (!typeSummaries.ContainsKey(extension))
        {
            typeSummaries[extension] = new FileTypeSummary();
        }

        typeSummaries[extension].AddFile(filePath, countInFile, fileSize);
    }

    private static IEnumerable<string> EnumerateFiles(string searchDirectory, string includePattern, string[] hardExcludeFilters)
    {
        var normalizedFilters = hardExcludeFilters
            .Select(static filter => filter.Replace("*", string.Empty, StringComparison.Ordinal))
            .Where(static filter => !string.IsNullOrWhiteSpace(filter))
            .ToArray();

        if (normalizedFilters.Length == 0)
        {
            foreach (var file in Directory.GetFiles(searchDirectory, includePattern, SearchOption.AllDirectories))
            {
                yield return file;
            }

            yield break;
        }

        var pending = new Stack<string>();
        pending.Push(searchDirectory);

        while (pending.Count > 0)
        {
            var currentDirectory = pending.Pop();
            if (PathMatchesAnyFilter(currentDirectory, normalizedFilters))
            {
                continue;
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(currentDirectory, includePattern, SearchOption.TopDirectoryOnly);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var file in files)
            {
                if (PathMatchesAnyFilter(file, normalizedFilters))
                {
                    continue;
                }

                yield return file;
            }

            IEnumerable<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(currentDirectory, "*", SearchOption.TopDirectoryOnly);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var directory in directories)
            {
                if (PathMatchesAnyFilter(directory, normalizedFilters))
                {
                    continue;
                }

                pending.Push(directory);
            }
        }
    }

    private static bool PathMatchesAnyFilter(string path, IEnumerable<string> filters)
    {
        foreach (var filter in filters)
        {
            if (path.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void GetSdChanges(string baseDir)
    {
        var output = new StringBuilder();
        ExecuteCommand("sd", $"changes -s submitted {baseDir}\\...", output);
        Log.Info(output.ToString());
    }

    private int ExecuteCommand(string commandPath, string arguments, StringBuilder output)
    {
        using var process = new Process();
        var startInfo = process.StartInfo;
        startInfo.FileName = commandPath;
        startInfo.Arguments = arguments;
        startInfo.UseShellExecute = false;
        startInfo.WorkingDirectory = Environment.CurrentDirectory;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.StandardOutputEncoding = Encoding.UTF8;
        startInfo.StandardErrorEncoding = Encoding.UTF8;

        _commandOutput = output;
        process.OutputDataReceived += CommandOutputHandler;
        process.ErrorDataReceived += CommandErrorHandler;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return process.ExitCode;
    }

    private void CommandOutputHandler(object sender, DataReceivedEventArgs args)
    {
        if (string.IsNullOrEmpty(args.Data))
        {
            return;
        }

        _commandOutput?.Append("[O]" + args.Data.Replace("\0", string.Empty, StringComparison.Ordinal) + "\n");
    }

    private void CommandErrorHandler(object sender, DataReceivedEventArgs args)
    {
        if (string.IsNullOrEmpty(args.Data))
        {
            return;
        }

        _commandOutput?.Append("[E]" + args.Data.Replace("\0", string.Empty, StringComparison.Ordinal) + "\n");
    }

    private sealed class FileTypeSummary
    {
        internal List<FileLineInfo> Files { get; } = [];

        public int CountInFile { get; private set; }

        public long FileSize { get; private set; }

        internal void AddFile(string filePath, int countInFile, long fileSize)
        {
            CountInFile += countInFile;
            FileSize += fileSize;
            Files.Add(new FileLineInfo(filePath, countInFile, fileSize));
        }
    }

    private sealed class FileLineInfo
    {
        public FileLineInfo(string filePath, int countInFile, long fileSize)
        {
            FilePath = filePath;
            CountInFile = countInFile;
            FileSize = fileSize;
        }

        public string FilePath { get; }

        public int CountInFile { get; }

        public long FileSize { get; }
    }
}
