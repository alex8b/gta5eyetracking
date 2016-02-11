using System;
using System.Collections.Generic;
using System.Linq;
using Gta5EyeTracking.Deadzones;
using Gta5EyeTracking.HidEmulation;
using GTA;
using GTA.Math;
using NativeUI;

namespace Gta5EyeTracking.Features
{
	public class Freelook
	{
		private readonly Settings _settings;

		private readonly ControllerEmulation _controllerEmulation;

		private readonly double _freelookVelocityJoystick;

		private readonly MouseEmulation _mouseEmulation;
		private readonly double _freelookVelocityPixelsPerSec;
        private readonly double _freelookVelocityCam;
		
		private bool _lastInVehicle;
	    private double _relativeHeadingVehicle;
        private double _relativePitchVehicle;

	    public Freelook(ControllerEmulation controllerEmulation,
			MouseEmulation mouseEmulation,
			Settings settings
			)
		{
			_controllerEmulation = controllerEmulation;
			_mouseEmulation = mouseEmulation;
			_settings = settings;

			_freelookVelocityJoystick = 2;
			_freelookVelocityPixelsPerSec = 1500;
	        _freelookVelocityCam = 5;
		}

		public bool IsInFixedDeadzone(Vector2 screenCoord, double aspectRatio)
		{
			const double  minnimapWidthPx = 330.0;
			const double  minimapHeightPx = 280.0;
			const double definingHeight = 1080.0;
			const double minimapHeight = (minimapHeightPx / definingHeight) * 2;
			var minimapWidth = (minnimapWidthPx / definingHeight) / aspectRatio * 2;
			var safe = UIMenu.GetSafezoneBounds();
			var safeHeight = (safe.Y / definingHeight)*2;
			var safeWidth = (safe.X/definingHeight)/aspectRatio*2;

			var tmpZone = new Deadzone(-1f, 1f - (float)minimapHeight, (float)(minimapWidth + safeWidth), (float)(minimapHeight + safeHeight));
			var tmpZones = new List<Deadzone>(_settings.Deadzones) {tmpZone};
			return tmpZones.Any(z => z.Contains(screenCoord));
		}

		private void EmulateHid(double deltaX, double deltaY)
		{
			if (_settings.FreelookDevice == FeeelookDevice.Gamepad)
			{
				_controllerEmulation.DeltaX = deltaX * _freelookVelocityJoystick;
				_controllerEmulation.DeltaY = deltaY * _freelookVelocityJoystick;
			}
			else
			{
				_controllerEmulation.DeltaX = 0;
				_controllerEmulation.DeltaY = 0;
				_mouseEmulation.DeltaX = deltaX * _freelookVelocityPixelsPerSec * 0.05; //deltaTime?
				_mouseEmulation.DeltaY = deltaY * _freelookVelocityPixelsPerSec * 0.05;
			}
		}

