using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;
using Microsoft.Win32;

namespace RunAtStartUp
{
  public class RunEditor
  {
    const string RegPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

    static readonly RunCleanup sCleanup = new RunCleanup();
    static readonly Lazy<string> sOutputAssembly;

    private readonly string mAppName;
    private readonly string mAppPath;

    public RunEditor(string appName, string appPath)
    {
      mAppName = appName;
      mAppPath = appPath;
    }

    private bool Update(bool hasArguments, string arguments, bool add)
    {
      var info = new ProcessStartInfo(sOutputAssembly.Value);
      info.Arguments = CreateArguments(mAppName, add, mAppPath, hasArguments, arguments);
      info.Verb = "runas";

      using (var process = Process.Start(info))
      {
        process.WaitForExit();
        return (process.ExitCode == 1);
      }
    }

    private static string CreateArguments(string name, bool add, string path, bool hasArguments, string arguments)
    {
      using (var stream = new MemoryStream())
      {
        var writer = new BinaryWriter(stream);
        writer.Write(name);
        writer.Write(add);
        writer.Write(path);
        writer.Write(hasArguments);
        writer.Write(arguments);
        writer.Flush();
        return Convert.ToBase64String(stream.ToArray());
      }
    }

    public bool Read()
    {
      using (var key = Registry.CurrentUser.OpenSubKey(RegPath, false))
      {
        return key.GetValue(mAppName) != null;
      }
    }

    public Task<bool> Remove()
    {
      return Task.Run(() => Update(false, "", false));
    }

    public Task<bool> Write(string arguments)
    {
      return Task.Run(() => Update(true, arguments, true));
    }

    static RunEditor()
    {
      sOutputAssembly = new Lazy<string>(CreateAssembly, true);
    }

    static string CreateAssembly()
    {
      var code = Resources.Program;
      var provider = new CSharpCodeProvider();

      var parameters = new CompilerParameters();
      parameters.GenerateInMemory = false;
      parameters.GenerateExecutable = true;
      parameters.OutputAssembly = "UpdateRegistry.exe";

      var results = provider.CompileAssemblyFromSource(parameters, code);
      if (results.Errors.HasErrors)
      {
        var sb = new StringBuilder();
        foreach (CompilerError error in results.Errors)
        {
          sb.AppendLine(string.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
        }
        throw new InvalidOperationException(sb.ToString());
      }

      return parameters.OutputAssembly;
    }

    static void RemoveOutputAssembly()
    {
      if (!sOutputAssembly.IsValueCreated)
      {
        return;
      }

      File.Delete(sOutputAssembly.Value);
    }

    private class RunCleanup
    {
      ~RunCleanup()
      {
        RemoveOutputAssembly();
      }
    }
  }
}
