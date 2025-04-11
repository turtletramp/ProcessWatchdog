namespace org.danzl.ProcessWatchdog
{
    public class Win
    {
        public static bool IsWindows()
		{
			// Check if the current OS is Windows
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		public static void ShowMessage(string message, string title = "Message")
		{
			WinNativeMethods.MessageBox(IntPtr.Zero, message, title, 0); // 0 = OK button only
		}
	}
}
