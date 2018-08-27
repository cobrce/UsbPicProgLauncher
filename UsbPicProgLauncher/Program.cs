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
using System.Text;

namespace UsbPicProgLauncher
{
	static class Program
	{
		internal static Dictionary<string, string> profiles;


		[STAThread]
		static void Main()
		{
			MessageBoxTimeout(IntPtr.Zero, "UsbPicProgLauncher by COB\r\r...Running in background", "", 0x40, 0, 2000);
			if (ReadProfiles())
			{
				try
				{
					StartWatcher(GenerateQuery());

					while (true)
						Thread.Sleep(1000);
				}
				catch (Exception e)
				{
					MessageBox(e.Message);
				}
			}
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

		private static bool ReadProfiles()
		{
			string profilesFile = Path.Combine(
				Path.GetDirectoryName(typeof(Program).Assembly.Location),
				"profiles.json"
				);

			try
			{
				profiles = JsonConvert.DeserializeObject<Dictionary<string, string>>(GetJsonFileContentAsStr(profilesFile));
				return true;
			}
			catch (UnauthorizedAccessException)
			{
				MessageBox($"{profilesFile} not found and could not create it");
			}
			catch (IOException)
			{
				MessageBox($"Can't read {profilesFile}");
			}
			catch
			{
				MessageBox($"Error parsing {profilesFile}");
			}

			return false;
		}

		private static string GetJsonFileContentAsStr(string profilesFile)
		{
			if (File.Exists(profilesFile))
				using (var file = File.OpenText(profilesFile))
				{
					return file.ReadToEnd();
				}
			else
			{
				using (var file = File.CreateText(profilesFile))
				{
					file.Write(Properties.Resources.profiles);
					return Properties.Resources.profiles;
				}
			}
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

		private static void MessageBox(string text)
		{
			System.Windows.Forms.MessageBox.Show(text, "UsbPicProgLauncher");
		}
	}
}
