using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace InstallerUI
{
	public partial class MainWindow : Window
	{
		private Updater _updater;
		private readonly Settings _settings;
		private readonly SettingsStorage _settingsStorage;
		private readonly MainWindowModel _model;

		public MainWindow(MainWindowModel model)
		{
			_model = model;

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

			_updater = new Updater(_settings);
			_updater.ModInstalled += UpdaterOnModInstalled;
			_updater.ScriptHookVInstalled += UpdaterOnScriptHookVInstalled;
			_updater.ScriptHookVRemoved += UpdaterOnScriptHookVRemoved;
			_updater.ModRemoved += UpdaterOnModRemoved;
			_updater.UpdatesChecked += UpdaterOnUpdatesChecked;

			InitializeComponent();
			this.DataContext = _model;
			var version = Assembly.GetExecutingAssembly().GetName().Version;
			_model.WindowTitle = "GTA V Eye Tracking Mod Installer " + version.Major + "." + version.Minor + "." + version.Build;
			UpdateText();

			var args = Environment.GetCommandLineArgs();
			if (args.Contains("-hide") 
				&& _settings.GtaPath != "")
			{
				Hide();
			}

			Task.Run(() =>
			{
				_updater.CheckForUpdates(true);
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
			//_updaterNotifyIcon.ShowNotification("Removed Eye Tracking Mod");
			UpdateText();
		}

		private void UpdaterOnScriptHookVRemoved(object sender, EventArgs eventArgs)
		{
			//_updaterNotifyIcon.ShowNotification("Removed Script Hook V");
			UpdateText();
		}

		private void UpdaterOnScriptHookVInstalled(object sender, EventArgs eventArgs)
		{
			//_updaterNotifyIcon.ShowNotification("Installed Script Hook V");
			UpdateText();
		}

		private void UpdaterOnModInstalled(object sender, EventArgs eventArgs)
		{
			//_updaterNotifyIcon.ShowNotification("Installed Eye Tracking Mod");
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
		}

		private void UpdaterNotifyIconOnOpenWindowMenuItemClick(object sender, EventArgs eventArgs)
		{
			this.Show();
		}

		private void UpdaterNotifyIconOnCheckForUpdateMenuItemClick(object sender, EventArgs e)
		{
			//_updaterNotifyIcon.ShowNotification("Checking for updates");
			Task.Run(() =>
			{
				_updater.CheckForUpdates();
			});
		}

		private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
		{
			Shutdown();
		}

		public void Shutdown()
		{
			_updater.ScriptHookVInstalled -= UpdaterOnScriptHookVInstalled;
			_updater.ModInstalled -= UpdaterOnModInstalled;
			_updater.ScriptHookVRemoved -= UpdaterOnScriptHookVRemoved;
			_updater.ModRemoved -= UpdaterOnModRemoved;
			_updater.UpdatesChecked -= UpdaterOnUpdatesChecked;
			_updater.Close();

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
			//_updaterNotifyIcon.ShowNotification("Checking for updates");

			Task.Run(() =>
			{
				_updater.CheckForUpdates();
			});
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
	}
}
