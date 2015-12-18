using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Gta5EyeTrackingModUpdater
{
	public class MainWindowModel : INotifyPropertyChanged
	{
		private string _gtaPathText;
		private string _windowTitle;
		private string _scriptHookVVersion;
		private string _gtaVersion;
		private string _modVersion;
		private bool _autoupdate;
		private bool _autostart;
		private bool _canInstall;
		private bool _canRemove;
		private bool _installing;
		private string _scriptHookVAvailableVersion;
		private string _modAvailableVersion;
		private string _modUpdaterVersion;
		private string _modUpdaterAvailableVersion;

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

		public bool Autoupdate
		{
			get { return _autoupdate; }
			set
			{
				_autoupdate = value;
				OnNotifyPropertyChanged("Autoupdate");
			}
		}

		public bool Autostart
		{
			get { return _autostart; }
			set
			{
				_autostart = value;
				OnNotifyPropertyChanged("Autostart");
			}
		}

		public bool CanInstall
		{
			get { return _canInstall && !_installing; }
			set
			{
				_canInstall = value;
				OnNotifyPropertyChanged("CanInstall");
			}
		}

		public bool CanRemove
		{
			get { return _canRemove && !_installing; }
			set
			{
				_canRemove = value;
				OnNotifyPropertyChanged("CanRemove");
			}
		}

		public bool Installing
		{
			get { return _installing; }
			set
			{
				_installing = value;
				OnNotifyPropertyChanged("CanRemove");
				OnNotifyPropertyChanged("CanInstall");
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

		public string ModUpdaterVersion
		{
			get { return _modUpdaterVersion; }
			set
			{
				_modUpdaterVersion = value;
				OnNotifyPropertyChanged("ModUpdaterVersion");
			}
		}

		public string ModUpdaterAvailableVersion
		{
			get { return _modUpdaterAvailableVersion; }
			set
			{
				_modUpdaterAvailableVersion = value;
				OnNotifyPropertyChanged("ModUpdaterAvailableVersion");
			}
		}

	}
}