		public void ThirdPersonFreelook(Vector2 gazeNormalizedCenterDelta, double aspectRatio)
		{
            _relativeHeadingVehicle = 0;
            _relativePitchVehicle = 0;

			if (!GameplayCamera.IsRendering) return;

			if (!_lastInVehicle && Game.Player.Character.IsInVehicle())
			{
				GameplayCamera.RelativeHeading = 0; //reset the view when you enter a vehicle
			}
			_lastInVehicle = Game.Player.Character.IsInVehicle();

			double deltaX = 0;
			double deltaY = 0;
			if (_settings.ThirdPersonFreelookEnabled
				&& (!IsInFixedDeadzone(gazeNormalizedCenterDelta, aspectRatio)))
			{
				var minPitchDeg = _settings.ThirdPersonMinPitchDeg;
				var maxPitchDeg = _settings.ThirdPersonMaxPitchDeg;
				var deadzoneWidth = _settings.ThirdPersonDeadZoneWidth;
				var deadzoneHeight = _settings.ThirdPersonDeadZoneHeight;
				var verticalOffset = _settings.ThirdPersonYOffset;

				if (Game.Player.Character.IsInVehicle())
				{
					minPitchDeg = _settings.ThirdPersonMinPitchDrivingDeg;
					maxPitchDeg = _settings.ThirdPersonMaxPitchDrivingDeg;
					deadzoneWidth = _settings.ThirdPersonDeadZoneWidthDriving;
					deadzoneHeight = _settings.ThirdPersonDeadZoneHeightDriving;
					verticalOffset = _settings.ThirdPersonYOffsetDriving;
				}
				if (Game.Player.Character.IsInPlane)
				{
					minPitchDeg = _settings.ThirdPersonMinPitchPlaneDeg;
					maxPitchDeg = _settings.ThirdPersonMaxPitchPlaneDeg;
					deadzoneWidth = _settings.ThirdPersonDeadZoneWidthPlane;
					deadzoneHeight = _settings.ThirdPersonDeadZoneHeightPlane;
					verticalOffset = _settings.ThirdPersonYOffsetPlane;
				}
				if (Game.Player.Character.IsInHeli)
				{
					minPitchDeg = _settings.ThirdPersonMinPitchHeliDeg;
					maxPitchDeg = _settings.ThirdPersonMaxPitchHeliDeg;
					deadzoneWidth = _settings.ThirdPersonDeadZoneWidthHeli;
					deadzoneHeight = _settings.ThirdPersonDeadZoneHeightHeli;
					verticalOffset = _settings.ThirdPersonYOffsetHeli;
				}

				var freelookDeltaVector = new Vector2(gazeNormalizedCenterDelta.X, (float)(gazeNormalizedCenterDelta.Y + verticalOffset));
				
				if (!(Math.Abs(freelookDeltaVector.X) <= deadzoneWidth))
				{
					deltaX = (freelookDeltaVector.X - Math.Sign(freelookDeltaVector.X) * deadzoneWidth) * (float)(_settings.ThirdPersonSensitivity);
				}

				if (!(Math.Abs(freelookDeltaVector.Y) <= deadzoneHeight))
				{
					if ((_settings.FreelookDevice == FeeelookDevice.Gamepad && Game.Player.Character.IsInVehicle()) //gamepad + vehicle = no caps
					   	|| 
						!(((GameplayCamera.Rotation.X < minPitchDeg) && (freelookDeltaVector.Y > 0)) //Limits for mouse
							|| ((GameplayCamera.Rotation.X > maxPitchDeg) && (freelookDeltaVector.Y < 0)))
						)
					{
						deltaY = (freelookDeltaVector.Y - Math.Sign(freelookDeltaVector.Y) * deadzoneHeight) * (float)(_settings.ThirdPersonSensitivity);
					}
					else
					{
						deltaY = ((freelookDeltaVector.Y - Math.Sign(freelookDeltaVector.Y) * deadzoneHeight) * (float)(_settings.ThirdPersonSensitivity)) * 0.1;
					}
				}
			}

			EmulateHid(deltaX, deltaY);
		}

		public void ThirdPersonAimFreelook(Vector2 gazeNormalizedCenterDelta, Ped ped, double aspectRatio)
		{
            _relativeHeadingVehicle = 0;
            _relativePitchVehicle = 0;

			if (!GameplayCamera.IsRendering) return;

			double deltaX = 0;
			double deltaY = 0;
			if (_settings.ThirdPersonFreelookEnabled
				&& (!IsInFixedDeadzone(gazeNormalizedCenterDelta, aspectRatio)))
			{
				var freelookDeltaVector = new Vector2(gazeNormalizedCenterDelta.X, gazeNormalizedCenterDelta.Y);

				if (ped != null && ped != Game.Player.Character)
				{
					Vector2 screenCoords;
					var pos = ped.GetBoneCoord(Bone.SKEL_L_Clavicle);
					Geometry.WorldToScreenRel(pos, out screenCoords);
					freelookDeltaVector = screenCoords;
				}

				deltaX = freelookDeltaVector.X * (float)(_settings.AimingSensitivity);
				deltaY = freelookDeltaVector.Y * (float)(_settings.AimingSensitivity);
			}

			EmulateHid(deltaX, deltaY);
		}

		public void FirstPersonFreelook(Vector2 gazeNormalizedCenterDelta, double aspectRatio)
		{
            _relativeHeadingVehicle = 0;
            _relativePitchVehicle = 0;

			if (!GameplayCamera.IsRendering) return;

			double deltaX = 0;
			double deltaY = 0;
			if (_settings.FirstPersonFreelookEnabled
				&& (!IsInFixedDeadzone(gazeNormalizedCenterDelta, aspectRatio)))
			{
				var freelookDeltaVector = new Vector2(gazeNormalizedCenterDelta.X, gazeNormalizedCenterDelta.Y);

				if (!(Math.Abs(freelookDeltaVector.X) <= _settings.FirstPersonDeadZoneWidth))
				{
					deltaX = (freelookDeltaVector.X - Math.Sign(freelookDeltaVector.X) * _settings.FirstPersonDeadZoneWidth) * (float)(_settings.FirstPersonSensitivity);
				}

				if (!(Math.Abs(freelookDeltaVector.Y) <= _settings.FirstPersonDeadZoneHeight))
				{
					
					if (((GameplayCamera.Rotation.X >= _settings.FirstPersonMinPitchDeg) && (freelookDeltaVector.Y > 0))
						|| ((GameplayCamera.Rotation.X <= _settings.FirstPersonMaxPitchDeg) && (freelookDeltaVector.Y < 0)))
					{
						deltaY = (freelookDeltaVector.Y - Math.Sign(freelookDeltaVector.Y) * _settings.FirstPersonDeadZoneHeight) * (float)(_settings.FirstPersonSensitivity);
					}
				}

			}

			EmulateHid(deltaX, deltaY);
		}

