using org.danzl.ProcessWatchdog;

namespace ProcessWatchdogTest
{
	public class ProcessPathTest
	{
		[Fact]
		public void ProcessPath()
		{
			if (Win.IsWindows())
			{
				Assert.Equal("C:\\Program Files\\test", Program.ProcessPath("{{ProgramFiles}}\\test"));
				Assert.Equal("C:\\Program Files (x86)\\test", Program.ProcessPath("{{ProgramFilesX86}}\\test"));
				Assert.Equal(Directory.GetCurrentDirectory() + "\\test", Program.ProcessPath(".\\test"));
				Assert.Equal(Directory.GetCurrentDirectory() + "\\test", Program.ProcessPath("test"));
			}
		}
	}
}