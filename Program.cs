using Serilog;
using System.Diagnostics;
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
		static ProcessWatchdogConfig _config { get; set; }

		public static string AppDataFolder { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "danzl.org", "ProcessWatchdog");

		public static bool LoadConfig()
		{
			// search for the config in our app data folder
			string configPath = Path.Combine(AppDataFolder, "ProcessWatchdog.config.json"); 
			if (!File.Exists(configPath))
			{
				// search for the ProcessWatchdog.config in the current directory
				configPath = Path.Combine(Directory.GetCurrentDirectory(), "ProcessWatchdog.config.json");
			}
			if (File.Exists(configPath))
			{
				string json = File.ReadAllText(configPath);
				_config = System.Text.Json.JsonSerializer.Deserialize<ProcessWatchdogConfig>(json);

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
						sb.Append("  defaultconfig                Writes the empty default config to stdout\n");
						sb.Append("The config file is named ProcessWatchdog.config.json and must be in the working directory set for the ProcessWatchdog.\n");
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
						Console.WriteLine(JsonSerializer.Serialize<ProcessWatchdogConfig>(new ProcessWatchdogConfig()
						{
							processes = new ProcessWatchdogConfig.ProcessWatchdogInfo[]
							{
								new ProcessWatchdogConfig.ProcessWatchdogInfo()
								{
									workingDirectory = ".",
									executablePath = "",
									arguments = ""
								}
							}
						}, new JsonSerializerOptions { WriteIndented = true }));
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
