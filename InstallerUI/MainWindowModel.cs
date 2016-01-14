using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.TextFormatting;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace InstallerUI
{
	public class MainWindowModel : INotifyPropertyChanged
	{
		private Updater _updater;
		private readonly Settings _settings;
		private readonly SettingsStorage _settingsStorage;
		private bool _isThinking;
		private string _gtaPathText;
		private string _windowTitle;
		private string _scriptHookVVersion;
		private string _gtaVersion;
		private string _modVersion;
		private bool _canInstall;
		private bool _canRemove;
		private string _scriptHookVAvailableVersion;
		private string _modAvailableVersion;
		private bool _accept;

		public event PropertyChangedEventHandler PropertyChanged;
		public BootstrapperApplication Bootstrapper { get; private set; }

		public MainWindowModel(BootstrapperApplication bootstrapper)
		{
			this.IsThinking = false;
			this.Bootstrapper = bootstrapper;
			this.Bootstrapper.PlanPackageBegin += SetPackagePlannedState;
			this.Bootstrapper.PlanMsiFeature += SetFeaturePlannedState;
			this.Bootstrapper.PlanComplete += PlanComplete;

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
		}

		private void OnNotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private void SetFeaturePlannedState(object sender, PlanMsiFeatureEventArgs e)
		{
			Util.Log("SetFeaturePlannedState: " + e.FeatureId);
			e.State = FeatureState.Unknown;
		}

		private void SetPackagePlannedState(object sender, PlanPackageBeginEventArgs e)
		{
			e.Result = Result.Continue;
			if (Bootstrapper.Command.Action == LaunchAction.Uninstall)
			{
				e.State = RequestState.None;
			}
			else
			{
				e.State = RequestState.Present;
			}
			Util.Log("SetPackagePlannedState: " + e.PackageId + " " + e.State);
		}

		/// <summary>
		/// Method that gets invoked when the Bootstrapper PlanComplete event is fired.
		/// If the planning was successful, it instructs the Bootstrapper Engine to
		/// install the packages.
		/// </summary>
		private void PlanComplete(object sender, PlanCompleteEventArgs e)
		{
			if (e.Status >= 0)
				Bootstrapper.Engine.Apply(System.IntPtr.Zero);
		}

		public bool IsThinking
		{
			get { return _isThinking; }
			set
			{
				_isThinking = value;
				OnNotifyPropertyChanged("IsThinking");
				OnNotifyPropertyChanged("CanRemove");
				OnNotifyPropertyChanged("CanInstall");
			}
		}

		public string GtaPathText
		{
			get { return _gtaPathText; }
			set
			{
				_gtaPathText = value;
				OnNotifyPropertyChanged("GtaPathText");
			}
		}

		public string WindowTitle
		{
			get { return _windowTitle; }
			set
			{
				_windowTitle = value;
				OnNotifyPropertyChanged("WindowTitle");
			}
		}

		public string GtaVersion
		{
			get { return _gtaVersion; }
			set
			{
				_gtaVersion = value;
				OnNotifyPropertyChanged("GtaVersion");
			}
		}

		public bool CanInstall
		{
			get { return _accept && _canInstall && !_isThinking; }
			set
			{
				_canInstall = value;
				OnNotifyPropertyChanged("CanInstall");
			}
		}

		public bool CanRemove
		{
			get { return _canRemove && !_isThinking; }
			set
			{
				_canRemove = value;
				OnNotifyPropertyChanged("CanRemove");
			}
		}

		public string ScriptHookVVersion
		{
			get { return _scriptHookVVersion; }
			set
			{
				_scriptHookVVersion = value;
				OnNotifyPropertyChanged("ScriptHookVVersion");
			}
		}

		public string ScriptHookVAvailableVersion
		{
			get { return _scriptHookVAvailableVersion; }
			set
			{
				_scriptHookVAvailableVersion = value;
				OnNotifyPropertyChanged("ScriptHookVAvailableVersion");
			}
		}

		public string ModVersion
		{
			get { return _modVersion; }
			set
			{
				_modVersion = value;
				OnNotifyPropertyChanged("ModVersion");
			}
		}

		public string ModAvailableVersion
		{
			get { return _modAvailableVersion; }
			set
			{
				_modAvailableVersion = value;
				OnNotifyPropertyChanged("ModAvailableVersion");
			}
		}

		public bool Accept
		{
			get { return _accept; }
			set
			{
				_accept = value;
				OnNotifyPropertyChanged("Accept");
				OnNotifyPropertyChanged("CanInstall");
			}
		}

		public void Uninstall()
		{
			this.IsThinking = true;
			Bootstrapper.Engine.Plan(LaunchAction.Uninstall);
			_updater.RemoveScriptHookV();
			_updater.RemoveMod();
			this.IsThinking = false;
		}

		public void CheckForUpdates()
		{
			this.IsThinking = true;
			_updater.CheckForUpdates(false);
			this.IsThinking = false;
		}

		public void Install()
		{
			this.IsThinking = true;
			Bootstrapper.Engine.Plan(LaunchAction.Install);
			_updater.CheckForUpdates(true);
			this.IsThinking = false;
		}

		public void Dispose()
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

		private void UpdaterOnUpdatesChecked(object sender, EventArgs eventArgs)
		{
			UpdateText();
		}

		private void UpdaterOnModRemoved(object sender, EventArgs eventArgs)
		{
			UpdateText();
		}

		private void UpdaterOnScriptHookVRemoved(object sender, EventArgs eventArgs)
		{
			UpdateText();
		}

		private void UpdaterOnScriptHookVInstalled(object sender, EventArgs eventArgs)
		{
			UpdateText();
		}

		private void UpdaterOnModInstalled(object sender, EventArgs eventArgs)
		{
			UpdateText();
		}

		public void UpdateText()
		{
			this.GtaPathText = _settings.GtaPath;
			// Gta V
			var gtaVersion = _updater.GetGtaVersion();
			if ((this.GtaPathText == "")
				|| (gtaVersion == new Version(0, 0)))
			{
				this.GtaVersion = "not found";
			}
			else
			{
				this.GtaVersion = gtaVersion.ToString();
			}
			// Script hook V
			var installedScriptHookVVersion = _updater.GetInstalledScriptHookVVersion();
			if (installedScriptHookVVersion == "")
			{
				this.ScriptHookVVersion = "not installed";
			}
			else
			{
				this.ScriptHookVVersion = installedScriptHookVVersion;
			}
			this.ScriptHookVAvailableVersion = "";
			var availableScriptHookVVersion = _updater.GetAvailableScriptHookVVersion();
			var isGtaVersionSupported = false;
			if (availableScriptHookVVersion != null)
			{
				isGtaVersionSupported = _updater.IsGtaSupportedByAvailableScriptHookVVersion();
				if (!isGtaVersionSupported)
				{
					this.ScriptHookVAvailableVersion = "GTA V not supported";
				}
				else if (availableScriptHookVVersion == "")
				{
					this.ScriptHookVAvailableVersion = "not available";
				}
				else
				{
					this.ScriptHookVAvailableVersion = availableScriptHookVVersion;
				}
			}
			// Mod
			var installedModVersion = _updater.GetModVersion();
			if (installedModVersion == new Version(0, 0))
			{
				this.ModVersion = "not installed";
			}
			else
			{
				this.ModVersion = installedModVersion.ToString();
				this.Accept = true;
			}

			var availableModVersion = _updater.GetAvailableModVersion();
			if (availableModVersion != null)
			{
				this.ModAvailableVersion = availableModVersion.ToString();
			}

			//Button states

			this.CanInstall = isGtaVersionSupported &&
				(_updater.IsVersionLower(installedScriptHookVVersion, availableScriptHookVVersion)
				|| (installedModVersion < availableModVersion));
			this.CanRemove = _updater.IsScriptHookVInstalled();
		}

		public void SetGtaPath(string path)
		{
			_settings.GtaPath = path;
		}
	}
}