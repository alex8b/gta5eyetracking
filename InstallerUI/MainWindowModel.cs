using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Color = System.Drawing.Color;

namespace InstallerUI
{
	public class MainWindowModel : INotifyPropertyChanged
	{
		public const int MaxCheckCount = 3;

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
		private Brush _gtaVersionColor;
		private Brush _scriptHookVVersionColor;
		private Brush _scriptHookVAvailableVersionColor;
		private Brush _modVersionColor;
		private Brush _modAvailableVersionColor;
		private readonly Brush _redColor = Brushes.Red;
        private readonly Brush _greenColor = Brushes.GreenYellow;
		private readonly Brush _whiteColor = Brushes.White;
		private string _statusText;
		public LaunchAction LastInstallCommand { get; set; }

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
			StatusText = "Ready.";
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
			if (LastInstallCommand == LaunchAction.Uninstall)
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
		public string StatusText
		{
			get { return _statusText; }
			set
			{
				_statusText = value;
				OnNotifyPropertyChanged("StatusText");
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
		public string GtaVersion
		{
			get { return _gtaVersion; }
			set
			{
				_gtaVersion = value;
				OnNotifyPropertyChanged("GtaVersion");
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

		public Brush GtaVersionColor
		{
			get { return _gtaVersionColor; }
			set
			{
				_gtaVersionColor = value;
				OnNotifyPropertyChanged("GtaVersionColor");
			}
		}

		public Brush ScriptHookVVersionColor
		{
			get { return _scriptHookVVersionColor; }
			set
			{
				_scriptHookVVersionColor = value;
				OnNotifyPropertyChanged("ScriptHookVVersionColor");
			}
		}

		public Brush ScriptHookVAvailableVersionColor
		{
			get { return _scriptHookVAvailableVersionColor; }
			set
			{
				_scriptHookVAvailableVersionColor = value;
				OnNotifyPropertyChanged("ScriptHookVAvailableVersionColor");
			}
		}

		public Brush ModVersionColor
		{
			get { return _modVersionColor; }
			set
			{
				_modVersionColor = value;
				OnNotifyPropertyChanged("ModVersionColor");
			}
		}

		public Brush ModAvailableVersionColor
		{
			get { return _modAvailableVersionColor; }
			set
			{
				_modAvailableVersionColor = value;
				OnNotifyPropertyChanged("ModAvailableVersionColor");
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
			StatusText = "Uninstalling...";
			this.IsThinking = true;
			LastInstallCommand = LaunchAction.Uninstall;
            Bootstrapper.Engine.Plan(LaunchAction.Uninstall);
			_updater.RemoveScriptHookV();
			_updater.RemoveMod();
			this.IsThinking = false;
		}

		public void CheckForUpdates()
		{
			StatusText = "Checking for updates...";
			this.IsThinking = true;
			var result = false;
			int i = 0;
			while (!result && (i < MaxCheckCount))
			{
				result = _updater.CheckForUpdates(false);
				i++;
			}
			if (!result)
			{
				StatusText = "Failed to read version info from the server. Please try again later.";
			}
			else
			{
				StatusText = "Ready.";
			}
			this.IsThinking = false;
		}

		public void Install()
		{
			StatusText = "Installing...";
			this.IsThinking = true;
			LastInstallCommand = LaunchAction.Install;
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
			StatusText = "GTA V Eye Tracking Mod removed.";
		}

		private void UpdaterOnScriptHookVRemoved(object sender, EventArgs eventArgs)
		{
			UpdateText();
			StatusText = "Script Hook V removed.";
		}

		private void UpdaterOnScriptHookVInstalled(object sender, EventArgs eventArgs)
		{
			UpdateText();
			StatusText = "Script Hook V installed.";
		}

		private void UpdaterOnModInstalled(object sender, EventArgs eventArgs)
		{
			UpdateText();
			StatusText = "GTA V Eye Tracking Mod installed.";
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
				this.GtaVersionColor = _redColor;
			}
			else
			{
				this.GtaVersion = gtaVersion.ToString();
				this.GtaVersionColor = _whiteColor;
			}
			// Script hook V
			var installedScriptHookVVersion = _updater.GetInstalledScriptHookVVersion();
			var availableScriptHookVVersion = _updater.GetAvailableScriptHookVVersion();
			if (installedScriptHookVVersion == "")
			{
				this.ScriptHookVVersion = "not installed";
				this.ScriptHookVVersionColor = _redColor;
			}
			else
			{
				this.ScriptHookVVersion = installedScriptHookVVersion;
				if (availableScriptHookVVersion != null && _updater.IsVersionLower(installedScriptHookVVersion, availableScriptHookVVersion))
				{
					this.ScriptHookVVersionColor = _redColor;
				}
				else
				{
					this.ScriptHookVVersionColor = _whiteColor;
				}
			}
			this.ScriptHookVAvailableVersion = "";

			var isGtaVersionSupported = false;
			if (availableScriptHookVVersion != null)
			{
				isGtaVersionSupported = _updater.IsGtaSupportedByAvailableScriptHookVVersion();
				if (!isGtaVersionSupported)
				{
					this.ScriptHookVAvailableVersion = "GTA V not supported";
					this.ScriptHookVAvailableVersionColor = _redColor;
				}
				else if (availableScriptHookVVersion == "")
				{
					this.ScriptHookVAvailableVersion = "not available";
					this.ScriptHookVAvailableVersionColor = _redColor;
				}
				else
				{
					this.ScriptHookVAvailableVersion = availableScriptHookVVersion;
					if (availableScriptHookVVersion == installedScriptHookVVersion)
					{
						this.ScriptHookVAvailableVersionColor = _whiteColor;
					}
					else
					{
						this.ScriptHookVAvailableVersionColor = _greenColor;
					}
				}
			}
			// Mod
			var installedModVersion = _updater.GetModVersion();
			var availableModVersion = _updater.GetAvailableModVersion();

			if (installedModVersion == new Version(0, 0))
			{
				this.ModVersion = "not installed";
				this.ModVersionColor = _redColor;
			}
			else
			{
				this.ModVersion = installedModVersion.ToString();
				if (availableModVersion != null && installedModVersion < availableModVersion)
				{
					this.ModVersionColor = _redColor;
				}
				else
				{
					this.ModVersionColor = _whiteColor;
				}

				this.Accept = true;
			}


			if (availableModVersion != null)
			{
				this.ModAvailableVersion = availableModVersion.ToString();
				if (availableModVersion == installedModVersion)
				{
					this.ModAvailableVersionColor = _whiteColor;
				}
				else
				{
					this.ModAvailableVersionColor = _greenColor;
				}
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