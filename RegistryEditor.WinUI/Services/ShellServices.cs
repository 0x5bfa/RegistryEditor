using System.ComponentModel;
using System.Diagnostics;

namespace RegistryEditor.WinUI.Services
{
	public static class ShellServices
	{
		public static string RunPowershellCommand(bool runAs, string command)
		{
			try
			{
				using Process process = new();
				process.StartInfo.FileName = "powershell.exe";
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				process.StartInfo.Arguments = command;

				if (runAs)
				{
					process.StartInfo.UseShellExecute = true;
					process.StartInfo.Verb = "runas";
				}

				process.Start();

				if (process.WaitForExit(30 * 1000))
				{
					return null;
				}

				return null;
			}
			catch (Win32Exception)
			{
				// If user cancels UAC
				return null;
			}
		}

		public static bool RunCmdPromptCommand(bool runAs, string command)
		{
			try
			{
				using Process process = new();
				process.StartInfo.FileName = "cmd.exe";
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				process.StartInfo.Arguments = command;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;

				if (runAs)
					process.StartInfo.Verb = "RunAs";

				process.Start();

				if (process.WaitForExit(30 * 1000))
					return process.ExitCode == 0;

				return false;
			}
			catch (Win32Exception)
			{
				// If user cancels UAC
				return false;
			}
		}
	}
}
