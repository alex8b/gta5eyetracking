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
		private readonly MainWindowModel _model;

		public MainWindow(MainWindowModel model)
		{
			_model = model;
			this.Closing += OnClosing;

			InitializeComponent();
			this.DataContext = _model;
			var version = Assembly.GetExecutingAssembly().GetName().Version;
			_model.WindowTitle = "GTA V Eye Tracking Mod Installer " + version.Major + "." + version.Minor + "." + version.Build;
			_model.UpdateText();
			Task.Run(() =>
			{
				_model.CheckForUpdates();
			});
		}

		private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
		{
			if (this.Visibility == Visibility.Hidden)
			{
				cancelEventArgs.Cancel = false;
				return;
			}

			if (!_model.IsThinking &&
			    (!_model.CanInstall || MessageBox.Show("Are you sure you want to quit GTA V Eye Tracking Mod installation?", this.Title,
				    MessageBoxButton.YesNo) == MessageBoxResult.Yes))
			{
				cancelEventArgs.Cancel = false;
			}
			else
			{
				cancelEventArgs.Cancel = true;
			}
		}


		private void Browse_OnClick(object sender, RoutedEventArgs e)
		{
			var openFileDialog = new OpenFileDialog {DefaultExt = ".exe", Filter = "GTA5.exe|GTA5.exe"};
			var showDialog = openFileDialog.ShowDialog();

			if (!showDialog.HasValue) return;

			var gtaExePath = openFileDialog.FileName;
			if (File.Exists(gtaExePath) && Path.GetFileName(gtaExePath).Equals("gta5.exe", StringComparison.OrdinalIgnoreCase))
			{
				_model.SetGtaPath(Path.GetDirectoryName(gtaExePath));
				_model.UpdateText();
				Task.Run(() =>
				{
					_model.CheckForUpdates();
				});
			}
		}

		private void Install_OnClick(object sender, RoutedEventArgs e)
		{
			Task.Run(() =>
			{
				_model.Install();
			});
		}

		public void Remove_OnClick(object sender, RoutedEventArgs e)
		{
			Task.Run(() =>
			{
				_model.Uninstall();
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
