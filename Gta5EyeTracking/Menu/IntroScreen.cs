using System;
using NativeUI;

namespace Gta5EyeTracking.Menu
{
    public class IntroScreen
    {
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
            _userAgreement = new UIMenu("Tobii Eye Tracking", "~b~PRIVACY POLICY");
            _userAgreement.SetMenuWidthOffset(50);
            //_userAgreement.ControlDisablingEnabled = false;
            _menuPool.Add(_userAgreement);

            const string privacyPolicyText = "By selecting to send usage statistics you agree that your usage statistics, such as a game session time, " +
                                             "mod settings and mod features you use will be collected by the developer. The data will be collected " +
                                             "anonymously, processed on Google Analytics and used solely to enhance user experience.";
            //"The mod is licensed under Creative Commons " +
            //"Attribution-NonCommercial-ShareAlike 4.0 International license. The full text of the license is available " +
            //"at http://creativecommons.org/licenses/by-nc-sa/4.0/legalcode and included in the mod package. By clicking " +
            //"Accept you verify that you have read and accepted the terms of the license agreement.";

            var sendUsageStatistics = new UIMenuCheckboxItem("Send Usage Statistics", true, privacyPolicyText);
            _userAgreement.AddItem(sendUsageStatistics);

            var accept = new UIMenuItem("Close", privacyPolicyText);
            accept.Activated += (sender, item) =>
            {
                _settings.SendUsageStatistics = sendUsageStatistics.Checked;
                _settings.UserAgreementAccepted = true;
                CloseMenu();
            };
            _userAgreement.AddItem(accept);

            //var decline = new UIMenuItem("Decline", privacyPolicyText);
            //decline.Activated += (sender, item) =>
            //{
            //	_settings.UserAgreementAccepted = false;
            //	CloseMenu();
            //	ShutDownRequested(this, new EventArgs());
            //};
            //_userAgreement.AddItem(decline);

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
