using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace InstallerUI
{
	public class MainWindowModel : INotifyPropertyChanged
	{
		public MainWindowModel(BootstrapperApplication bootstrapper)
		{
			this.IsThinking = false;
			this.Bootstrapper = bootstrapper;
			this.Bootstrapper.PlanPackageBegin += SetPackagePlannedState;
			this.Bootstrapper.PlanMsiFeature += SetFeaturePlannedState;
			this.Bootstrapper.PlanComplete += PlanComplete;
		}
		
		private bool _isThinking;
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
		public BootstrapperApplication Bootstrapper { get; private set; }

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

		//private RelayCommand installCommand;
		//public RelayCommand InstallCommand
		//{
		//	get
		//	{
		//		if (installCommand == null)
		//			installCommand = new RelayCommand(() => InstallExecute(), () => InstallEnabled == true);
		//		return installCommand;
		//	}
		//}
		//private RelayCommand uninstallCommand;
		//public RelayCommand UninstallCommand
		//{
		//	get
		//	{
		//		if (uninstallCommand == null)
		//			uninstallCommand = new RelayCommand(() => UninstallExecute(), () => UninstallEnabled == true);
		//		return uninstallCommand;
		//	}
		//}
		//private RelayCommand exitCommand;
		//public RelayCommand ExitCommand
		//{
		//	get
		//	{
		//		if (exitCommand == null)
		//			exitCommand = new RelayCommand(() => ExitExecute());
		//		return exitCommand;
		//	}
		//}




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
		private void OnNotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
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
	}
}