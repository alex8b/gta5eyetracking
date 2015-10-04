using System;
using System.Collections.Generic;
using NativeUI;

namespace Gta5EyeTracking.Menu
{
	public class SettingsMenu 
	{
        public event EventHandler<EventArgs> ShutDownRequested = delegate {  };
		public UIMenu DeadzoneMenu;
        public UIMenu ThirdPersonFreelookMenu;
        public UIMenu FirstPersonFreelookMenu;

		private UIMenu _mainMenu;
		private UIMenuCheckboxItem _sendUsageStatistics;

        private readonly MenuPool _menuPool;
		private readonly Settings _settings;
		private UIMenuListItem _freelookDevice;

	    private List<object> _values0To1;
	    private List<object> _valuesMinus1To1;
	    private List<object> _values0To70;
	    private List<object> _valuesMinus70To0;
	    private List<object> _freelookDevices;

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
            CreateThirdPersonFreelookMenu();
            CreateFirstPersonFreelookMenu();
            CreateDeadzoneMenu();
		    _freelookDevice = new UIMenuListItem("Freelook Device", _freelookDevices, (int)_settings.FreelookDevice, "Device to use for freelook. We recommend using XBOX 360 Controller or any other XInput compatible one.");
			_freelookDevice.OnListChanged += (sender, args) => { _settings.FreelookDevice = (FeeelookDevice)_freelookDevice.Index; };
			_mainMenu.AddItem(_freelookDevice);

            var firstPersonFreelookSettings = new UIMenuItem("First Person Freelook Settings", "Look around with gaze in first person");
            _mainMenu.AddItem(firstPersonFreelookSettings);
            _mainMenu.BindMenuToItem(FirstPersonFreelookMenu, firstPersonFreelookSettings);

            var thirdPersonFreelookSettings = new UIMenuItem("Third Person Freelook Settings", "Look around with gaze in third person");
            _mainMenu.AddItem(thirdPersonFreelookSettings);
            _mainMenu.BindMenuToItem(ThirdPersonFreelookMenu, thirdPersonFreelookSettings);

            var deadzoneSettings = new UIMenuItem("Freelook Deadzones", "Deadzones prevent camera movement when you are looking inside the zone, for example, the minimap");
            _mainMenu.AddItem(deadzoneSettings);
            _mainMenu.BindMenuToItem(DeadzoneMenu, deadzoneSettings);

		    var aimingSensitivitySlider = new UIMenuListItem("Aiming Sensitivity", _values0To1, (int)Math.Round(_settings.AimingSensitivity / 0.1), "Freelook sensitivity while aiming");
			aimingSensitivitySlider.OnListChanged += (sender, args) => { _settings.AimingSensitivity = aimingSensitivitySlider.IndexToItem(aimingSensitivitySlider.Index); };
			_mainMenu.AddItem(aimingSensitivitySlider);

			var aimWithGaze = new UIMenuCheckboxItem("Aim With Gaze", _settings.AimWithGazeEnabled, "Your gun will shoot where you look. Move the RIGHT JOYSTICK while HOLDING LEFT THUMB to fine adjust the crosshair around your gaze point while shooting.");
			aimWithGaze.CheckboxEvent += (sender, args) => { _settings.AimWithGazeEnabled = aimWithGaze.Checked; };
			_mainMenu.AddItem(aimWithGaze);

			var snapAtPedestrians = new UIMenuCheckboxItem("Snap At Pedestrians", _settings.SnapAtPedestriansEnabled, "Snap crosshair at pedestrians. Makes it less challenging to aim with gaze.");
			snapAtPedestrians.CheckboxEvent += (sender, args) => { _settings.SnapAtPedestriansEnabled = snapAtPedestrians.Checked; };
			_mainMenu.AddItem(snapAtPedestrians);

