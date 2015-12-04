using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Gta5EyeTrackingModUpdater
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private UpdaterNotifyIcon _updaterNotifyIcon;
		private Updater _updater;

		public MainWindow()
		{
			InitializeComponent();
			Hide();
			this.Closing += OnClosing;
			_updaterNotifyIcon = new UpdaterNotifyIcon();
            _updater = new Updater(_updaterNotifyIcon);
			_updaterNotifyIcon.QuitMenuItemClick += UpdaterNotifyIconOnQuitMenuItemClick;
			_updaterNotifyIcon.CheckForUpdateMenuItemClick += UpdaterNotifyIconOnCheckForUpdateMenuItemClick;
			_updaterNotifyIcon.OpenWindowMenuItemClick += UpdaterNotifyIconOnOpenWindowMenuItemClick;
			_updaterNotifyIcon.DoubleClick += UpdaterNotifyIconOnOpenWindowMenuItemClick;
			//todo: ui - versions, install, uninstall, check for update, autostart
			//todo: close program if new instance is running
		}

		private void UpdaterNotifyIconOnOpenWindowMenuItemClick(object sender, EventArgs eventArgs)
		{
			this.Show();
		}

		private void UpdaterNotifyIconOnCheckForUpdateMenuItemClick(object sender, EventArgs e)
		{
			_updater.CheckForUpdates();
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
			_updaterNotifyIcon.QuitMenuItemClick -= UpdaterNotifyIconOnQuitMenuItemClick;
			_updaterNotifyIcon.CheckForUpdateMenuItemClick -= UpdaterNotifyIconOnCheckForUpdateMenuItemClick;
			_updaterNotifyIcon.OpenWindowMenuItemClick -= UpdaterNotifyIconOnOpenWindowMenuItemClick;
			_updaterNotifyIcon.DoubleClick -= UpdaterNotifyIconOnOpenWindowMenuItemClick;
			_updater.Close();
			_updaterNotifyIcon.Dispose();
			_updaterNotifyIcon = null;
			_updater = null;
			Application.Current.Shutdown();
		}
	}
}
