// Decompiled with JetBrains decompiler
// Type: Lines.Program
// Assembly: Lines, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 34AF9676-75A1-4205-923E-71BA08BAF731
// Assembly location: E:\tools\Lines.exe

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

#nullable disable
namespace Lines
{
  internal class Program
  {
    private const string DefaultIncludeFiles = "*.cs;*.java;*.c;*.cpp;*.h;*.hpp;*.sql;*.sqml;*.wxs;Descriptor.xml;*.asax;*.asmx;*.aspx;*.ascx;*.config;*.skin;*.js;*.css;*.xml;*.master;*.xsd;*.xslt";
    private readonly string _usage = "Line Count\r\n\r\n-d <dir>\r\n    Directory to search\r\n-i,-include <expr>\r\n    ; delimited file wildcard expression(s) of files to include, \r\n    defaults to... *.cs;*.java;*.c;*.cpp;*.h;*.hpp;*.sql;*.sqml;*.wxs;Descriptor.xml;*.asax;*.asmx;*.aspx;*.ascx;*.config;*.skin;*.js;*.css;*.xml;*.master;*.xsd;*.xslt\r\n-e,-exclude <expr>\r\n    ; delimited expression(s) used to exclude files.\r\n    NOTE: * wildcard characters not supported.\r\n-nt\r\n    No Totals are displayed\r\n-max <max byte size>\r\n    Parse files up to byte size specified\r\n-html\r\n    HTML output\r\n-sdc\r\n    Include SD changes\r\n-v\r\n    Verbose output\r\n-vt\r\n    Verbose output grouped by type\r\n-min-date <date string>\r\n    Exclude files older than specified date\r\n-max-date\r\n    Exclude files newer than specified date\r\n\r\n";
    private Dictionary<string, string> _args = new Dictionary<string, string>();
    private StringBuilder _sbCommandOutput = (StringBuilder) null;

    private static void Main(string[] args)
    {
      Program program = new Program();
      try
      {
        program.Run(args);
      }
      catch (Exception ex)
      {
        Log.Error("Unhandled Error, ex={0}", (object) ex);
      }
    }

    private void ParseArgs(string[] args)
    {
      for (int index = 0; index < args.Length; ++index)
      {
        if (args[index][0] == '-' && index + 1 < args.Length && args[index + 1][0] != '-')
          this._args[args[index++]] = args[index];
        else
          this._args[args[index]] = "";
      }
      if (!this._args.ContainsKey("-h") && !this._args.ContainsKey("-?") && !this._args.ContainsKey("/?"))
        return;
      Log.Warn(this._usage);
      Environment.Exit(0);
    }

    private string GetArgValue(string name, string defaultValue)
    {
      return this._args.ContainsKey(name) ? this._args[name] : defaultValue;
    }

    private string GetArgValue(string[] names, string defaultValue)
    {
      foreach (string name in names)
      {
        if (this._args.ContainsKey(name))
          return this._args[name];
      }
      return defaultValue;
    }

    private string GetArgValue(string name)
    {
      if (!this._args.ContainsKey(name))
      {
        string str = "Missing Argument '" + name + "'";
        Log.Error(str);
        throw new MissingFieldException(str);
      }
      return this._args[name];
    }

    private DateTime GetArgValueDateTime(string name, DateTime defaultValue)
    {
      return this._args.ContainsKey(name) ? DateTime.Parse(this._args[name]) : defaultValue;
    }

