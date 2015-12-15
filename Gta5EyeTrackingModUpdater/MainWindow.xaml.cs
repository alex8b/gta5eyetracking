using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Gta5EyeTrackingModUpdater
{
	public partial class MainWindow : Window
	{
		private UpdaterNotifyIcon _updaterNotifyIcon;
		private Updater _updater;
		private readonly DispatcherTimer _timer;
		private readonly TimeSpan _timerInterval;
		private readonly Settings _settings;
		private readonly SettingsStorage _settingsStorage;
		private readonly MainWindowModel _model;

		public MainWindow()
		{
			PreventMultipleProcess();

			_model = new MainWindowModel();

			this.Closing += OnClosing;

			_settingsStorage = new SettingsStorage();
			_settings = _settingsStorage.LoadSettings();

			if (_settings.GtaPath == null)
			{
				_settings.GtaPath = "";
			}

			//Init NotifyIcon
			_updaterNotifyIcon = new UpdaterNotifyIcon();
			_updater = new Updater(_updaterNotifyIcon, _settings);
			_updater.ModInstalled += UpdaterOnModInstalled;
			_updater.ScriptHookVInstalled += UpdaterOnScriptHookVInstalled;
			_updater.ScriptHookVRemoved += UpdaterOnScriptHookVRemoved;

			_updaterNotifyIcon.QuitMenuItemClick += UpdaterNotifyIconOnQuitMenuItemClick;
			_updaterNotifyIcon.CheckForUpdateMenuItemClick += UpdaterNotifyIconOnCheckForUpdateMenuItemClick;
			_updaterNotifyIcon.OpenWindowMenuItemClick += UpdaterNotifyIconOnOpenWindowMenuItemClick;
			_updaterNotifyIcon.DoubleClick += UpdaterNotifyIconOnOpenWindowMenuItemClick;

			InitializeComponent();
			this.DataContext = _model;
			_model.WindowTitle = "GTA V Eye Tracking Mod Updater " + Assembly.GetExecutingAssembly().GetName().Version;
			UpdateText();

			var args = Environment.GetCommandLineArgs();
			if (args.Contains("-hide") && _settings.GtaPath != "")
			{
				Hide();
			}

			//Init Timer
			_timerInterval = TimeSpan.FromMinutes(5);
			_timer = new DispatcherTimer();
			_timer.Tick += TimerOnTick;
			_timer.Interval = _timerInterval;
			_timer.Start();

			Task.Run(() =>
			{
				_updater.CheckForUpdates();
			});

			//todo: autostart
			//todo: remove some notifications
			//todo: not installed vs disabled vs not compatible
			//todo: Status: Checking for update, up to date
			//todo: log
			//todo: sign
		}

		private void UpdaterOnScriptHookVRemoved(object sender, EventArgs eventArgs)
		{
			UpdateText();
		}

		private void UpdaterOnScriptHookVInstalled(object sender, EventArgs eventArgs)
		{
			_updaterNotifyIcon.ShowNotification("Installed Script Hook V");
			UpdateText();
		}

		private void UpdaterOnModInstalled(object sender, EventArgs eventArgs)
		{
			_updaterNotifyIcon.ShowNotification("Installed Eye Tracking Mod");
			UpdateText();
		}

		private void UpdateText()
		{
			_model.GtaPathText = _settings.GtaPath;

			var modVersion = _updater.GetModVersion();
			if (modVersion == new Version(0, 0))
			{
				_model.ModVersionText = "Mod is not installed";
			}
			else
			{
				_model.ModVersionText = "Mod version: " + modVersion;
			}

			var scriptHookVVersion = _updater.GetInstalledScriptHookVVersion();
			if (scriptHookVVersion == "")
			{
				_model.ScriptHookVVersionText = "Script Hook V is not installed";
			}
			else
			{
				_model.ScriptHookVVersionText = "Script Hook V version: " + scriptHookVVersion;
			}

			var gtaVersion = _updater.GetGtaVersion();
			if ((_model.GtaPathText == "")
				|| (gtaVersion == new Version(0, 0)))
			{
				_model.GtaVersionText = "GTA V is not found in the provided path";
			}
			else
			{
				_model.GtaVersionText = "GTA V version: " + gtaVersion;
			}

			_model.Enabled = _updater.IsScriptHookVInstalled();
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
			var processes =
				Process.GetProcesses()
					.Where(
						pr => pr.ProcessName.Equals(Assembly.GetExecutingAssembly().GetName().Name, StringComparison.OrdinalIgnoreCase));
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
			_updaterNotifyIcon.ShowNotification("Checking for updates");
			Task.Run(() =>
			{
				_updater.CheckForUpdates();
			});
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

			_updater.ScriptHookVInstalled -= UpdaterOnScriptHookVInstalled;
			_updater.ModInstalled -= UpdaterOnModInstalled;
			_updater.ScriptHookVRemoved -= UpdaterOnScriptHookVRemoved;
			_updater.Close();
			_updaterNotifyIcon.Dispose();
			_updaterNotifyIcon = null;

			_updater = null;

			_settingsStorage.SaveSettings(_settings);
			Application.Current.Shutdown();
		}

		private void Browse_OnClick(object sender, RoutedEventArgs e)
		{
			var openFileDialog = new OpenFileDialog {DefaultExt = ".exe", Filter = "GTA5.exe|GTA5.exe"};
			var showDialog = openFileDialog.ShowDialog();

			if (!showDialog.HasValue) return;

			var gtaExePath = openFileDialog.FileName;
			if (File.Exists(gtaExePath) && Path.GetFileName(gtaExePath).Equals("gta5.exe", StringComparison.OrdinalIgnoreCase))
			{
				_settings.GtaPath = Path.GetDirectoryName(gtaExePath);
				UpdateText();
			}
		}

		private void CheckForUpdates_OnClick(object sender, RoutedEventArgs e)
		{
			_updaterNotifyIcon.ShowNotification("Checking for updates");

			Task.Run(() =>
			{
				_updater.CheckForUpdates();
			});
		}

		private void Enabled_OnChecked(object sender, RoutedEventArgs e)
		{
			Task.Run(() =>
			{
				_updater.CheckForUpdates();
			});
		}

		private void Enabled_OnUnchecked(object sender, RoutedEventArgs e)
		{
			Task.Run(() =>
			{
				_updater.RemoveScriptHookV();
			});
		}

		private void Autostart_OnChecked(object sender, RoutedEventArgs e)
		{
			_settings.Autostart = true;
			_model.Autostart = true;
		}

		private void Autostart_OnUnchecked(object sender, RoutedEventArgs e)
		{
			_settings.Autostart = false;
			_model.Autostart = false;
		}
	}
}
