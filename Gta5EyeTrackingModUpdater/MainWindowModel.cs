using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Gta5EyeTrackingModUpdater
{
	public class MainWindowModel : INotifyPropertyChanged
	{
		private string _gtaPathText;
		private string _windowTitle;
		private string _scriptHookVVersionText;
		private string _gtaVersionText;
		private string _modVersionText;
		private bool _autoupdate;
		private bool _autostart;
		private bool _canInstall;
		private bool _canRemove;
		private bool _installing;

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

		public string ScriptHookVVersionText
		{
			get { return _scriptHookVVersionText; }
			set
			{
				_scriptHookVVersionText = value;
				OnNotifyPropertyChanged("ScriptHookVVersionText");
			}
		}

		public string GtaVersionText
		{
			get { return _gtaVersionText; }
			set
			{
				_gtaVersionText = value;
				OnNotifyPropertyChanged("GtaVersionText");
			}
		}

		public string ModVersionText
		{
			get { return _modVersionText; }
			set
			{
				_modVersionText = value;
				OnNotifyPropertyChanged("ModVersionText");
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

		public MainWindowModel()
		{
		}
	}
}