    private void Run(string[] args)
    {
      Log.LogTime = false;
      Log.LogMessageType = false;
      this.ParseArgs(args);
      string str1 = "\n";
      string[] strArray1 = this.GetArgValue(new string[2]
      {
        "-d",
        "-dir"
      }, Environment.CurrentDirectory).Split(new char[1]
      {
        ';'
      }, StringSplitOptions.RemoveEmptyEntries);
      string[] strArray2 = this.GetArgValue(new string[2]
      {
        "-i",
        "-include"
      }, "*.cs;*.java;*.c;*.cpp;*.h;*.hpp;*.sql;*.sqml;*.wxs;Descriptor.xml;*.asax;*.asmx;*.aspx;*.ascx;*.config;*.skin;*.js;*.css;*.xml;*.master;*.xsd;*.xslt").Split(new char[1]
      {
        ';'
      }, StringSplitOptions.RemoveEmptyEntries);
      string[] strArray3 = this.GetArgValue(new string[2]
      {
        "-e",
        "-exclude"
      }, "").Split(new char[1]{ ';' }, StringSplitOptions.RemoveEmptyEntries);
      long num1 = long.Parse(this.GetArgValue("-max", "1048576"));
      bool flag1 = this._args.ContainsKey("-v");
      bool flag2 = this._args.ContainsKey("-vt");
      Log.HtmlOutput = this._args.ContainsKey("-html");
      bool flag3 = this._args.ContainsKey("-sdc");
      DateTime argValueDateTime1 = this.GetArgValueDateTime("-min-date", DateTime.MinValue);
      DateTime argValueDateTime2 = this.GetArgValueDateTime("-max-date", DateTime.MaxValue);
      if (!this._args.ContainsKey("-nt") & flag1)
        Log.Info("Searching from {0} for {1}", (object) strArray1, (object) string.Join(";", strArray2));
      if (flag1)
        Log.Info("{0,10} {1,10}    {2}", (object) "Lines", (object) "Size", (object) "Path");
      long num2 = 0;
      long num3 = 0;
      long num4 = 0;
      long num5 = 0;
      long num6 = 0;
      long num7 = 0;
      Dictionary<string, int> dictionary = new Dictionary<string, int>();
      List<string> stringList = new List<string>();
      SortedDictionary<string, Program.FileTypeSummary> typeSummaries = new SortedDictionary<string, Program.FileTypeSummary>();
      foreach (string str2 in strArray1)
      {
        foreach (string searchPattern in strArray2)
        {
          foreach (string file in Directory.GetFiles(str2, searchPattern, SearchOption.AllDirectories))
          {
            bool flag4 = false;
            if (!stringList.Contains(file))
            {
              stringList.Add(file);
              foreach (string str3 in strArray3)
              {
                string str4 = str3.Replace("*", "");
                if (-1 != file.IndexOf(str4))
                {
                  flag4 = true;
                  break;
                }
              }
              FileInfo fileInfo = new FileInfo(file);
              long length = fileInfo.Length;
              int countInFile = 0;
              bool flag5 = length > num1 && num1 > 0L;
              if (!flag5)
              {
                string str5 = File.ReadAllText(file);
                int num8 = 0;
                while (true)
                {
                  if (num8 + 1 <= str5.Length)
                  {
                    num8 = str5.IndexOf(str1, num8 + 1);
                    if (-1 != num8)
                      ++countInFile;
                    else
                      break;
                  }
                  else
                    break;
                }
              }
              if (DateTime.MinValue != argValueDateTime1 && fileInfo.LastWriteTime < argValueDateTime1)
                flag4 = true;
              if (DateTime.MaxValue != argValueDateTime2 && fileInfo.LastWriteTime > argValueDateTime2)
                flag4 = true;
              if (flag5)
              {
                if (flag1)
                  Log.Debug("-{0,9} {1,10}    {2}", (object) "too big", (object) length, (object) file);
                num7 += length;
                ++num5;
              }
              else if (flag4)
              {
                if (flag1)
                  Log.Debug("-{0,9} {1,10}    {2}", (object) countInFile, (object) length, (object) file);
                num6 += (long) countInFile;
                num7 += length;
                ++num5;
              }
              else
              {
                if (flag1)
                  Log.Info("{0,10} {1,10}    {2}", (object) countInFile, (object) length, (object) file);
                num2 += (long) countInFile;
                num4 += length;
                ++num3;
                this.UpdateFileTypeSummary(typeSummaries, file, countInFile, length);
              }
            }
          }
        }
        if (flag3)
          this.GetSdChanges(str2);
      }
      if (flag2)
      {
        foreach (string key in typeSummaries.Keys)
        {
          Log.Info("{0,10} {1,10}    {2}", (object) typeSummaries[key].CountInFile, (object) typeSummaries[key].FileSize, (object) key);
          foreach (Program.FileLineInfo file in typeSummaries[key].Files)
            Log.Info("{0,10} {1,10}    {2}", (object) file.CountInFile, (object) file.FileSize, (object) file.FilePath);
        }
        Log.Info("");
        foreach (string key in typeSummaries.Keys)
          Log.Info("{0,10} {1,10}    {2}", (object) typeSummaries[key].CountInFile, (object) typeSummaries[key].FileSize, (object) key);
      }
      if (this._args.ContainsKey("-nt"))
        return;
      Log.Info("{0} lines, {1}K, {2} files @ {3}", (object) num2, (object) (num4 / 1024L), (object) num3, (object) Path.Combine(string.Join(";", strArray1), string.Join(";", strArray2)));
      if (flag1)
      {
        Log.Debug("Excluded file(s) {0}", (object) num5);
        Log.Debug("{0,10} {1,10}    {2} exclude expr {3}", (object) num6, (object) num7, (object) string.Join(";", strArray1), (object) string.Join(";", strArray3));
      }
    }

