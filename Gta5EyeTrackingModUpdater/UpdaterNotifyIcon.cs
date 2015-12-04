using System;
using System.Windows.Forms;
using Gta5EyeTrackingModUpdater.Properties;
using ContextMenu = System.Windows.Forms.ContextMenu;

namespace Gta5EyeTrackingModUpdater
{
	public class UpdaterNotifyIcon: IDisposable
	{
		private readonly NotifyIcon _notifyIcon;

		public event EventHandler DoubleClick = delegate { };
		//public EventHandler BalloonTipClicked = delegate { };

		public event EventHandler OpenWindowMenuItemClick = delegate { };
		public event EventHandler CheckForUpdateMenuItemClick = delegate { };
		public event EventHandler QuitMenuItemClick = delegate { };


		public UpdaterNotifyIcon()
		{
			_notifyIcon = new NotifyIcon();
			_notifyIcon.Icon = Resources.ApplicationIcon;
			_notifyIcon.Visible = true;
			_notifyIcon.ContextMenu = new ContextMenu();
			_notifyIcon.Text = Resources.ApplicationName;

			_notifyIcon.DoubleClick += DoubleClickEventHandler;
			_notifyIcon.BalloonTipClicked += BalloonTipClickedEventHandler;

			_notifyIcon.ContextMenu.MenuItems.Add("Open main window", OpenWindowMenuItemClickEventHandler);
			_notifyIcon.ContextMenu.MenuItems.Add("Check for update", CheckForUpdateMenuItemClickEventHandler);
			_notifyIcon.ContextMenu.MenuItems.Add("Quit", QuitMenuItemClickEventHandler);
		}

		public void ShowNotification(string text)
		{
			_notifyIcon.ShowBalloonTip(10000, Resources.ApplicationName, text, ToolTipIcon.Info);
		}

		private void DoubleClickEventHandler(object sender, EventArgs eventArgs)
		{
			DoubleClick(this, eventArgs);
		}

		private void BalloonTipClickedEventHandler(object sender, EventArgs eventArgs)
		{
			//Process.Start(_applicationVersionMonitor.LatestNotifiedDownloadableVersion.DownloadUrl.ToString());
		}

		private void OpenWindowMenuItemClickEventHandler(object sender, EventArgs eventArgs)
		{
			OpenWindowMenuItemClick(this, eventArgs);
		}

		private void CheckForUpdateMenuItemClickEventHandler(object sender, EventArgs eventArgs)
		{
			//_applicationVersionMonitor.CheckForNewVersion();
			CheckForUpdateMenuItemClick(this, eventArgs);
		}

		private void QuitMenuItemClickEventHandler(object sender, EventArgs eventArgs)
		{
			QuitMenuItemClick(this, eventArgs);
		}

		public void Dispose()
		{
			_notifyIcon.Dispose();
		}
	}
}