using System;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading;

namespace RageHelper
{
  class Program
  {
    static string Cwd = Directory.GetCurrentDirectory();
    static string ConfigPath = Path.Combine(Cwd, "config.json");
    public static Config Config { get; set; }

    static void Main()
    {
      try
      {
        CreateEmptyConfig();
        LoadConfig();
        ProcessConfig();

        StartWatching();

        Console.WriteLine($"Start watching directories:\n- {Config.Server.From}\n- {Config.Client.From}\n");

        RunServerProcess();

        Console.WriteLine("Server process started successfully!");
        Console.WriteLine("Waiting for changes...\n");

        AddCurrentProcessExitHandler();
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);
      }

      Thread.Sleep(-1);
    }

    static void CreateEmptyConfig()
    {
      if (File.Exists(ConfigPath))
        return;

      ConfigParam configParam = new ConfigParam { Filter = new string[0], Exclude = new string[0], From = "", To = "" };
      Config config = new Config { RageServer = "", Client = configParam, Server = configParam };

      File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }

    static void LoadConfig()
    {
      string json = File.ReadAllText(ConfigPath);
      Config = JsonConvert.DeserializeObject<Config>(json);
    }

    static void ProcessConfig()
    {
      SetFullPathToExcludedFiles();
    }

    static void SetFullPathToExcludedFiles()
    {
      for (int i = 0; i < Config.Server.Exclude.Length; i++)
        Config.Server.Exclude[i] = Path.GetFullPath(Config.Server.Exclude[i], Config.Server.From);

      for (int i = 0; i < Config.Client.Exclude.Length; i++)
        Config.Client.Exclude[i] = Path.GetFullPath(Config.Client.Exclude[i], Config.Client.From);
    }

    static void StartWatching()
    {
      CreateServerWatcher();
      CreateClientWatcher();
    }

    static void CreateServerWatcher()
    {
      new FileWatcher(
                      Config.Server.From,
                      () => OnChangeFiles(Config.Server.From, Config.Server.To, Config.Server.Filter, Config.Server.Exclude),
                      Config.Server.Filter,
                      Config.Server.Exclude);
    }

    static void CreateClientWatcher()
    {
      new FileWatcher(
                      Config.Client.From,
                      () => OnChangeFiles(Config.Client.From, Config.Client.To, Config.Client.Filter, Config.Client.Exclude),
                      Config.Client.Filter,
                      Config.Client.Exclude);
    }

    static void OnChangeFiles(string src, string dest, string[] filter, string[] exclude)
    {
      try
      {
        Console.WriteLine($"[{DateTime.Now.ToString("H:mm:ss")}]");
        Console.WriteLine($"Changed files in: {src}");

        KillServerProcess();

        Console.WriteLine($"Copy files into: {dest}");

        Utils.ClearDirectory(dest);
        Utils.CopyFiles(src, dest, filter, exclude);

        RunServerProcess();

        Console.WriteLine("Server process successfully restarted!");
        Console.WriteLine("Waiting for changes...\n");
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);
      }
    }

    static void RunServerProcess()
    {
      Process process = new Process()
      {
        StartInfo = new ProcessStartInfo()
        {
          FileName = Config.RageServer,
          WindowStyle = ProcessWindowStyle.Minimized,
          CreateNoWindow = true,
          UseShellExecute = true,
          WorkingDirectory = Directory.GetParent(Config.RageServer).ToString()
        }
      };

      process.Start();
    }

    static void KillServerProcess()
    {
      string processName = Path.GetFileNameWithoutExtension(Config.RageServer);
      Process[] processes = Process.GetProcessesByName(processName);

      foreach (Process process in processes)
      {
        process.Kill(true);
      }
    }

    static void AddCurrentProcessExitHandler()
    {
      AppDomain.CurrentDomain.ProcessExit += (s, e) => KillServerProcess();
    }
  }
}
