﻿using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace org.danzl.ProcessWatchdog
{
	public class  ProcessWatchdogConfig
	{
		public class ProcessWatchdogInfo
		{
			public string workingDirectory { get; set; }
			public string executablePath { get; set; }
			public string arguments { get; set; }

			public bool hideShellWindow { get; set; }

			public bool IsValid()
			{
				if (Directory.Exists(workingDirectory) == false)
				{
					Log.Error($"workingDirectory '{workingDirectory}' does not exist");
					return false;
				}
				if (string.IsNullOrEmpty(executablePath))
				{
					Log.Error("executable is not set");
					return false;
				}
				if (!File.Exists(executablePath))
				{
					Log.Error($"Executable '{executablePath}' does not exist");
					return false;
				}
				return true;
			}
		}

		public required ProcessWatchdogInfo[] processes { get; set; }

		public bool IsValid()
		{
			if (processes == null || processes.Length == 0)
			{
				Log.Error("No processes defined in config");
				return false;
			}
			foreach (var process in processes)
			{
				if (!process.IsValid())
				{
					return false;
				}
			}
			return true;
		}
	}

	public class Program
    {
		public static readonly string ProcessWatchdogConfFile = "ProcessWatchdog.config.json";

		static ProcessWatchdogConfig _config { get; set; }

		public static string AppDataFolder { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "danzl.org", "ProcessWatchdog");

		public static string ProcessPath(string path)
		{
			// if it is a relative path, make it absolute
			if (path == null || path == ".")
			{
				path = Directory.GetCurrentDirectory();
			}
			else if (path.StartsWith("."))
			{
				path = path.Substring(1);
				if (path.StartsWith("\\"))
					path = path.Substring(1);
				path = Path.Combine(Directory.GetCurrentDirectory(), path);
			}
			
			// replace {{ProgramFiles}} with the actual ProgramFiles path
			if (path.Contains("{{ProgramFiles}}"))
			{
				path = path.Replace("{{ProgramFiles}}", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			}
			// replace {{ProgramFilesX86}} with the actual ProgramFiles path
			if (path.Contains("{{ProgramFilesX86}}"))
			{
				path = path.Replace("{{ProgramFilesX86}}", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
			}
			// replace {{AppData}} with the actual AppData path
			if (path.Contains("{{AppData}}"))
			{
				path = path.Replace("{{AppData}}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
			}
			// replace {{Home}} with the actual Home path
			if (path.Contains("{{Home}}"))
			{
				path = path.Replace("{{Home}}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
			}

			if (!Path.IsPathRooted(path))
			{
				path = Path.GetFullPath(path);
			}

			return path;
		}
		public static bool LoadConfig()
		{
			// search for the config in our app data folder
			string configPath = Path.Combine(AppDataFolder, ProcessWatchdogConfFile); 
			if (!File.Exists(configPath))
			{
				Log.Information($"Config not found in {configPath} - searching in current directory and executable directory");
				// search for the ProcessWatchdog.config in the current directory
				configPath = Path.Combine(Directory.GetCurrentDirectory(), ProcessWatchdogConfFile);

				if (!File.Exists(configPath))
				{
					Log.Information($"Config not found in {configPath} directory - searching in executable directory");
					// search in the executable directory
					configPath = Path.Combine(Assembly.GetExecutingAssembly().Location, ProcessWatchdogConfFile);
				}
			}
			if (File.Exists(configPath))
			{
				string json = File.ReadAllText(configPath);
				_config = System.Text.Json.JsonSerializer.Deserialize<ProcessWatchdogConfig>(json);

				// check the paths in the config and replace placeholders
				foreach (var process in _config.processes)
				{
					process.workingDirectory = ProcessPath(process.workingDirectory);
					process.executablePath = ProcessPath(process.executablePath);
				}

				if (_config.IsValid())
				{
					Log.Error("Config loaded successfully from " + configPath);
					return true;
				}
				else
				{
					Log.Error("Config is not valid");
					return false;
				}
			}
			else
			{
				Log.Error("ProcessWatchdog.config not found at '{configPath}'");
				return false;
			}
		}

		public static int Main(string[] args)
        {
			if (args.Length > 0)
			{
				switch (args[0])
				{
					case "-h":
					case "--help":
					case "/?":
					case "/help":
					case "help":
						StringBuilder sb = new StringBuilder();
						sb.Append("Usage: ProcessWatchdog\n");
						sb.Append("Options:\n");
						sb.Append("  -h, --help, /?, /help, help  Show this help message\n");
						sb.Append("  defaultconfig                Writes the empty default config to " + ProcessWatchdogConfFile + " in current directory\n");
						sb.Append("The config file is named  " + ProcessWatchdogConfFile + " and must be in the working directory set for the ProcessWatchdog.\n");
						if (Win.IsWindows())
						{
							Win.ShowMessage(sb.ToString(), "ProcessWatchdog Help");
						}
						else
						{
							Console.WriteLine(sb.ToString());
						}
						return 0;
					case "defaultconfig":
						var defaultConf = JsonSerializer.Serialize<ProcessWatchdogConfig>(new ProcessWatchdogConfig()
						{
							processes = new ProcessWatchdogConfig.ProcessWatchdogInfo[]
							{
								new ProcessWatchdogConfig.ProcessWatchdogInfo()
								{
									workingDirectory = ".",
									executablePath = "",
									arguments = "",
									hideShellWindow = false
								}
							}
						}, new JsonSerializerOptions { WriteIndented = true });
						// write it to current directory
						File.WriteAllBytes(ProcessWatchdogConfFile, Encoding.UTF8.GetBytes(defaultConf));
						return 0;
				}
			}



#if DEBUG
			// Debug mode: log to debug output
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Verbose() 
				.WriteTo.Debug()
				.CreateLogger();
#endif
#if RELEASE
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.WriteTo.File(
					Path.Combine(AppDataFolder, "processwatchdog.log"),
					rollingInterval: RollingInterval.Day,
					rollOnFileSizeLimit: true,
					fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
					shared: true)
				.WriteTo.Debug()
				.CreateLogger();
#endif
			if (!LoadConfig())
			{
				if (Win.IsWindows())
				{
					Win.ShowMessage("Failed to load config. See " + AppDataFolder + " for further details. Exiting...", "ERROR");
				}
				Log.Error("Failed to load config. Exiting...");
				return 1;
			}

			Log.Information("Starting process watchdog...");
			foreach (var pi in _config.processes)
			{
				Launch(pi);
			}
			Thread.Sleep(Timeout.Infinite);
			return 0;
		}
		static void Launch(ProcessWatchdogConfig.ProcessWatchdogInfo pi)
		{
			Process process = new Process();
			process.StartInfo.FileName = pi.executablePath;
			process.StartInfo.WorkingDirectory = pi.workingDirectory;
			process.StartInfo.Arguments = pi.arguments;
			if (pi.hideShellWindow)
			{
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = true;
			}
			process.EnableRaisingEvents = true;
			process.Exited += (sender, e) =>
			{
				Log.Warning($"Process '{pi.executablePath}' PID {process.Id} exited with code {process.ExitCode} --> restarting");
				Launch(pi);
			};
			process.Start();
			Log.Information($"Process '{pi.executablePath}' started with PID {process.Id}");
		}
	}
}
