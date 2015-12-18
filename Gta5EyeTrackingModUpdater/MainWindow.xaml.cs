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
			_model = new MainWindowModel();

			this.Closing += OnClosing;

			_settingsStorage = new SettingsStorage();
			_settings = _settingsStorage.LoadSettings();
			
			if (_settings.GtaPath == null)
			{
				_settings.GtaPath = "";
			}

			if (!Util.IsValidGtaFolder(_settings.GtaPath))
			{
				var registryPath = Util.GetGtaInstallPathFromRegistry();
				if (Util.IsValidGtaFolder(registryPath))
				{
					_settings.GtaPath = registryPath;
				}
				else
				{
					_settings.GtaPath = "";
				}
			}

			SetupAutostart();

			//Init NotifyIcon
			_updaterNotifyIcon = new UpdaterNotifyIcon();
			_updater = new Updater(_updaterNotifyIcon, _settings);
			_updater.ModInstalled += UpdaterOnModInstalled;
			_updater.ScriptHookVInstalled += UpdaterOnScriptHookVInstalled;
			_updater.ScriptHookVRemoved += UpdaterOnScriptHookVRemoved;
			_updater.ModRemoved += UpdaterOnModRemoved;
			_updater.UpdatesChecked += UpdaterOnUpdatesChecked;
			_updaterNotifyIcon.QuitMenuItemClick += UpdaterNotifyIconOnQuitMenuItemClick;
			_updaterNotifyIcon.CheckForUpdateMenuItemClick += UpdaterNotifyIconOnCheckForUpdateMenuItemClick;
			_updaterNotifyIcon.OpenWindowMenuItemClick += UpdaterNotifyIconOnOpenWindowMenuItemClick;
			_updaterNotifyIcon.DoubleClick += UpdaterNotifyIconOnOpenWindowMenuItemClick;

			InitializeComponent();
			this.DataContext = _model;
			var version = Assembly.GetExecutingAssembly().GetName().Version;
			_model.WindowTitle = "GTA V Eye Tracking Mod Updater " + version.Major + "." + version.Minor + "." + version.Build;
			UpdateText();

			var args = Environment.GetCommandLineArgs();
			if (args.Contains("-hide") 
				&& _settings.GtaPath != "")
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
				_updater.CheckForUpdates(_settings.Autoupdate);
			});

			//todo: sign
			//todo: run remove on uninstall
		}

		private void UpdaterOnUpdatesChecked(object sender, EventArgs eventArgs)
		{
			UpdateText();
		}

		private void UpdaterOnModRemoved(object sender, EventArgs eventArgs)
		{
			_updaterNotifyIcon.ShowNotification("Removed Eye Tracking Mod");
			UpdateText();
		}

		private void UpdaterOnScriptHookVRemoved(object sender, EventArgs eventArgs)
		{
			_updaterNotifyIcon.ShowNotification("Removed Script Hook V");
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

			// Gta V
			var gtaVersion = _updater.GetGtaVersion();
			if ((_model.GtaPathText == "")
				|| (gtaVersion == new Version(0, 0)))
			{
				_model.GtaVersion = "not found";
			}
			else
			{
				_model.GtaVersion = gtaVersion.ToString();
			}

			// Script hook V
			var installedScriptHookVVersion = _updater.GetInstalledScriptHookVVersion();
			if (installedScriptHookVVersion == "")
			{
				_model.ScriptHookVVersion = "not installed";
			}
			else
			{
				_model.ScriptHookVVersion = installedScriptHookVVersion;
			}

			_model.ScriptHookVAvailableVersion = "";
			var availableScriptHookVVersion = _updater.GetAvailableScriptHookVVersion();
			var isGtaVersionSupported = false;
			if (availableScriptHookVVersion != null)
			{
				isGtaVersionSupported = _updater.IsGtaSupportedByAvailableScriptHookVVersion();
				if (!isGtaVersionSupported)
				{
					_model.ScriptHookVAvailableVersion = "GTA V not supported";
				}
				else if (availableScriptHookVVersion == "")
				{
					_model.ScriptHookVAvailableVersion = "not available";
				}
				else
				{
					_model.ScriptHookVAvailableVersion = availableScriptHookVVersion;
				}
			}

			// Mod
			var installedModVersion = _updater.GetModVersion();
			if (installedModVersion == new Version(0, 0))
			{
				_model.ModVersion = "not installed";
			}
			else
			{
				_model.ModVersion = installedModVersion.ToString();
			}

			var availableModVersion = _updater.GetAvailableModVersion();
			if (availableModVersion != null)
			{
				_model.ModAvailableVersion = availableModVersion.ToString();
			}


			//Mod updater
			var version = Assembly.GetExecutingAssembly().GetName().Version;
			var installedModUpdaterVersion = version.Major + "." + version.Minor + "." + version.Build;

			_model.ModUpdaterVersion = installedModUpdaterVersion;
			var availableModUpdaterVersion = _updater.GetAvailableModUpdaterVersion();
			if (availableModVersion != null)
			{
				_model.ModUpdaterAvailableVersion = availableModUpdaterVersion.ToString();
			}

			//Button states

			_model.CanInstall = isGtaVersionSupported &&
				(_updater.IsVersionLower(installedScriptHookVVersion, availableScriptHookVVersion)
				|| (installedModVersion < availableModVersion));
			_model.CanRemove = _updater.IsScriptHookVInstalled();



			// Autoupdate

			_model.Autoupdate = _settings.Autoupdate;
			_model.Autostart = _settings.Autostart;
		}

		private void TimerOnTick(object sender, EventArgs eventArgs)
		{
			if (_updater != null)
			{
				_updater.CheckForUpdates(_settings.Autoupdate);
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
			_updater.ModRemoved -= UpdaterOnModRemoved;
			_updater.UpdatesChecked -= UpdaterOnUpdatesChecked;
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
				Task.Run(() =>
				{
					_updater.CheckForUpdates();
				});
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

		private void Autoupdate_OnChecked(object sender, RoutedEventArgs e)
		{
			_settings.Autoupdate = true;
			_model.Autoupdate = true;
		}

		private void Autoupdate_OnUnchecked(object sender, RoutedEventArgs e)
		{
			_settings.Autoupdate = false;
			_model.Autoupdate = false;
		}

		private void Autostart_OnChecked(object sender, RoutedEventArgs e)
		{
			_settings.Autostart = true;
			_model.Autostart = true;
			SetupAutostart();
		}

		private void Autostart_OnUnchecked(object sender, RoutedEventArgs e)
		{
			_settings.Autostart = false;
			_model.Autostart = false;
			SetupAutostart();
		}

		private void Install_OnClick(object sender, RoutedEventArgs e)
		{
			Task.Run(() =>
			{
				_model.Installing = true;
				_updater.CheckForUpdates(true);
				_model.Installing = false;
			});
		}

		private void Remove_OnClick(object sender, RoutedEventArgs e)
		{
			Task.Run(() =>
			{
				_model.Installing = true;
				_updater.RemoveScriptHookV();
				_updater.RemoveMod();
				_model.Installing = false;
			});
		}

		public void SetupAutostart()
		{
			var reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
			if (reg == null) return;
			if (_settings.Autostart)
			{
				reg.SetValue("Gta5EyeTrackingModUpdater", @"""" + Assembly.GetExecutingAssembly().Location + @""" -hide");
			}
			else
			{
				reg.DeleteValue("Gta5EyeTrackingModUpdater");
			}
		}
	}
}