    private void UpdateFileTypeSummary(
      SortedDictionary<string, Program.FileTypeSummary> typeSummaries,
      string filePath,
      int countInFile,
      long fileSize)
    {
      string extension = Path.GetExtension(filePath);
      if (!typeSummaries.ContainsKey(extension))
        typeSummaries[extension] = new Program.FileTypeSummary();
      typeSummaries[extension].AddFile(filePath, countInFile, fileSize);
    }

    private void GetSdChanges(string baseDir)
    {
      StringBuilder sbOutput = new StringBuilder();
      this.ExecuteCommand("sd", "changes -s submitted " + baseDir + "\\...", sbOutput);
      Log.Info(sbOutput.ToString());
    }

    private int ExecuteCommand(string commandPath, string arguments, StringBuilder sbOutput)
    {
      Process process = new Process();
      try
      {
        ProcessStartInfo startInfo = process.StartInfo;
        startInfo.FileName = commandPath;
        startInfo.Arguments = arguments;
        startInfo.UseShellExecute = false;
        startInfo.WorkingDirectory = Path.GetDirectoryName(commandPath);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.StandardOutputEncoding = Encoding.UTF8;
        startInfo.StandardErrorEncoding = Encoding.UTF8;
        this._sbCommandOutput = sbOutput;
        process.OutputDataReceived += new DataReceivedEventHandler(this.CommandOutputHandler);
        process.ErrorDataReceived += new DataReceivedEventHandler(this.CommandErrorHandler);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return process.ExitCode;
      }
      finally
      {
        process?.Close();
      }
    }

    private void CommandOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
    {
      if (string.IsNullOrEmpty(outLine.Data))
        return;
      this._sbCommandOutput.Append("[O]" + outLine.Data.Replace("\0", "") + "\n");
    }

    private void CommandErrorHandler(object sender, DataReceivedEventArgs e)
    {
      if (string.IsNullOrEmpty(e.Data))
        return;
      this._sbCommandOutput.Append("[E]" + e.Data.Replace("\0", "") + "\n");
    }

    private class FileTypeSummary
    {
      internal List<Program.FileLineInfo> Files = new List<Program.FileLineInfo>();

      public int CountInFile { get; set; }

      public long FileSize { get; set; }

      internal void AddFile(string filePath, int countInFile, long fileSize)
      {
        this.CountInFile += countInFile;
        this.FileSize += fileSize;
        this.Files.Add(new Program.FileLineInfo(filePath, countInFile, fileSize));
      }
    }

    private class FileLineInfo
    {
      public string FilePath { get; set; }

      public int CountInFile { get; set; }

      public long FileSize { get; set; }

      public FileLineInfo(string filePath, int countInFile, long fileSize)
      {
        this.FilePath = filePath;
        this.CountInFile = countInFile;
        this.FileSize = fileSize;
      }
    }
  }
}
