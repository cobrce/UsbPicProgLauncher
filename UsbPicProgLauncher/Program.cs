using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Management;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

namespace UsbPicProgLauncher
{
	static class Program
	{
		internal static Dictionary<string, string> profiles;

		[STAThread]
		static void Main()
		{
			ReadProfiles();

			MessageBoxTimeout(IntPtr.Zero, "UsbPicProgLauncher by COB\r\r...Running in background", "", 0x40, 0, 2000);

			StartWatcher(GenerateQuery());

			while (true)
				Thread.Sleep(1000);
		}

		[DllImport("user32.dll", SetLastError = true)]
		static extern int MessageBoxTimeout(IntPtr hwnd, String text, String title, uint type, Int16 wLanguageId, Int32 milliseconds);
		[DllImport("User32.dll", SetLastError = true)]
		static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

		private static void StartWatcher(WqlEventQuery query)
		{
			var watcher = new ManagementEventWatcher(query);
			watcher.EventArrived += new EventArrivedEventHandler(WaitForUSBChangeEvent);
			watcher.Start();
		}

		private static WqlEventQuery GenerateQuery()
		{
			WqlEventQuery query = new WqlEventQuery();
			query.EventClassName = "__InstanceCreationEvent";
			query.WithinInterval = new TimeSpan(0, 0, 1);
			query.Condition = @"TargetInstance ISA 'Win32_PnPEntity'";
			return query;
		}

		private static void ReadProfiles()
		{
			string profilesFile = Path.Combine(
				Path.GetDirectoryName(typeof(Program).Assembly.Location),
				"profiles.json"
				);
			try
			{
				using (var file = File.OpenText(profilesFile))
				{
					profiles = JsonConvert.DeserializeObject<Dictionary<string, string>>(file.ReadToEnd());
				}
			}
			catch { MessageBox.Show("Cannot parse setting.json", "UsbPicProgLauncher"); }
		}

		public static void WaitForUSBChangeEvent(object sender, EventArrivedEventArgs e)
		{
			ManagementBaseObject target = (ManagementBaseObject)e.NewEvent["TargetInstance"];

			string name = target.GetPropertyValue("Name").ToString().Trim();

			RunProgramFor(name);
		}

		public static void RunProgramFor(string name)
		{
			foreach (string key in profiles.Keys)
				if (key == name)
				{
					DeviceFound(profiles[key]);
					break;
				}
		}

		private static void DeviceFound(string programAbsPath)
		{
			foreach (Process process in Process.GetProcesses())
				try
				{
					if (process.MainModule.FileName == programAbsPath)
					{
						SwitchToThisWindow(process.MainWindowHandle, false);
						return;
					}
				}
				catch { }

			Process.Start(programAbsPath);
		}
	}
}
