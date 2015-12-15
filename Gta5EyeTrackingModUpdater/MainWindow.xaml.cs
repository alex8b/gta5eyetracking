using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Gta5EyeTrackingModUpdater
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private UpdaterNotifyIcon _updaterNotifyIcon;
		private Updater _updater;
		private readonly DispatcherTimer _timer;
		private TimeSpan _timerInterval;

		public string WindowName { get; set; }

		public MainWindow()
		{
			_timerInterval = TimeSpan.FromMinutes(5);
			
			PreventMultipleProcess();

			WindowName = "GTA V Eye Tracking Mod Updater " + Assembly.GetExecutingAssembly().GetName().Version;
			this.DataContext = this;

			InitializeComponent();
			var args = Environment.GetCommandLineArgs();
			if (args.Contains("-hide"))
			{
				Hide();
			}
			//todo: Show if gta folder is not specified
			this.Closing += OnClosing;
			_updaterNotifyIcon = new UpdaterNotifyIcon();
            _updater = new Updater(_updaterNotifyIcon);
			_updaterNotifyIcon.QuitMenuItemClick += UpdaterNotifyIconOnQuitMenuItemClick;
			_updaterNotifyIcon.CheckForUpdateMenuItemClick += UpdaterNotifyIconOnCheckForUpdateMenuItemClick;
			_updaterNotifyIcon.OpenWindowMenuItemClick += UpdaterNotifyIconOnOpenWindowMenuItemClick;
			_updaterNotifyIcon.DoubleClick += UpdaterNotifyIconOnOpenWindowMenuItemClick;

			_timer = new DispatcherTimer();
			_timer.Tick += TimerOnTick;
			_timer.Interval = _timerInterval;
			_timer.Start();

			Task.Run(() =>
			{
				_updater.CheckForUpdates();
			});
			
			//todo: ui - versions, install, uninstall, check for update, autostart
		}

		private void TimerOnTick(object sender, EventArgs eventArgs)
		{
			if (_updater != null)
			{
				_updater.CheckForUpdates();
			}
		}

		private void PreventMultipleProcess()
		{
			var processes = Process.GetProcesses().Where(pr => pr.ProcessName.Equals(Assembly.GetExecutingAssembly().GetName().Name, StringComparison.OrdinalIgnoreCase));
			if (processes.Any())
			{
				Application.Current.Shutdown();
			}
		}

		private void UpdaterNotifyIconOnOpenWindowMenuItemClick(object sender, EventArgs eventArgs)
		{
			this.Show();
		}

		private void UpdaterNotifyIconOnCheckForUpdateMenuItemClick(object sender, EventArgs e)
		{
			_updater.CheckForUpdates();
        }

		private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
		{
			cancelEventArgs.Cancel = true;
			this.Hide();
		}

		private void UpdaterNotifyIconOnQuitMenuItemClick(object sender, EventArgs eventArgs)
		{
			Shutdown();
		}


		public void Shutdown()
		{
			_timer.Tick -= TimerOnTick;
			_timer.Stop();

			_updaterNotifyIcon.QuitMenuItemClick -= UpdaterNotifyIconOnQuitMenuItemClick;
			_updaterNotifyIcon.CheckForUpdateMenuItemClick -= UpdaterNotifyIconOnCheckForUpdateMenuItemClick;
			_updaterNotifyIcon.OpenWindowMenuItemClick -= UpdaterNotifyIconOnOpenWindowMenuItemClick;
			_updaterNotifyIcon.DoubleClick -= UpdaterNotifyIconOnOpenWindowMenuItemClick;
			_updater.Close();
			_updaterNotifyIcon.Dispose();
			_updaterNotifyIcon = null;

			_updater = null;
			Application.Current.Shutdown();
		}
	}
}
