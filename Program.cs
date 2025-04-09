using System.Diagnostics;
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
					Console.WriteLine($"workingDirectory '{workingDirectory}' does not exist");
					return false;
				}
				if (string.IsNullOrEmpty(executablePath))
				{
					Console.WriteLine("executable is not set");
					return false;
				}
				if (!File.Exists(executablePath))
				{
					Console.WriteLine($"Executable '{executablePath}' does not exist");
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
				Console.WriteLine("No processes defined in config");
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

		public static bool LoadConfig()
		{
			// search for the ProcessWatchdog.config in the current directory
			string configPath = Path.Combine(Directory.GetCurrentDirectory(), "ProcessWatchdog.config.json");
			if (File.Exists(configPath))
			{
				string json = File.ReadAllText(configPath);
				_config = System.Text.Json.JsonSerializer.Deserialize<ProcessWatchdogConfig>(json);

				if (_config.IsValid())
				{
					Console.WriteLine("Config loaded successfully from " + configPath);
					return true;
				}
				else
				{
					Console.WriteLine("Config is not valid");
					return false;
				}
			}
			else
			{
				Console.WriteLine("ProcessWatchdog.config not found at '{configPath}'");
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
						Console.WriteLine("Usage: ProcessWatchdog");
						Console.WriteLine("Options:");
						Console.WriteLine("  -h, --help, /?, /help, help  Show this help message");
						Console.WriteLine("  defaultconfig                Writes the empty default config to stdout");
						Console.WriteLine("The config file is named ProcessWatchdog.config.json and must be in the working directory set for the ProcessWatchdog.");
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

			if (!LoadConfig())
			{
				Console.WriteLine("Failed to load config. Exiting...");
				return 1;
			}

			Console.WriteLine("Starting process watchdog...");
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
				Console.WriteLine($"Process '{pi.executablePath}' exited with code {process.ExitCode} --> restarting");
				Launch(pi);
			};
			process.Start();
		}
	}
}
