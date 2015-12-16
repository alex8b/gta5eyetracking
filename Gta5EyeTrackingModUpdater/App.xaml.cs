using System;
using System.Threading;
using System.Windows;

namespace Gta5EyeTrackingModUpdater
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private Mutex _mutex;
		private static string appGuid = "6D1C7E11-4A87-49A5-B38E-B7F4DD02445B";

		protected override void OnStartup(StartupEventArgs e)
		{
			bool createdNew;
			// thread should own mutex, so pass true
			_mutex = new Mutex(true, "Global\\" + appGuid, out createdNew);
			if (!createdNew)
			{
				_mutex = null;
				Environment.Exit(0);
				return;
			}

			base.OnStartup(e);
			//run application code
		}

		protected override void OnExit(ExitEventArgs e)
		{
			if (_mutex != null)
				_mutex.ReleaseMutex();
			base.OnExit(e);
		}
	}
}
