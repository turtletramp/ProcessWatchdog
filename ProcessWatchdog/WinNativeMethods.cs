using System.Runtime.InteropServices;

namespace org.danzl.ProcessWatchdog
{
	public static class WinNativeMethods
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
	}
}