			var gazeFilteringSlider = new UIMenuListItem("Gaze Filter", _values0To1, (int)Math.Round(_settings.GazeFiltering / 0.1), "Filter gaze data. Higher values will make crosshair movements smoother, but will increase the latency.");
			gazeFilteringSlider.OnListChanged += (sender, args) => { _settings.GazeFiltering = gazeFilteringSlider.IndexToItem(gazeFilteringSlider.Index); };
			_mainMenu.AddItem(gazeFilteringSlider);

			var incinerateAtGaze = new UIMenuCheckboxItem("Incinerate At Gaze", _settings.IncinerateAtGazeEnabled, "Push A button to burn things where you look. This feature replaces the default command for A button.");
			incinerateAtGaze.CheckboxEvent += (sender, args) => { _settings.IncinerateAtGazeEnabled = incinerateAtGaze.Checked; };
			_mainMenu.AddItem(incinerateAtGaze);

			var taseAtGaze = new UIMenuCheckboxItem("Tase At Gaze", _settings.TaseAtGazeEnabled, "Push RB to tase people remotely with your eyes. Doesn't work in aircrafts. This feature replaces the default command for RB.");
			taseAtGaze.CheckboxEvent += (sender, args) => { _settings.TaseAtGazeEnabled = taseAtGaze.Checked; };
			_mainMenu.AddItem(taseAtGaze);

			var missilesAtGaze = new UIMenuCheckboxItem("Launch Missiles At Gaze", _settings.MissilesAtGazeEnabled, "Push B button to launch missiles at gaze. This feature replaces the default command for B button.");
			missilesAtGaze.CheckboxEvent += (sender, args) => { _settings.MissilesAtGazeEnabled = missilesAtGaze.Checked; };
			_mainMenu.AddItem(missilesAtGaze);

			var pedestrianIntreraction = new UIMenuCheckboxItem("Pedestrian Interaction", _settings.PedestrianInteractionEnabled, "Pedestrians will talk to you if you stare at them for too long");
			pedestrianIntreraction.CheckboxEvent += (sender, args) => { _settings.PedestrianInteractionEnabled = pedestrianIntreraction.Checked; };
			_mainMenu.AddItem(pedestrianIntreraction);

			var dontFallFromBikes = new UIMenuCheckboxItem("Don't Fall From Bikes", _settings.DontFallFromBikesEnabled, "You won't fall from a bike when you crash into something");
			dontFallFromBikes.CheckboxEvent += (sender, args) => { _settings.DontFallFromBikesEnabled = dontFallFromBikes.Checked; };
			_mainMenu.AddItem(dontFallFromBikes);

			_sendUsageStatistics = new UIMenuCheckboxItem("Send Usage Statistics", _settings.SendUsageStatistics, "Anonymously collect and send usage statistics to the mod developers to improve the experience");
			_sendUsageStatistics.CheckboxEvent += (sender, args) => { _settings.SendUsageStatistics = _sendUsageStatistics.Checked; };
			_mainMenu.AddItem(_sendUsageStatistics);

			var shutDown = new UIMenuItem("Shut Down", "Unload the mod");
		    shutDown.Activated += (sender, item) =>
		    {
		        ShutDownRequested(this, new EventArgs());
		    };
            
            _mainMenu.AddItem(shutDown);

			_mainMenu.RefreshIndex();
		}

	    private void CreateDeadzoneMenu()
	    {
	        DeadzoneMenu = new UIMenu("Tobii Eye Tracking", "~b~FREELOOK DEADZONES");
	        DeadzoneMenu.SetMenuWidthOffset(50);
	            
	        DeadzoneMenu.AddItem(new UIMenuItem("Add Deadzone",
	            "Deadzones prevent camera movement when you are looking inside the zone, for example, the minimap"));
	        DeadzoneMenu.RefreshIndex();
	        _menuPool.Add(DeadzoneMenu);
	    }

