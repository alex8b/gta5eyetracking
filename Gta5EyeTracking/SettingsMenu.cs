using System;
using System.Collections.Generic;
using NativeUI;

namespace Gta5EyeTracking
{
	public class SettingsMenu 
	{
		private UIMenu _mainMenu;
		private readonly MenuPool _menuPool;
		private readonly Settings _settings;
		private UIMenuListItem _freelookDevice;

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

			var values0To1 = new List<dynamic>();
			for (var i = 0; i <= 10; i++)
			{
				values0To1.Add(i * 0.1);
			}
			var valuesMinus1To1 = new List<dynamic>();
			for (var i = -10; i <= 10; i++)
			{
				valuesMinus1To1.Add(i * 0.1);
			}

			var values0To70 = new List<dynamic>();
			for (var i = 0; i <= 70; i++)
			{
				values0To70.Add((double)i);
			}

			var valuesMinus70To0 = new List<dynamic>();
			for (var i = -70; i <= 0; i++)
			{
				valuesMinus70To0.Add((double)i);
			}

			var freelookDevices = new List<dynamic>
			{
				"Gamepad",
				"Mouse"
			};

			_freelookDevice = new UIMenuListItem("Freelook device", freelookDevices, (int)_settings.FreelookDevice, "Device to use for freelook");
			_freelookDevice.OnListChanged += (sender, args) => { _settings.FreelookDevice = (FeeelookDevice)_freelookDevice.Index; };
			_mainMenu.AddItem(_freelookDevice);

			var thirdPersonFreelook = new UIMenuCheckboxItem("TPS Freelook", _settings.ThirdPersonFreelookEnabled, "Control camera with gaze");
			thirdPersonFreelook.CheckboxEvent += (sender, args) => { _settings.ThirdPersonFreelookEnabled = thirdPersonFreelook.Checked; };
			_mainMenu.AddItem(thirdPersonFreelook);

			var thirdPersonSensitivitySlider = new UIMenuListItem("TPS Freelook Sensitivity", values0To1, (int)Math.Round(_settings.ThirdPersonSensitivity / 0.1), "Freelook sensitivity");
			thirdPersonSensitivitySlider.OnListChanged += (sender, args) => { _settings.ThirdPersonSensitivity = thirdPersonSensitivitySlider.IndexToItem(thirdPersonSensitivitySlider.Index); };
			_mainMenu.AddItem(thirdPersonSensitivitySlider);



			{
				var thirdPersonYOffsetSlider = new UIMenuListItem("TPS Freelook Vertical Offset", valuesMinus1To1, (int)Math.Round((_settings.ThirdPersonYOffset + 1) / 0.1), "Freelook vertical offset");
				thirdPersonYOffsetSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonYOffset = thirdPersonYOffsetSlider.IndexToItem(thirdPersonYOffsetSlider.Index); };
				_mainMenu.AddItem(thirdPersonYOffsetSlider);

				var thirdPersonDeadZoneWidthSlider = new UIMenuListItem("TPS Freelook Deadzone Width", values0To1, (int)Math.Round(_settings.ThirdPersonDeadZoneWidth / 0.1), "Freelook deadzone");
				thirdPersonDeadZoneWidthSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonDeadZoneWidth = thirdPersonDeadZoneWidthSlider.IndexToItem(thirdPersonDeadZoneWidthSlider.Index); };
				_mainMenu.AddItem(thirdPersonDeadZoneWidthSlider);

