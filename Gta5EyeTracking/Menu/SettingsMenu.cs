using System;
using System.Collections.Generic;
using NativeUI;

namespace Gta5EyeTracking.Menu
{
    public class SettingsMenu
    {
        private UIMenu _mainMenu;
        private UIMenuCheckboxItem _sendUsageStatistics;

        private readonly MenuPool _menuPool;
        private readonly Settings _settings;

        private List<object> _values0To1;

        public SettingsMenu(MenuPool menuPool, Settings settings)
        {
            _menuPool = menuPool;
            _settings = settings;

            CreateMenu();
        }

        private void CreateMenu()
        {
            _mainMenu = new UIMenu("Tobii Eye Tracking", "~b~SETTINGS");
            _mainMenu.SetMenuWidthOffset(50);
            //_mainMenu.ControlDisablingEnabled = false;
            _menuPool.Add(_mainMenu);

            InitLists();

            var responsivenessSlider = new UIMenuListItem("Responsiveness", _values0To1, (int)Math.Round(_settings.Responsiveness / 0.1), "Filter gaze data. Higher values will make crosshair movements smoother, but will increase the latency.");
            responsivenessSlider.OnListChanged += (sender, args) => { _settings.Responsiveness = (float)responsivenessSlider.IndexToItem(responsivenessSlider.Index); };
            _mainMenu.AddItem(responsivenessSlider);

            var firstPersonFreelook = new UIMenuCheckboxItem("Extended View", _settings.ExtendedViewEnabled, "Extend your view by looking at the edges of the screen");
            firstPersonFreelook.CheckboxEvent += (sender, args) => { _settings.ExtendedViewEnabled = firstPersonFreelook.Checked; };
            _mainMenu.AddItem(firstPersonFreelook);

            var extendedViewSensitivitySlider = new UIMenuListItem("Extended View Sensitivity", _values0To1, (int)Math.Round(_settings.ExtendedViewSensitivity / 0.1), "Extended View sensitivity");
            extendedViewSensitivitySlider.OnListChanged +=
                (sender, args) =>
                {
                    _settings.ExtendedViewSensitivity =
                        (float)extendedViewSensitivitySlider.IndexToItem(extendedViewSensitivitySlider.Index);
                };
            _mainMenu.AddItem(extendedViewSensitivitySlider);

            var fireAtGaze = new UIMenuCheckboxItem("Shoot At Gaze", _settings.FireAtGazeEnabled, "Your gun will shoot where you look. Move the RIGHT JOYSTICK while HOLDING LEFT THUMB to fine adjust the crosshair around your gaze point while shooting.");
            fireAtGaze.CheckboxEvent += (sender, args) => { _settings.FireAtGazeEnabled = fireAtGaze.Checked; };
            _mainMenu.AddItem(fireAtGaze);

            var aimAtGaze = new UIMenuCheckboxItem("Aim At Gaze", _settings.AimAtGazeEnabled, "You camera will turn towards the target when you press aim button.");
            aimAtGaze.CheckboxEvent += (sender, args) => { _settings.AimAtGazeEnabled = aimAtGaze.Checked; };
            _mainMenu.AddItem(aimAtGaze);

            var snapAtTargets = new UIMenuCheckboxItem("Snap At Targets", _settings.SnapAtTargetsEnabled, "Snap crosshair at targets. Makes it less challenging to use Aim At Gaze and Shoot At Gaze features.");
            snapAtTargets.CheckboxEvent += (sender, args) => { _settings.SnapAtTargetsEnabled = snapAtTargets.Checked; };
            _mainMenu.AddItem(snapAtTargets);

            var incinerateAtGaze = new UIMenuCheckboxItem("Incinerate At Gaze", _settings.IncinerateAtGazeEnabled, "Push A button to burn things where you look. This feature replaces the default command for A button.");
            incinerateAtGaze.CheckboxEvent += (sender, args) => { _settings.IncinerateAtGazeEnabled = incinerateAtGaze.Checked; };
            _mainMenu.AddItem(incinerateAtGaze);

            var taseAtGaze = new UIMenuCheckboxItem("Tase At Gaze", _settings.TaseAtGazeEnabled, "Push RB to tase people remotely with your eyes. Doesn't work in aircrafts. This feature replaces the default command for RB.");
            taseAtGaze.CheckboxEvent += (sender, args) => { _settings.TaseAtGazeEnabled = taseAtGaze.Checked; };
            _mainMenu.AddItem(taseAtGaze);

            var missilesAtGaze = new UIMenuCheckboxItem("Launch Missiles At Gaze", _settings.MissilesAtGazeEnabled, "Push B button to launch missiles at gaze. This feature replaces the default command for B button.");
            missilesAtGaze.CheckboxEvent += (sender, args) => { _settings.MissilesAtGazeEnabled = missilesAtGaze.Checked; };
            _mainMenu.AddItem(missilesAtGaze);

            var alwaysShowCrosshair = new UIMenuCheckboxItem("Always Show Crosshair", _settings.AlwaysShowCrosshairEnabled, "Show crosshair even when you are not aiming or shooting");
            alwaysShowCrosshair.CheckboxEvent += (sender, args) => { _settings.AlwaysShowCrosshairEnabled = alwaysShowCrosshair.Checked; };
            _mainMenu.AddItem(alwaysShowCrosshair);

            var dontFallFromBikes = new UIMenuCheckboxItem("Don't Fall From Bikes", _settings.DontFallFromBikesEnabled, "You won't fall from a bike when you crash into something");
            dontFallFromBikes.CheckboxEvent += (sender, args) => { _settings.DontFallFromBikesEnabled = dontFallFromBikes.Checked; };
            _mainMenu.AddItem(dontFallFromBikes);

            var firtsPersonMode = new UIMenuCheckboxItem("First person mode", _settings.FirstPersonModeEnabled, "Switch to first person view");
            firtsPersonMode.CheckboxEvent += (sender, args) => { _settings.FirstPersonModeEnabled = firtsPersonMode.Checked; };
            _mainMenu.AddItem(firtsPersonMode);

            const string privacyPolicyText = "By selecting to send usage statistics you agree that your usage statistics, such as a game session time, " +
                                             "mod settings and mod features you use will be collected by the developer. The data will be collected " +
                                             "anonymously, processed on Google Analytics and used solely to enhance user experience.";

            _sendUsageStatistics = new UIMenuCheckboxItem("Send Usage Statistics", _settings.SendUsageStatistics, privacyPolicyText);
            _sendUsageStatistics.CheckboxEvent += (sender, args) => { _settings.SendUsageStatistics = _sendUsageStatistics.Checked; };
            _mainMenu.AddItem(_sendUsageStatistics);

            _mainMenu.RefreshIndex();
        }

        private void InitLists()
        {
            _values0To1 = new List<dynamic>();
            for (var i = 0; i <= 10; i++)
            {
                _values0To1.Add(i * 0.1f);
            }
        }

        public void OpenMenu()
        {
            _sendUsageStatistics.Checked = _settings.SendUsageStatistics;
            _mainMenu.Visible = true;
        }

        public void CloseMenu()
        {
            _mainMenu.Visible = false;
        }

        //public void ReloadSettings()
        //{
        //	_freelookDevice.Index = (int) _settings.FreelookDevice;
        //}
    }
}