	    private void CreateFirstPersonFreelookMenu()
	    {
            FirstPersonFreelookMenu = new UIMenu("Tobii Eye Tracking", "~b~FIRST PERSON FREELOOK SETTINGS");
            FirstPersonFreelookMenu.SetMenuWidthOffset(50);
	        {
	            var firstPersonFreelook = new UIMenuCheckboxItem("FPS Freelook", _settings.FirstPersonFreelookEnabled,
	                "Control camera with gaze");
	            firstPersonFreelook.CheckboxEvent +=
	                (sender, args) => { _settings.FirstPersonFreelookEnabled = firstPersonFreelook.Checked; };
                FirstPersonFreelookMenu.AddItem(firstPersonFreelook);

	            var firstPersonSensitivitySlider = new UIMenuListItem("FPS Freelook Sensitivity", _values0To1,
	                (int) Math.Round(_settings.FirstPersonSensitivity/0.1), "Freelook sensitivity");
	            firstPersonSensitivitySlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.FirstPersonSensitivity =
	                        firstPersonSensitivitySlider.IndexToItem(firstPersonSensitivitySlider.Index);
	                };
                FirstPersonFreelookMenu.AddItem(firstPersonSensitivitySlider);

	            var firstPersonDeadZoneWidthSlider = new UIMenuListItem("FPS Freelook Deadzone Width", _values0To1,
	                (int) Math.Round(_settings.FirstPersonDeadZoneWidth/0.1), "Freelook deadzone");
	            firstPersonDeadZoneWidthSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.FirstPersonDeadZoneWidth =
	                        firstPersonDeadZoneWidthSlider.IndexToItem(firstPersonDeadZoneWidthSlider.Index);
	                };
                FirstPersonFreelookMenu.AddItem(firstPersonDeadZoneWidthSlider);

