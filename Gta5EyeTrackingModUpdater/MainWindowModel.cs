using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Gta5EyeTrackingModUpdater
{
	public class MainWindowModel : INotifyPropertyChanged
	{
		private string _gtaPathText;
		private string _windowName;
		private string _scriptHookVVersionText;
		private string _gtaVersionText;
		private string _modVersionText;
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

		public string WindowName
		{
			get { return _windowName; }
			set
			{
				_windowName = value;
				OnNotifyPropertyChanged("WindowName");
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

		public MainWindowModel()
		{
			_gtaPathText = "adfsadasdasd";
		}
	}
}