// Decompiled with JetBrains decompiler
// Type: Lines.Log
// Assembly: Lines, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 34AF9676-75A1-4205-923E-71BA08BAF731
// Assembly location: E:\tools\Lines.exe

using System;
using System.Diagnostics;
using System.Text;

#nullable disable
namespace Lines
{
  internal class Log
  {
    private static string _logFile = (string) null;
    private static bool _logTime = true;
    private static bool _logMessageType = true;
    private static bool _logRelativeTime = false;
    private static DateTime _startTime = DateTime.Now;
    private static bool _verbose = true;
    private static bool _consoleOutput = true;
    private static bool _htmlOutput = true;

    public static bool LogTime
    {
      get => Log._logTime;
      set => Log._logTime = value;
    }

    public static bool LogMessageType
    {
      get => Log._logMessageType;
      set => Log._logMessageType = value;
    }

    public static bool LogRelativeTime
    {
      get => Log._logRelativeTime;
      set => Log._logRelativeTime = value;
    }

    public static bool Verbose
    {
      get => Log._verbose;
      set => Log._verbose = value;
    }

    public static bool ConsoleOutput
    {
      get => Log._consoleOutput;
      set => Log._consoleOutput = value;
    }

    public static bool HtmlOutput
    {
      get => Log._htmlOutput;
      set => Log._htmlOutput = value;
    }

    public static void Error(string msg, params object[] args)
    {
      msg = string.Format(msg, args);
      if (Log._consoleOutput)
        Console.ForegroundColor = ConsoleColor.Red;
      Log.Write("[E]", msg);
      if (!Log._consoleOutput)
        return;
      Console.ResetColor();
    }

    public static void Warn(string msg, params object[] args)
    {
      msg = string.Format(msg, args);
      if (Log._consoleOutput)
        Console.ForegroundColor = ConsoleColor.Yellow;
      Log.Write("[W]", msg);
      if (!Log._consoleOutput)
        return;
      Console.ResetColor();
    }

    public static void Info(string msg, params object[] args)
    {
      msg = string.Format(msg, args);
      if (Log._consoleOutput)
        Console.ForegroundColor = ConsoleColor.White;
      Log.Write("[I]", msg);
      if (!Log._consoleOutput)
        return;
      Console.ResetColor();
    }

    public static void Debug(string msg, params object[] args)
    {
      if (!Log._verbose)
        return;
      msg = string.Format(msg, args);
      if (Log._consoleOutput)
        Console.ForegroundColor = ConsoleColor.DarkGray;
      Log.Write("[D]", msg);
      if (!Log._consoleOutput)
        return;
      Console.ResetColor();
    }

    private static void Write(string msgType, string msg)
    {
      StringBuilder stringBuilder = new StringBuilder();
      if (Log.LogMessageType)
      {
        stringBuilder.Append(msgType);
        stringBuilder.Append(" ");
      }
      if (Log.LogTime)
      {
        if (!Log.LogRelativeTime)
        {
          stringBuilder.Append(DateTime.Now.ToString("HH:mm:ss.ff"));
        }
        else
        {
          TimeSpan timeSpan = DateTime.Now.Subtract(Log._startTime);
          stringBuilder.Append(string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", (object) timeSpan.Hours, (object) timeSpan.Minutes, (object) timeSpan.Seconds, (object) timeSpan.Milliseconds));
        }
        stringBuilder.Append(" ");
      }
      stringBuilder.Append(msg);
      if (Log._consoleOutput)
      {
        if (Log._htmlOutput)
          Console.WriteLine("<br/>" + stringBuilder.ToString());
        else
          Console.WriteLine(stringBuilder.ToString());
      }
      if (Log._htmlOutput)
        Trace.WriteLine("<br/>" + stringBuilder.ToString());
      else
        Trace.WriteLine(stringBuilder.ToString());
    }

    public static void SetLogFile(string logFile)
    {
      Log._logFile = logFile;
      Trace.Listeners.Clear();
      if (string.IsNullOrEmpty(Log._logFile))
        return;
      Trace.Listeners.Add((TraceListener) new TextWriterTraceListener(Log._logFile));
    }

    public static void Close()
    {
      foreach (TraceListener listener in Trace.Listeners)
        listener.Flush();
    }
  }
}