        public void FirstPersonFreelookVehicle(Vector2 gazeNormalizedCenterDelta, double aspectRatio)
        {
            double deltaX = 0;
            double deltaY = 0;
            if (_settings.FirstPersonFreelookDrivingEnabled)
            {
                if (!IsInFixedDeadzone(gazeNormalizedCenterDelta, aspectRatio))
                {
                    var freelookDeltaVector = new Vector2(gazeNormalizedCenterDelta.X, gazeNormalizedCenterDelta.Y);

                    if (!(Math.Abs(freelookDeltaVector.X) <= _settings.FirstPersonDeadZoneWidthDriving))
                    {
                        deltaX = (freelookDeltaVector.X - Math.Sign(freelookDeltaVector.X) * _settings.FirstPersonDeadZoneWidthDriving) * (float)(_settings.FirstPersonSensitivityDriving);
                    }

                    if (!(Math.Abs(freelookDeltaVector.Y) <= _settings.FirstPersonDeadZoneHeightDriving))
                    {

                        if (((GameplayCamera.Rotation.X >= _settings.FirstPersonMinPitchDegDriving) && (freelookDeltaVector.Y > 0))
                            || ((GameplayCamera.Rotation.X <= _settings.FirstPersonMaxPitchDegDriving) && (freelookDeltaVector.Y < 0)))
                        {
                            deltaY = (freelookDeltaVector.Y - Math.Sign(freelookDeltaVector.Y) * _settings.FirstPersonDeadZoneHeightDriving) * (float)(_settings.FirstPersonSensitivityDriving);
                        }
                    }
                }

                _relativeHeadingVehicle += -deltaX * _freelookVelocityCam;
                _relativePitchVehicle += deltaY * _freelookVelocityCam;
                GameplayCamera.RelativeHeading = (float)(_relativeHeadingVehicle);
                Util.SetGamePlayCamRawPitch((float)(_relativePitchVehicle));
            }
        }

		public void FirstPersonAimFreelook(Vector2 gazeNormalizedCenterDelta, Ped ped, double aspectRatio)
		{
            _relativeHeadingVehicle = 0;
            _relativePitchVehicle = 0;
            if (!GameplayCamera.IsRendering) return;

			double deltaX = 0;
			double deltaY = 0;
			if (_settings.FirstPersonFreelookEnabled
				&& (!IsInFixedDeadzone(gazeNormalizedCenterDelta, aspectRatio)))
			{
				var freelookDeltaVector = new Vector2(gazeNormalizedCenterDelta.X, gazeNormalizedCenterDelta.Y);

				if (ped != null && ped != Game.Player.Character)
				{
					Vector2 screenCoords;
					Geometry.WorldToScreenRel(ped.Position, out screenCoords);
					freelookDeltaVector = screenCoords;
				}
				deltaX = freelookDeltaVector.X * (float)(_settings.AimingSensitivity);
				deltaY = freelookDeltaVector.Y * (float)(_settings.AimingSensitivity);
			}

			EmulateHid(deltaX, deltaY);
		}

		public void Process(Vector2 gazeNormalizedCenterDelta, Ped ped, double aspectRatio)
		{
			if (Game.Player.Character.IsInVehicle())
			{
				//vehicle

				if (Geometry.IsFirstPersonCameraActive())
				{
					FirstPersonFreelookVehicle(gazeNormalizedCenterDelta, aspectRatio);
				}
				else
				{
					ThirdPersonFreelook(gazeNormalizedCenterDelta, aspectRatio);
				}
			}
			else
			{
				//on foot
				if (Geometry.IsFirstPersonCameraActive())
				{
					if (GameplayCamera.IsAimCamActive)
					{
						//fps aim
						FirstPersonAimFreelook(gazeNormalizedCenterDelta, ped, aspectRatio);
					}
					else
					{
						//fps
						FirstPersonFreelook(gazeNormalizedCenterDelta, aspectRatio);
					}
				}
				else
				{
					if (GameplayCamera.IsAimCamActive)
					{
						//aim
						ThirdPersonAimFreelook(gazeNormalizedCenterDelta, ped, aspectRatio);
					}
					else
					{
						//normal
						ThirdPersonFreelook(gazeNormalizedCenterDelta, aspectRatio);
					}
				}
			}
		}
	}
}
