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
        static Config Config { get; set; }

        static void Main()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    CreateEmptyConfig();
                    Console.WriteLine("The default config successfully created. Close the helper and populate the config.");
                }
                else
                {
                    LoadConfig();
                    ProcessConfig();
                    Console.WriteLine("Execute startup shell commands...");
                    ExecuteStartupShellCommands();
                    Console.WriteLine("Copy observable files...");
                    CopyObservableFilesOnStartup();
                    StartWatching();
                    Console.WriteLine($"Start watching");
                    RunServerProcess();
                    Console.WriteLine("Server process started successfully!");
                    Console.WriteLine("Waiting for changes...\n");

                    AddCurrentProcessExitHandler();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Thread.Sleep(-1);
        }

        static void CreateEmptyConfig()
        {
            ConfigRule configRule = new ConfigRule
            {
                Filter = new string[0],
                Exclude = new string[0],
                Observable = "",
                Dest = ""
            };

            Config config = new Config
            {
                RageServer = "",
                Shell = new ConfigShell { Startup = new string[0] },
                Rules = new ConfigRule[] { configRule }
            };

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
            foreach (var rule in Config.Rules)
            {
                for (int i = 0; i < rule.Exclude.Length; i++)
                {
                    rule.Exclude[i] = Path.GetFullPath(rule.Exclude[i], rule.Observable);
                }
            }
        }

        static void ExecuteStartupShellCommands()
        {
            foreach (var cmd in Config.Shell.Startup)
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        WindowStyle = ProcessWindowStyle.Minimized,
                        CreateNoWindow = true,
                        UseShellExecute = true,
                        Arguments = $"/c {cmd}"
                    }
                };

                process.Start();
            }
        }

        static void CopyObservableFilesOnStartup()
        {
            foreach (var rule in Config.Rules)
            {
                Utils.ClearDirectory(rule.Dest);
                Utils.CopyFiles(rule.Observable, rule.Dest, rule.Filter, rule.Exclude);
            }
        }

        static void StartWatching()
        {
            CreateWatchers();
        }

        static void CreateWatchers()
        {
            foreach (var rule in Config.Rules)
            {
                new FileWatcher(rule.Observable,
                                () => OnChangeFiles(rule.Observable, rule.Dest, rule.Filter, rule.Exclude),
                                rule.Filter,
                                rule.Exclude);
            }
        }

        static void OnChangeFiles(string observable, string dest, string[] filter, string[] exclude)
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now:H:mm:ss}]");
                Console.WriteLine($"Changed files in: {observable}");

                KillServerProcess();

                Console.WriteLine($"Copy files into: {dest}");

                Utils.ClearDirectory(dest);
                Utils.CopyFiles(observable, dest, filter, exclude);

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
                process.WaitForExit();
            }
        }

        static void AddCurrentProcessExitHandler()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => KillServerProcess();
        }
    }
}
