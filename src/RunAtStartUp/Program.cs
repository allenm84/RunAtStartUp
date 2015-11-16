using System;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;

namespace RunAtStartUp
{
  class Program
  {
    const string Path = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

    static void Main(string[] args)
    {
      bool success = false;
      if (IsAdministrator())
      {
        success = true;

        try
        {
          using (var stream = new MemoryStream(Convert.FromBase64String(args[0])))
          {
            var reader = new BinaryReader(stream);

            var name = reader.ReadString();
            var add = reader.ReadBoolean();
            var path = reader.ReadString();
            var hasArguments = reader.ReadBoolean();
            var arguments = string.Empty;
            if (hasArguments)
            {
              arguments = reader.ReadString();
            }

            using (var key = Registry.CurrentUser.OpenSubKey(Path, false))
            {
              if (add)
              {
                if (!string.IsNullOrWhiteSpace(arguments))
                {
                  key.SetValue(name, string.Format("{0} \"{1}\"", path, arguments));
                }
                else
                {
                  key.SetValue(name, path);
                }
              }
              else
              {
                key.DeleteValue(name, false);
              }
            }
          }
        }
        catch
        {
          success = false;
        }
      }

      Environment.Exit(success ? 1 : 0);
    }

    private static bool IsAdministrator()
    {
      return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
    }  
  }
}