	            var firstPersonDeadZoneHeightSlider = new UIMenuListItem("FPS Freelook Deadzone Height", _values0To1,
	                (int) Math.Round(_settings.FirstPersonDeadZoneHeight/0.1), "Freelook deadzone");
	            firstPersonDeadZoneHeightSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.FirstPersonDeadZoneHeight =
	                        firstPersonDeadZoneHeightSlider.IndexToItem(firstPersonDeadZoneHeightSlider.Index);
	                };
                FirstPersonFreelookMenu.AddItem(firstPersonDeadZoneHeightSlider);

	            var firstPersonMinPitchSlider = new UIMenuListItem("FPS Min Pitch", _valuesMinus70To0,
	                (int) Math.Round((_settings.FirstPersonMinPitchDeg + 70)/1), "Freelook min pitch angle");
	            firstPersonMinPitchSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.FirstPersonMinPitchDeg = firstPersonMinPitchSlider.IndexToItem(firstPersonMinPitchSlider.Index);
	                };
                FirstPersonFreelookMenu.AddItem(firstPersonMinPitchSlider);

	            var firstPersonMaxPitchSlider = new UIMenuListItem("FPS Max Pitch", _values0To70,
	                (int) Math.Round((_settings.FirstPersonMaxPitchDeg)/1), "Freelook max pitch angle");
	            firstPersonMaxPitchSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.FirstPersonMaxPitchDeg = firstPersonMaxPitchSlider.IndexToItem(firstPersonMaxPitchSlider.Index);
	                };
                FirstPersonFreelookMenu.AddItem(firstPersonMaxPitchSlider);
	        }

	        {
	            var firstPersonFreelookDriving = new UIMenuCheckboxItem("FPS Freelook Driving",
	                _settings.FirstPersonFreelookDrivingEnabled, "Control camera with gaze");
	            firstPersonFreelookDriving.CheckboxEvent +=
	                (sender, args) => { _settings.FirstPersonFreelookDrivingEnabled = firstPersonFreelookDriving.Checked; };
                FirstPersonFreelookMenu.AddItem(firstPersonFreelookDriving);

	            var firstPersonSensitivityDrivingSlider = new UIMenuListItem("FPS Freelook Sensitivity Driving", _values0To1,
	                (int) Math.Round(_settings.FirstPersonSensitivityDriving/0.1), "Freelook sensitivity");
	            firstPersonSensitivityDrivingSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.FirstPersonSensitivityDriving =
	                        firstPersonSensitivityDrivingSlider.IndexToItem(firstPersonSensitivityDrivingSlider.Index);
	                };
                FirstPersonFreelookMenu.AddItem(firstPersonSensitivityDrivingSlider);

	            var firstPersonDeadZoneWidthDrivingSlider = new UIMenuListItem("FPS Freelook Deadzone Width Driving",
	                _values0To1, (int) Math.Round(_settings.FirstPersonDeadZoneWidthDriving/0.1), "Freelook deadzone");
	            firstPersonDeadZoneWidthDrivingSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.FirstPersonDeadZoneWidthDriving =
	                        firstPersonDeadZoneWidthDrivingSlider.IndexToItem(firstPersonDeadZoneWidthDrivingSlider.Index);
	                };
                FirstPersonFreelookMenu.AddItem(firstPersonDeadZoneWidthDrivingSlider);

	            var firstPersonDeadZoneHeightDrivingSlider = new UIMenuListItem("FPS Freelook Deadzone Height Driving",
	                _values0To1, (int) Math.Round(_settings.FirstPersonDeadZoneHeightDriving/0.1), "Freelook deadzone");
	            firstPersonDeadZoneHeightDrivingSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.FirstPersonDeadZoneHeightDriving =
	                        firstPersonDeadZoneHeightDrivingSlider.IndexToItem(firstPersonDeadZoneHeightDrivingSlider.Index);
	                };
                FirstPersonFreelookMenu.AddItem(firstPersonDeadZoneHeightDrivingSlider);

	            var firstPersonMinPitchDrivingSlider = new UIMenuListItem("FPS Min Pitch Driving", _valuesMinus70To0,
	                (int) Math.Round((_settings.FirstPersonMinPitchDegDriving + 70)/1), "Freelook min pitch angle");
	            firstPersonMinPitchDrivingSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.FirstPersonMinPitchDegDriving =
	                        firstPersonMinPitchDrivingSlider.IndexToItem(firstPersonMinPitchDrivingSlider.Index);
	                };
                FirstPersonFreelookMenu.AddItem(firstPersonMinPitchDrivingSlider);

	            var firstPersonMaxPitchDrivingSlider = new UIMenuListItem("FPS Max Pitch Driving", _values0To70,
	                (int) Math.Round((_settings.FirstPersonMaxPitchDegDriving)/1), "Freelook max pitch angle");
	            firstPersonMaxPitchDrivingSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.FirstPersonMaxPitchDegDriving =
	                        firstPersonMaxPitchDrivingSlider.IndexToItem(firstPersonMaxPitchDrivingSlider.Index);
	                };
                FirstPersonFreelookMenu.AddItem(firstPersonMaxPitchDrivingSlider);
	        }
            FirstPersonFreelookMenu.RefreshIndex();
            _menuPool.Add(FirstPersonFreelookMenu);
	    }

	    private void CreateThirdPersonFreelookMenu()
	    {
            ThirdPersonFreelookMenu = new UIMenu("Tobii Eye Tracking", "~b~THIRD PERSON FREELOOK SETTINGS");
            ThirdPersonFreelookMenu.SetMenuWidthOffset(50);
	        {
	            var thirdPersonFreelook = new UIMenuCheckboxItem("TPS Freelook", _settings.ThirdPersonFreelookEnabled,
	                "Control camera with gaze");
	            thirdPersonFreelook.CheckboxEvent +=
	                (sender, args) => { _settings.ThirdPersonFreelookEnabled = thirdPersonFreelook.Checked; };
                ThirdPersonFreelookMenu.AddItem(thirdPersonFreelook);

	            var thirdPersonSensitivitySlider = new UIMenuListItem("TPS Freelook Sensitivity", _values0To1,
	                (int) Math.Round(_settings.ThirdPersonSensitivity/0.1), "Freelook sensitivity");
	            thirdPersonSensitivitySlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonSensitivity =
	                        thirdPersonSensitivitySlider.IndexToItem(thirdPersonSensitivitySlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonSensitivitySlider);

	            var thirdPersonYOffsetSlider = new UIMenuListItem("TPS Freelook Vertical Offset", _valuesMinus1To1,
	                (int) Math.Round((_settings.ThirdPersonYOffset + 1)/0.1), "Freelook vertical offset");
	            thirdPersonYOffsetSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonYOffset = thirdPersonYOffsetSlider.IndexToItem(thirdPersonYOffsetSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonYOffsetSlider);

	            var thirdPersonDeadZoneWidthSlider = new UIMenuListItem("TPS Freelook Deadzone Width", _values0To1,
	                (int) Math.Round(_settings.ThirdPersonDeadZoneWidth/0.1), "Freelook deadzone");
	            thirdPersonDeadZoneWidthSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonDeadZoneWidth =
	                        thirdPersonDeadZoneWidthSlider.IndexToItem(thirdPersonDeadZoneWidthSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonDeadZoneWidthSlider);

	            var thirdPersonDeadZoneHeightSlider = new UIMenuListItem("TPS Freelook Deadzone Height", _values0To1,
	                (int) Math.Round(_settings.ThirdPersonDeadZoneHeight/0.1), "Freelook deadzone");
	            thirdPersonDeadZoneHeightSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonDeadZoneHeight =
	                        thirdPersonDeadZoneHeightSlider.IndexToItem(thirdPersonDeadZoneHeightSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonDeadZoneHeightSlider);

	            var thirdPersonMinPitchSlider = new UIMenuListItem("TPS Min Pitch", _valuesMinus70To0,
	                (int) Math.Round((_settings.ThirdPersonMinPitchDeg + 70)/1), "Freelook min pitch angle");
	            thirdPersonMinPitchSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonMinPitchDeg = thirdPersonMinPitchSlider.IndexToItem(thirdPersonMinPitchSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonMinPitchSlider);

	            var thirdPersonMaxPitchSlider = new UIMenuListItem("TPS Max Pitch", _values0To70,
	                (int) Math.Round((_settings.ThirdPersonMaxPitchDeg)/1), "Freelook max pitch angle");
	            thirdPersonMaxPitchSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonMaxPitchDeg = thirdPersonMaxPitchSlider.IndexToItem(thirdPersonMaxPitchSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonMaxPitchSlider);
	        }

	        {
	            var thirdPersonYOffsetDrivingSlider = new UIMenuListItem("TPS Freelook Vertical Offset Driving",
	                _valuesMinus1To1, (int) Math.Round((_settings.ThirdPersonYOffsetDriving + 1)/0.1),
	                "Freelook vertical offset");
	            thirdPersonYOffsetDrivingSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonYOffsetDriving =
	                        thirdPersonYOffsetDrivingSlider.IndexToItem(thirdPersonYOffsetDrivingSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonYOffsetDrivingSlider);

	            var thirdPersonDeadZoneWidthDrivingSlider = new UIMenuListItem("TPS Freelook Deadzone Width Driving",
	                _values0To1, (int) Math.Round(_settings.ThirdPersonDeadZoneWidthDriving/0.1), "Freelook deadzone");
	            thirdPersonDeadZoneWidthDrivingSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonDeadZoneWidthDriving =
	                        thirdPersonDeadZoneWidthDrivingSlider.IndexToItem(thirdPersonDeadZoneWidthDrivingSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonDeadZoneWidthDrivingSlider);

	            var thirdPersonDeadZoneHeightDrivingSlider = new UIMenuListItem("TPS Freelook Deadzone Height Driving",
	                _values0To1, (int) Math.Round(_settings.ThirdPersonDeadZoneHeightDriving/0.1), "Freelook deadzone");
	            thirdPersonDeadZoneHeightDrivingSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonDeadZoneHeightDriving =
	                        thirdPersonDeadZoneHeightDrivingSlider.IndexToItem(thirdPersonDeadZoneHeightDrivingSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonDeadZoneHeightDrivingSlider);

	            var thirdPersonMinPitchDrivingSlider = new UIMenuListItem("TPS Min Pitch Driving", _valuesMinus70To0,
	                (int) Math.Round((_settings.ThirdPersonMinPitchDrivingDeg + 70)/1), "Freelook min pitch angle");
	            thirdPersonMinPitchDrivingSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonMinPitchDrivingDeg =
	                        thirdPersonMinPitchDrivingSlider.IndexToItem(thirdPersonMinPitchDrivingSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonMinPitchDrivingSlider);

	            var thirdPersonMaxPitchDrivingSlider = new UIMenuListItem("TPS Max Pitch Driving", _values0To70,
	                (int) Math.Round((_settings.ThirdPersonMaxPitchDeg)/1), "Freelook max pitch angle");
	            thirdPersonMaxPitchDrivingSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonMaxPitchDrivingDeg =
	                        thirdPersonMaxPitchDrivingSlider.IndexToItem(thirdPersonMaxPitchDrivingSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonMaxPitchDrivingSlider);
	        }

	        {
	            var thirdPersonYOffsetPlaneSlider = new UIMenuListItem("TPS Freelook Vertical Offset Plane", _valuesMinus1To1,
	                (int) Math.Round((_settings.ThirdPersonYOffsetPlane + 1)/0.1), "Freelook vertical offset");
	            thirdPersonYOffsetPlaneSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonYOffsetPlane =
	                        thirdPersonYOffsetPlaneSlider.IndexToItem(thirdPersonYOffsetPlaneSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonYOffsetPlaneSlider);

	            var thirdPersonDeadZoneWidthPlaneSlider = new UIMenuListItem("TPS Freelook Deadzone Width Plane", _values0To1,
	                (int) Math.Round(_settings.ThirdPersonDeadZoneWidthPlane/0.1), "Freelook deadzone");
	            thirdPersonDeadZoneWidthPlaneSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonDeadZoneWidthPlane =
	                        thirdPersonDeadZoneWidthPlaneSlider.IndexToItem(thirdPersonDeadZoneWidthPlaneSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonDeadZoneWidthPlaneSlider);

	            var thirdPersonDeadZoneHeightPlaneSlider = new UIMenuListItem("TPS Freelook Deadzone Height Plane", _values0To1,
	                (int) Math.Round(_settings.ThirdPersonDeadZoneHeightPlane/0.1), "Freelook deadzone");
	            thirdPersonDeadZoneHeightPlaneSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonDeadZoneHeightPlane =
	                        thirdPersonDeadZoneHeightPlaneSlider.IndexToItem(thirdPersonDeadZoneHeightPlaneSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonDeadZoneHeightPlaneSlider);

	            var thirdPersonMinPitchPlaneSlider = new UIMenuListItem("TPS Min Pitch Plane", _valuesMinus70To0,
	                (int) Math.Round((_settings.ThirdPersonMinPitchPlaneDeg + 70)/1), "Freelook min pitch angle");
	            thirdPersonMinPitchPlaneSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonMinPitchPlaneDeg =
	                        thirdPersonMinPitchPlaneSlider.IndexToItem(thirdPersonMinPitchPlaneSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonMinPitchPlaneSlider);

	            var thirdPersonMaxPitchPlaneSlider = new UIMenuListItem("TPS Max Pitch Plane", _values0To70,
	                (int) Math.Round((_settings.ThirdPersonMaxPitchPlaneDeg)/1), "Freelook max pitch angle");
	            thirdPersonMaxPitchPlaneSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonMaxPitchPlaneDeg =
	                        thirdPersonMaxPitchPlaneSlider.IndexToItem(thirdPersonMaxPitchPlaneSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonMaxPitchPlaneSlider);
	        }


	        {
	            var thirdPersonYOffsetHeliSlider = new UIMenuListItem("TPS Freelook Vertical Offset Heli", _valuesMinus1To1,
	                (int) Math.Round((_settings.ThirdPersonYOffsetHeli + 1)/0.1), "Freelook vertical offset");
	            thirdPersonYOffsetHeliSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonYOffsetHeli =
	                        thirdPersonYOffsetHeliSlider.IndexToItem(thirdPersonYOffsetHeliSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonYOffsetHeliSlider);

	            var thirdPersonDeadZoneWidthHeliSlider = new UIMenuListItem("TPS Freelook Deadzone Width Heli", _values0To1,
	                (int) Math.Round(_settings.ThirdPersonDeadZoneWidthHeli/0.1), "Freelook deadzone");
	            thirdPersonDeadZoneWidthHeliSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonDeadZoneWidthHeli =
	                        thirdPersonDeadZoneWidthHeliSlider.IndexToItem(thirdPersonDeadZoneWidthHeliSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonDeadZoneWidthHeliSlider);

	            var thirdPersonDeadZoneHeightHeliSlider = new UIMenuListItem("TPS Freelook Deadzone Height Heli", _values0To1,
	                (int) Math.Round(_settings.ThirdPersonDeadZoneHeightHeli/0.1), "Freelook deadzone");
	            thirdPersonDeadZoneHeightHeliSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonDeadZoneHeightHeli =
	                        thirdPersonDeadZoneHeightHeliSlider.IndexToItem(thirdPersonDeadZoneHeightHeliSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonDeadZoneHeightHeliSlider);

	            var thirdPersonMinPitchHeliSlider = new UIMenuListItem("TPS Min Pitch Heli", _valuesMinus70To0,
	                (int) Math.Round((_settings.ThirdPersonMinPitchHeliDeg + 70)/1), "Freelook min pitch angle");
	            thirdPersonMinPitchHeliSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonMinPitchHeliDeg =
	                        thirdPersonMinPitchHeliSlider.IndexToItem(thirdPersonMinPitchHeliSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonMinPitchHeliSlider);

	            var thirdPersonMaxPitchHeliSlider = new UIMenuListItem("TPS Max Pitch Heli", _values0To70,
	                (int) Math.Round((_settings.ThirdPersonMaxPitchDeg)/1), "Freelook max pitch angle");
	            thirdPersonMaxPitchHeliSlider.OnListChanged +=
	                (sender, args) =>
	                {
	                    _settings.ThirdPersonMaxPitchHeliDeg =
	                        thirdPersonMaxPitchHeliSlider.IndexToItem(thirdPersonMaxPitchHeliSlider.Index);
	                };
                ThirdPersonFreelookMenu.AddItem(thirdPersonMaxPitchHeliSlider);
	        }
            ThirdPersonFreelookMenu.RefreshIndex();
            _menuPool.Add(ThirdPersonFreelookMenu);
	    }

	    private void InitLists()
	    {
	        _values0To1 = new List<dynamic>();
	        for (var i = 0; i <= 10; i++)
	        {
	            _values0To1.Add(i*0.1);
	        }
	        _valuesMinus1To1 = new List<dynamic>();
	        for (var i = -10; i <= 10; i++)
	        {
	            _valuesMinus1To1.Add(i*0.1);
	        }

	        _values0To70 = new List<dynamic>();
	        for (var i = 0; i <= 70; i++)
	        {
	            _values0To70.Add((double) i);
	        }

	        _valuesMinus70To0 = new List<dynamic>();
	        for (var i = -70; i <= 0; i++)
	        {
	            _valuesMinus70To0.Add((double) i);
	        }

	        _freelookDevices = new List<dynamic>
	        {
	            "Gamepad",
	            "Mouse"
	        };
	    }

	    public void OpenMenu()
	    {
		    _sendUsageStatistics.Checked = _settings.SendUsageStatistics;
            _mainMenu.Visible = true;
		}

		public void CloseMenu()
		{
			_mainMenu.Visible = false;
			DeadzoneMenu.Visible = false;
			FirstPersonFreelookMenu.Visible = false;
			ThirdPersonFreelookMenu.Visible = false;
		}

		public void ReloadSettings()
		{
			_freelookDevice.Index = (int) _settings.FreelookDevice;
		}
	}
}