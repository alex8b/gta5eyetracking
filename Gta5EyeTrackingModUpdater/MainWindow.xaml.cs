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

		public string GtaPathText { get; set; }

		public string WindowName { get; set; }
		public string ScriptHookVVersionText { get; set; }
		public string GtaVersionText { get; set; }
		public string ModVersionText { get; set; }

		public MainWindow()
		{
			PreventMultipleProcess();

			WindowName = "GTA V Eye Tracking Mod Updater " + Assembly.GetExecutingAssembly().GetName().Version;
			this.DataContext = this;

			this.Closing += OnClosing;

			_settings = new Settings();
			LoadSettings();

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

		
			UpdateText();
			
			InitializeComponent();

			var args = Environment.GetCommandLineArgs();
			if (args.Contains("-hide") && GtaPathText != "")
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

		private void LoadSettings()
		{
			_settings.GtaPath = Util.GetGtaInstallPathFromRegistry();
			if (_settings.GtaPath == null)
			{
				_settings.GtaPath = "C:\\";
			}
			//read from config
		}

		private void UpdateText()
		{
			GtaPathText = _settings.GtaPath;

			var modVersion = _updater.GetModVersion();
			if (modVersion == new Version(0, 0))
			{
				ModVersionText = "Mod is not installed";
			}
			else
			{
				ModVersionText = "Mod version: " + modVersion;
			}

			var scriptHookVVersion = _updater.GetInstalledScriptHookVVersion();
			if (scriptHookVVersion == "")
			{
				ScriptHookVVersionText = "Script Hook V is not installed";
			}
			else
			{
				ScriptHookVVersionText = "Script Hook V version: " + scriptHookVVersion;
			}

			var gtaVersion = _updater.GetGtaVersion();
			if ((GtaPathText == "")
				|| (gtaVersion == new Version(0, 0)))
			{
				GtaVersionText = "GTA V is not found in the provided path";
			}
			else
			{
				GtaVersionText = "GTA V version: " + gtaVersion;
			}
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

			_updater.ScriptHookVInstalled -= UpdaterOnScriptHookVInstalled;
			_updater.ModInstalled -= UpdaterOnModInstalled;
			_updater.ScriptHookVRemoved -= UpdaterOnScriptHookVRemoved;
			_updater.Close();
			_updaterNotifyIcon.Dispose();
			_updaterNotifyIcon = null;

			_updater = null;
			Application.Current.Shutdown();
		}

		private void Browse_OnClick(object sender, RoutedEventArgs e)
		{
			var openFileDialog = new OpenFileDialog { DefaultExt = ".exe", Filter = "GTA5.exe|GTA5.exe" };
			var showDialog = openFileDialog.ShowDialog();

			if (!showDialog.HasValue) return;

			var gtaExePath = openFileDialog.FileName;
			if (File.Exists(gtaExePath) && Path.GetFileName(gtaExePath).Equals("gta5.exe", StringComparison.OrdinalIgnoreCase))
			{
				_settings.GtaPath = Path.GetDirectoryName(gtaExePath);
				UpdateText();
			}
		}


	}
}
