using System.Diagnostics;
using System.Text;

namespace Lines;

internal static class Log
{
    private static string? _logFile;
    private static bool _logTime = true;
    private static bool _logMessageType = true;
    private static bool _logRelativeTime;
    private static readonly DateTime StartTime = DateTime.Now;
    private static bool _verbose = true;
    private static bool _consoleOutput = true;
    private static bool _htmlOutput = true;

    public static bool LogTime
    {
        get => _logTime;
        set => _logTime = value;
    }

    public static bool LogMessageType
    {
        get => _logMessageType;
        set => _logMessageType = value;
    }

    public static bool LogRelativeTime
    {
        get => _logRelativeTime;
        set => _logRelativeTime = value;
    }

    public static bool Verbose
    {
        get => _verbose;
        set => _verbose = value;
    }

    public static bool ConsoleOutput
    {
        get => _consoleOutput;
        set => _consoleOutput = value;
    }

    public static bool HtmlOutput
    {
        get => _htmlOutput;
        set => _htmlOutput = value;
    }

    public static void Error(string msg, params object[] args)
    {
        msg = string.Format(msg, args);
        if (_consoleOutput)
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }

        Write("[E]", msg);

        if (_consoleOutput)
        {
            Console.ResetColor();
        }
    }

    public static void Warn(string msg, params object[] args)
    {
        msg = string.Format(msg, args);
        if (_consoleOutput)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        Write("[W]", msg);

        if (_consoleOutput)
        {
            Console.ResetColor();
        }
    }

    public static void Info(string msg, params object[] args)
    {
        msg = string.Format(msg, args);
        if (_consoleOutput)
        {
            Console.ForegroundColor = ConsoleColor.White;
        }

        Write("[I]", msg);

        if (_consoleOutput)
        {
            Console.ResetColor();
        }
    }

    public static void Debug(string msg, params object[] args)
    {
        if (!_verbose)
        {
            return;
        }

        msg = string.Format(msg, args);
        if (_consoleOutput)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
        }

        Write("[D]", msg);

        if (_consoleOutput)
        {
            Console.ResetColor();
        }
    }

    private static void Write(string msgType, string msg)
    {
        var builder = new StringBuilder();
        if (LogMessageType)
        {
            builder.Append(msgType);
            builder.Append(' ');
        }

        if (LogTime)
        {
            if (!LogRelativeTime)
            {
                builder.Append(DateTime.Now.ToString("HH:mm:ss.ff"));
            }
            else
            {
                var timeSpan = DateTime.Now.Subtract(StartTime);
                builder.Append(string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds));
            }

            builder.Append(' ');
        }

        builder.Append(msg);
        var line = builder.ToString();

        if (_consoleOutput)
        {
            Console.WriteLine(_htmlOutput ? "<br/>" + line : line);
        }

        Trace.WriteLine(_htmlOutput ? "<br/>" + line : line);
    }

    public static void SetLogFile(string logFile)
    {
        _logFile = logFile;
        Trace.Listeners.Clear();
        if (string.IsNullOrEmpty(_logFile))
        {
            return;
        }

        Trace.Listeners.Add(new TextWriterTraceListener(_logFile));
    }

    public static void Close()
    {
        foreach (TraceListener listener in Trace.Listeners)
        {
            listener.Flush();
        }
    }
}
