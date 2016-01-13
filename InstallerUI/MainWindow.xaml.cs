using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
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

			Task.Run(() =>
			{
				_model.IsThinking = true;
				_updater.CheckForUpdates(false);
				_model.IsThinking = false;
			});
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
				_model.Accept = true;
			}

			var availableModVersion = _updater.GetAvailableModVersion();
			if (availableModVersion != null)
			{
				_model.ModAvailableVersion = availableModVersion.ToString();
			}

			//Button states

			_model.CanInstall = isGtaVersionSupported &&
				(_updater.IsVersionLower(installedScriptHookVVersion, availableScriptHookVVersion)
				|| (installedModVersion < availableModVersion));
			_model.CanRemove = _updater.IsScriptHookVInstalled();
		}


		private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
		{
			if (!_model.IsThinking &&
			    (MessageBox.Show("Are you sure you want to quit GTA V Eye Tracking Mod installation?", this.Title,
				    MessageBoxButton.YesNo) == MessageBoxResult.Yes))
			{
				Shutdown();
				cancelEventArgs.Cancel = false;
			}
			else
			{
				cancelEventArgs.Cancel = true;
			}
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

		private void Install_OnClick(object sender, RoutedEventArgs e)
		{
			Task.Run(() =>
			{
				_model.IsThinking = true;
				_model.Bootstrapper.PlanPackageBegin += SetPackagePlannedState;
				_model.Bootstrapper.PlanMsiFeature += SetFeaturePlannedState;
				_model.Bootstrapper.PlanComplete += BootstrapperOnPlanComplete;
				_model.Bootstrapper.Engine.Plan(LaunchAction.Install);
				_model.Bootstrapper.Engine.Apply(IntPtr.Zero);
				_updater.CheckForUpdates(true);
				_model.IsThinking = false;
			});
		}

		private void BootstrapperOnPlanComplete(object sender, PlanCompleteEventArgs e)
		{
			_model.Bootstrapper.PlanComplete -= BootstrapperOnPlanComplete;
			Util.Log("OnPlanComplete: " + e.Status);
		}

		private void SetFeaturePlannedState(object sender, PlanMsiFeatureEventArgs e)
		{
			Util.Log("SetFeaturePlannedState: " + e.FeatureId);
			e.State = FeatureState.Unknown;
		}

		private void SetPackagePlannedState(object sender, PlanPackageBeginEventArgs e)
		{
			Util.Log("SetPackagePlannedState: " + e.PackageId);
			e.State = RequestState.Present;
		}

		private void Remove_OnClick(object sender, RoutedEventArgs e)
		{
			Task.Run(() =>
			{
				_model.IsThinking = true;
				_model.Bootstrapper.Engine.Plan(LaunchAction.Uninstall);
				_model.Bootstrapper.Engine.Apply(IntPtr.Zero);
				_updater.RemoveScriptHookV();
				_updater.RemoveMod();
				_model.IsThinking = false;
			});
		}

		private void Cancel_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void LicenseLink_OnClick(object sender, RoutedEventArgs e)
		{
			Process.Start("https://raw.githubusercontent.com/alex8b/gta5eyetracking/master/licenses/eula.txt");
		}

		private void Accept_OnChecked(object sender, RoutedEventArgs e)
		{
			_model.Accept = true;
		}

		private void Accept_OnUnchecked(object sender, RoutedEventArgs e)
		{
			_model.Accept = false;
		}
	}
}
