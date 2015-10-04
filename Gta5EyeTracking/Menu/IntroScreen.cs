using System;
using NativeUI;

namespace Gta5EyeTracking.Menu
{
	public class IntroScreen
	{
		public event EventHandler<EventArgs> ShutDownRequested = delegate { };

		private readonly MenuPool _menuPool;
		private readonly Settings _settings;
		private UIMenu _userAgreement;

		public IntroScreen(MenuPool menuPool, Settings settings)
		{
			_menuPool = menuPool;
			_settings = settings;

			CreateMenu();
		}

		private void CreateMenu()
		{
			_userAgreement = new UIMenu("Tobii Eye Tracking", "~b~USER AGREEMENT");
			_userAgreement.SetMenuWidthOffset(400);
			//_userAgreement.ControlDisablingEnabled = false;
			_menuPool.Add(_userAgreement);

			var eulaText = "Here be dragons.";

			var sendUsageStatistics = new UIMenuCheckboxItem("Send Usage Statistics", true, eulaText);
			_userAgreement.AddItem(sendUsageStatistics);

			var accept = new UIMenuItem("Accept", eulaText);
			accept.Activated += (sender, item) =>
			{
				_settings.SendUsageStatistics = sendUsageStatistics.Checked;
				_settings.UserAgreementAccepted = true;
				CloseMenu();
			};
			_userAgreement.AddItem(accept);

			var decline = new UIMenuItem("Decline", eulaText);
			decline.Activated += (sender, item) =>
			{
				_settings.UserAgreementAccepted = false;
				CloseMenu();
				ShutDownRequested(this, new EventArgs());
			};
			_userAgreement.AddItem(decline);

			_userAgreement.RefreshIndex();
		}

		public void OpenMenu()
		{
			if (!_userAgreement.Visible)
			{
				_userAgreement.Visible = true;
			}
			
		}

		public void CloseMenu()
		{
			_userAgreement.Visible = false;
		}
	}
}
