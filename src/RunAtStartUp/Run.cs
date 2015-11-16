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
  public class Run
  {
    const string Path = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

    static readonly string sOutputAssemly = "program.exe";
    static readonly RunCleanup sCleanup = new RunCleanup();

    private readonly string mAppName;

    public Run(string appName)
    {
      mAppName = appName;
    }

    private bool Update(string path, bool hasArguments, string arguments, bool add)
    {
      var info = new ProcessStartInfo(sOutputAssemly);
      info.Arguments = CreateArguments(mAppName, add, path, hasArguments, arguments);
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
      using (var key = Registry.CurrentUser.OpenSubKey(Path, false))
      {
        return key.GetValue(mAppName) != null;
      }
    }

    public Task<bool> Remove(string path)
    {
      return Task.Run(() => Update(path, false, "", false));
    }

    public Task<bool> Write(string path, string arguments)
    {
      return Task.Run(() => Update(path, true, arguments, true));
    }

    static Run()
    {
      var code = Resources.Program;
      var provider = new CSharpCodeProvider();

      var parameters = new CompilerParameters();
      parameters.GenerateInMemory = false;
      parameters.GenerateExecutable = true;
      parameters.OutputAssembly = sOutputAssemly;

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
    }

    static void RemoveOutputAssembly()
    {
      File.Delete(sOutputAssemly);
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