				var thirdPersonDeadZoneHeightSlider = new UIMenuListItem("TPS Freelook Deadzone Height", values0To1, (int)Math.Round(_settings.ThirdPersonDeadZoneHeight / 0.1), "Freelook deadzone");
				thirdPersonDeadZoneHeightSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonDeadZoneHeight = thirdPersonDeadZoneHeightSlider.IndexToItem(thirdPersonDeadZoneHeightSlider.Index); };
				_mainMenu.AddItem(thirdPersonDeadZoneHeightSlider);

				var thirdPersonMinPitchSlider = new UIMenuListItem("TPS Min Pitch", valuesMinus70To0, (int)Math.Round((_settings.ThirdPersonMinPitchDeg + 70) / 1), "Freelook min pitch angle");
				thirdPersonMinPitchSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonMinPitchDeg = thirdPersonMinPitchSlider.IndexToItem(thirdPersonMinPitchSlider.Index); };
				_mainMenu.AddItem(thirdPersonMinPitchSlider);

				var thirdPersonMaxPitchSlider = new UIMenuListItem("TPS Max Pitch", values0To70, (int)Math.Round((_settings.ThirdPersonMaxPitchDeg) / 1), "Freelook max pitch angle");
				thirdPersonMaxPitchSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonMaxPitchDeg = thirdPersonMaxPitchSlider.IndexToItem(thirdPersonMaxPitchSlider.Index); };
				_mainMenu.AddItem(thirdPersonMaxPitchSlider);
			}

			{
				var thirdPersonYOffsetDrivingSlider = new UIMenuListItem("TPS Freelook Vertical Offset Driving", valuesMinus1To1, (int)Math.Round((_settings.ThirdPersonYOffsetDriving + 1) / 0.1), "Freelook vertical offset");
				thirdPersonYOffsetDrivingSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonYOffsetDriving = thirdPersonYOffsetDrivingSlider.IndexToItem(thirdPersonYOffsetDrivingSlider.Index); };
				_mainMenu.AddItem(thirdPersonYOffsetDrivingSlider);

				var thirdPersonDeadZoneWidthDrivingSlider = new UIMenuListItem("TPS Freelook Deadzone Width Driving", values0To1, (int)Math.Round(_settings.ThirdPersonDeadZoneWidthDriving / 0.1), "Freelook deadzone");
				thirdPersonDeadZoneWidthDrivingSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonDeadZoneWidthDriving = thirdPersonDeadZoneWidthDrivingSlider.IndexToItem(thirdPersonDeadZoneWidthDrivingSlider.Index); };
				_mainMenu.AddItem(thirdPersonDeadZoneWidthDrivingSlider);

				var thirdPersonDeadZoneHeightDrivingSlider = new UIMenuListItem("TPS Freelook Deadzone Height Driving", values0To1, (int)Math.Round(_settings.ThirdPersonDeadZoneHeightDriving / 0.1), "Freelook deadzone");
				thirdPersonDeadZoneHeightDrivingSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonDeadZoneHeightDriving = thirdPersonDeadZoneHeightDrivingSlider.IndexToItem(thirdPersonDeadZoneHeightDrivingSlider.Index); };
				_mainMenu.AddItem(thirdPersonDeadZoneHeightDrivingSlider);

				var thirdPersonMinPitchDrivingSlider = new UIMenuListItem("TPS Min Pitch Driving", valuesMinus70To0, (int)Math.Round((_settings.ThirdPersonMinPitchDrivingDeg + 70) / 1), "Freelook min pitch angle");
				thirdPersonMinPitchDrivingSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonMinPitchDrivingDeg = thirdPersonMinPitchDrivingSlider.IndexToItem(thirdPersonMinPitchDrivingSlider.Index); };
				_mainMenu.AddItem(thirdPersonMinPitchDrivingSlider);

				var thirdPersonMaxPitchDrivingSlider = new UIMenuListItem("TPS Max Pitch Driving", values0To70, (int)Math.Round((_settings.ThirdPersonMaxPitchDeg) / 1), "Freelook max pitch angle");
				thirdPersonMaxPitchDrivingSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonMaxPitchDrivingDeg = thirdPersonMaxPitchDrivingSlider.IndexToItem(thirdPersonMaxPitchDrivingSlider.Index); };
				_mainMenu.AddItem(thirdPersonMaxPitchDrivingSlider);
			}

			{
				var thirdPersonYOffsetPlaneSlider = new UIMenuListItem("TPS Freelook Vertical Offset Plane", valuesMinus1To1, (int)Math.Round((_settings.ThirdPersonYOffsetPlane + 1) / 0.1), "Freelook vertical offset");
				thirdPersonYOffsetPlaneSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonYOffsetPlane = thirdPersonYOffsetPlaneSlider.IndexToItem(thirdPersonYOffsetPlaneSlider.Index); };
				_mainMenu.AddItem(thirdPersonYOffsetPlaneSlider);

				var thirdPersonDeadZoneWidthPlaneSlider = new UIMenuListItem("TPS Freelook Deadzone Width Plane", values0To1, (int)Math.Round(_settings.ThirdPersonDeadZoneWidthPlane / 0.1), "Freelook deadzone");
				thirdPersonDeadZoneWidthPlaneSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonDeadZoneWidthPlane = thirdPersonDeadZoneWidthPlaneSlider.IndexToItem(thirdPersonDeadZoneWidthPlaneSlider.Index); };
				_mainMenu.AddItem(thirdPersonDeadZoneWidthPlaneSlider);

				var thirdPersonDeadZoneHeightPlaneSlider = new UIMenuListItem("TPS Freelook Deadzone Height Plane", values0To1, (int)Math.Round(_settings.ThirdPersonDeadZoneHeightPlane / 0.1), "Freelook deadzone");
				thirdPersonDeadZoneHeightPlaneSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonDeadZoneHeightPlane = thirdPersonDeadZoneHeightPlaneSlider.IndexToItem(thirdPersonDeadZoneHeightPlaneSlider.Index); };
				_mainMenu.AddItem(thirdPersonDeadZoneHeightPlaneSlider);

				var thirdPersonMinPitchPlaneSlider = new UIMenuListItem("TPS Min Pitch Plane", valuesMinus70To0, (int)Math.Round((_settings.ThirdPersonMinPitchPlaneDeg + 70) / 1), "Freelook min pitch angle");
				thirdPersonMinPitchPlaneSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonMinPitchPlaneDeg = thirdPersonMinPitchPlaneSlider.IndexToItem(thirdPersonMinPitchPlaneSlider.Index); };
				_mainMenu.AddItem(thirdPersonMinPitchPlaneSlider);

				var thirdPersonMaxPitchPlaneSlider = new UIMenuListItem("TPS Max Pitch Plane", values0To70, (int)Math.Round((_settings.ThirdPersonMaxPitchPlaneDeg) / 1), "Freelook max pitch angle");
				thirdPersonMaxPitchPlaneSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonMaxPitchPlaneDeg = thirdPersonMaxPitchPlaneSlider.IndexToItem(thirdPersonMaxPitchPlaneSlider.Index); };
				_mainMenu.AddItem(thirdPersonMaxPitchPlaneSlider);
			}


			{
				var thirdPersonYOffsetHeliSlider = new UIMenuListItem("TPS Freelook Vertical Offset Heli", valuesMinus1To1, (int)Math.Round((_settings.ThirdPersonYOffsetHeli + 1) / 0.1), "Freelook vertical offset");
				thirdPersonYOffsetHeliSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonYOffsetHeli = thirdPersonYOffsetHeliSlider.IndexToItem(thirdPersonYOffsetHeliSlider.Index); };
				_mainMenu.AddItem(thirdPersonYOffsetHeliSlider);

				var thirdPersonDeadZoneWidthHeliSlider = new UIMenuListItem("TPS Freelook Deadzone Width Heli", values0To1, (int)Math.Round(_settings.ThirdPersonDeadZoneWidthHeli / 0.1), "Freelook deadzone");
				thirdPersonDeadZoneWidthHeliSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonDeadZoneWidthHeli = thirdPersonDeadZoneWidthHeliSlider.IndexToItem(thirdPersonDeadZoneWidthHeliSlider.Index); };
				_mainMenu.AddItem(thirdPersonDeadZoneWidthHeliSlider);

				var thirdPersonDeadZoneHeightHeliSlider = new UIMenuListItem("TPS Freelook Deadzone Height Heli", values0To1, (int)Math.Round(_settings.ThirdPersonDeadZoneHeightHeli / 0.1), "Freelook deadzone");
				thirdPersonDeadZoneHeightHeliSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonDeadZoneHeightHeli = thirdPersonDeadZoneHeightHeliSlider.IndexToItem(thirdPersonDeadZoneHeightHeliSlider.Index); };
				_mainMenu.AddItem(thirdPersonDeadZoneHeightHeliSlider);

				var thirdPersonMinPitchHeliSlider = new UIMenuListItem("TPS Min Pitch Heli", valuesMinus70To0, (int)Math.Round((_settings.ThirdPersonMinPitchHeliDeg + 70) / 1), "Freelook min pitch angle");
				thirdPersonMinPitchHeliSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonMinPitchHeliDeg = thirdPersonMinPitchHeliSlider.IndexToItem(thirdPersonMinPitchHeliSlider.Index); };
				_mainMenu.AddItem(thirdPersonMinPitchHeliSlider);

				var thirdPersonMaxPitchHeliSlider = new UIMenuListItem("TPS Max Pitch Heli", values0To70, (int)Math.Round((_settings.ThirdPersonMaxPitchDeg) / 1), "Freelook max pitch angle");
				thirdPersonMaxPitchHeliSlider.OnListChanged += (sender, args) => { _settings.ThirdPersonMaxPitchHeliDeg = thirdPersonMaxPitchHeliSlider.IndexToItem(thirdPersonMaxPitchHeliSlider.Index); };
				_mainMenu.AddItem(thirdPersonMaxPitchHeliSlider);
			}


			var firstPersonFreelook = new UIMenuCheckboxItem("FPS Freelook", _settings.FirstPersonFreelookEnabled, "Control camera with gaze");
			firstPersonFreelook.CheckboxEvent += (sender, args) => { _settings.FirstPersonFreelookEnabled = firstPersonFreelook.Checked; };
			_mainMenu.AddItem(firstPersonFreelook);

			var firstPersonSensitivitySlider = new UIMenuListItem("FPS Freelook Sensitivity", values0To1, (int)Math.Round(_settings.FirstPersonSensitivity / 0.1), "Freelook sensitivity");
			firstPersonSensitivitySlider.OnListChanged += (sender, args) => { _settings.FirstPersonSensitivity = firstPersonSensitivitySlider.IndexToItem(firstPersonSensitivitySlider.Index); };
			_mainMenu.AddItem(firstPersonSensitivitySlider);

			{
				var firstPersonDeadZoneWidthSlider = new UIMenuListItem("FPS Freelook Deadzone Width", values0To1, (int)Math.Round(_settings.FirstPersonDeadZoneWidth / 0.1), "Freelook deadzone");
				firstPersonDeadZoneWidthSlider.OnListChanged += (sender, args) => { _settings.FirstPersonDeadZoneWidth = firstPersonDeadZoneWidthSlider.IndexToItem(firstPersonDeadZoneWidthSlider.Index); };
				_mainMenu.AddItem(firstPersonDeadZoneWidthSlider);

				var firstPersonDeadZoneHeightSlider = new UIMenuListItem("FPS Freelook Deadzone Height", values0To1, (int)Math.Round(_settings.FirstPersonDeadZoneHeight / 0.1), "Freelook deadzone");
				firstPersonDeadZoneHeightSlider.OnListChanged += (sender, args) => { _settings.FirstPersonDeadZoneHeight = firstPersonDeadZoneHeightSlider.IndexToItem(firstPersonDeadZoneHeightSlider.Index); };
				_mainMenu.AddItem(firstPersonDeadZoneHeightSlider);

				var firstPersonMinPitchSlider = new UIMenuListItem("FPS Min Pitch", valuesMinus70To0, (int)Math.Round((_settings.FirstPersonMinPitchDeg + 70) / 1), "Freelook min pitch angle");
				firstPersonMinPitchSlider.OnListChanged += (sender, args) => { _settings.FirstPersonMinPitchDeg = firstPersonMinPitchSlider.IndexToItem(firstPersonMinPitchSlider.Index); }; 
				_mainMenu.AddItem(firstPersonMinPitchSlider);

				var firstPersonMaxPitchSlider = new UIMenuListItem("FPS Max Pitch", values0To70, (int)Math.Round((_settings.FirstPersonMaxPitchDeg) / 1), "Freelook max pitch angle");
				firstPersonMaxPitchSlider.OnListChanged += (sender, args) => { _settings.FirstPersonMaxPitchDeg = firstPersonMaxPitchSlider.IndexToItem(firstPersonMaxPitchSlider.Index); };
				_mainMenu.AddItem(firstPersonMaxPitchSlider);
			}


			var aimingSensitivitySlider = new UIMenuListItem("Aiming Sensitivity", values0To1, (int)Math.Round(_settings.AimingSensitivity / 0.1), "Freelok sensitivity while aiming");
			aimingSensitivitySlider.OnListChanged += (sender, args) => { _settings.AimingSensitivity = aimingSensitivitySlider.IndexToItem(aimingSensitivitySlider.Index); };
			_mainMenu.AddItem(aimingSensitivitySlider);

			var aimWithGaze = new UIMenuCheckboxItem("Aim With Gaze", _settings.AimWithGazeEnabled, "Aim with gaze");
			aimWithGaze.CheckboxEvent += (sender, args) => { _settings.AimWithGazeEnabled = aimWithGaze.Checked; };
			_mainMenu.AddItem(aimWithGaze);

			var snapAtPedestrians = new UIMenuCheckboxItem("Snap At Pedestrians", _settings.SnapAtPedestriansEnabled, "Snap aim at pedestrians");
			snapAtPedestrians.CheckboxEvent += (sender, args) => { _settings.SnapAtPedestriansEnabled = snapAtPedestrians.Checked; };
			_mainMenu.AddItem(snapAtPedestrians);

			var gazeFilteringSlider = new UIMenuListItem("Gaze Filter", values0To1, (int)Math.Round(_settings.GazeFiltering / 0.1), "Filter gaze data");
			gazeFilteringSlider.OnListChanged += (sender, args) => { _settings.GazeFiltering = gazeFilteringSlider.IndexToItem(gazeFilteringSlider.Index); };
			_mainMenu.AddItem(gazeFilteringSlider);

			var incinerateAtGaze = new UIMenuCheckboxItem("Incinerate At Gaze", _settings.IncinerateAtGazeEnabled, "Incinerate at gaze");
			incinerateAtGaze.CheckboxEvent += (sender, args) => { _settings.IncinerateAtGazeEnabled = incinerateAtGaze.Checked; };
			_mainMenu.AddItem(incinerateAtGaze);

			var taseAtGaze = new UIMenuCheckboxItem("Tase At Gaze", _settings.TaseAtGazeEnabled, "Tase at gaze");
			taseAtGaze.CheckboxEvent += (sender, args) => { _settings.TaseAtGazeEnabled = taseAtGaze.Checked; };
			_mainMenu.AddItem(taseAtGaze);

			var missilesAtGaze = new UIMenuCheckboxItem("Launch Missiles At Gaze", _settings.MissilesAtGazeEnabled, "Launch missiles at gaze");
			missilesAtGaze.CheckboxEvent += (sender, args) => { _settings.MissilesAtGazeEnabled = missilesAtGaze.Checked; };
			_mainMenu.AddItem(missilesAtGaze);

			var pedestrianIntreraction = new UIMenuCheckboxItem("Pedestrian Interaction", _settings.PedestrianInteractionEnabled, "Pedestrians talk to you");
			pedestrianIntreraction.CheckboxEvent += (sender, args) => { _settings.PedestrianInteractionEnabled = pedestrianIntreraction.Checked; };
			_mainMenu.AddItem(pedestrianIntreraction);

			var dontFallFromBikes = new UIMenuCheckboxItem("Don't Fall From Bikes", _settings.DontFallFromBikesEnabled, "Never fall from bikes");
			dontFallFromBikes.CheckboxEvent += (sender, args) => { _settings.DontFallFromBikesEnabled = dontFallFromBikes.Checked; };
			_mainMenu.AddItem(dontFallFromBikes);

			_mainMenu.RefreshIndex();
		}

		public void OpenMenu()
		{
			_mainMenu.Visible = true;
		}

		public void CloseMenu()
		{
			_mainMenu.Visible = false;
		}

		public void ReloadSettings()
		{
			_freelookDevice.Index = (int) _settings.FreelookDevice;
		}
	}
}