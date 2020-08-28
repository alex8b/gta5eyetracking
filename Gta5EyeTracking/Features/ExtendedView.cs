using System;
using GTA;
using GTA.Math;
using Control = GTA.Control;

namespace Gta5EyeTracking.Features
{
    public class ExtendedView : ExtendedViewBase, IDisposable
    {
        public Vector3 CameraPositionWithoutExtendedView { get; private set; }
        //public Vector3 CameraRotationWithoutExtendedView { get; private set; }
		public Quaternion CameraRotationWithoutExtendedViewQ { get; private set; } = Quaternion.Identity;
		//public Vector3 GameplayCameraRotationFiltered { get; private set; }
		public Quaternion GameplayCameraRotationFilteredQ { get; private set; } = Quaternion.Identity;
		//public Vector3 VehicleRotationFiltered { get; private set; }
		public Quaternion VehicleRotationFilteredQ { get; private set; } = Quaternion.Identity;
        public float DistanceToCharacter { get; private set; }

        public float AimAtCrosshairDeadzoneSize = 0.1f;

        private const float HeadPositionSensitivity = 0.5f;
        private const float HeadPositionScalar = 0.001f;

        private const float GameplayCameraFilteringScalar = 0.1f;
        private const float AimTransitionDuration = 1f;

        private const float ExtraOffsetLerpScalar = 0.1f;
        private const float DistanceToCharacterLerpScalar = 0.1f;

        private readonly Settings _settings;
        private readonly GameState _gameState;
        private readonly Aiming _aiming;
        private readonly DebugOutput _debugOutput;

        private readonly Camera _extendedViewCamera;
        private readonly Camera _forwardCamera;

        private DateTime _lastNotInVehicle;

        private float _aimTransitionState;

        private Vector3 _extraOffset;

        private Vector3? _lastTarget;
        private float _pitchToTarget;
        private float _yawToTarget;

        private bool _isInThirdPersonAim;
        private bool _aimAtGazeRequested;

        private float _headXFiltered;

		private Vector3 _extendedViewCameraRotation;
		private Quaternion _extendedViewCameraRotationQ;

        public ExtendedView(Settings settings,
            GameState gameState,
            Aiming aiming,
            DebugOutput debugOutput
            )
        {
            _settings = settings;
            _gameState = gameState;
            _aiming = aiming;
            _debugOutput = debugOutput;

            _aimTransitionState = 1;

            _extendedViewCamera = World.CreateCamera(new Vector3(), Vector3.Zero, 60f);
            _forwardCamera = World.CreateCamera(new Vector3(), Vector3.Zero, 60f);
        }

        public void ProcessThirdPerson()
        {
            _aimTransitionState = Math.Min(1, _aimTransitionState + Time.unscaledDeltaTime / AimTransitionDuration);
            _isInThirdPersonAim = false;
            _aimAtGazeRequested = false;
            _lastTarget = null;

            Game.Player.Character.IsVisible = true;

            if (!_gameState.IsSniperWeaponAndZoomed && _settings.ExtendedViewEnabled)
            {
                if (World.RenderingCamera != _extendedViewCamera)
                {
                    World.RenderingCamera = _extendedViewCamera;
                }

                var extraOffset = new Vector3(0, 0, 1f);
                if (Game.Player.Character.IsInVehicle())
                {
                    extraOffset = Game.Player.Character.IsInPlane ? new Vector3(0, 0, 3f) : new Vector3(0, 0, 2f);
                }

                ApplyCameraRotation(true);

                CalculateDistanceToCharacter(extraOffset);

                _extraOffset = Vector3.Lerp(_extraOffset, extraOffset, ExtraOffsetLerpScalar);

                ApplyCameraPosition(_extendedViewCamera, _extraOffset, false);
                ApplyCameraPosition(_forwardCamera, _extraOffset, false);
            }
            else
            {
                World.RenderingCamera = null;
            }
        }

        public void ProcessThirdPersonAim()
        {
            _aimTransitionState = Math.Max(0, _aimTransitionState - Time.unscaledDeltaTime / AimTransitionDuration);
            _isInThirdPersonAim = true;

            Game.Player.Character.IsVisible = true;

            if (!_gameState.IsSniperWeaponAndZoomed && _settings.ExtendedViewEnabled)
            {
                if (World.RenderingCamera != _extendedViewCamera)
                {
                    World.RenderingCamera = _extendedViewCamera;
                }

				if (_aimTransitionState > 0)
				{
					RotateGameplayCameraTowardsTarget();

					if (_aimAtGazeRequested)
					{
						//YawOffset = Geometry.BoundRotationDeg(GameplayCameraRotationFiltered.Z - _extendedViewCamera.Rotation.Z);
						//PitchOffset = Geometry.BoundRotationDeg(GameplayCameraRotationFiltered.X - _extendedViewCamera.Rotation.X);

                        //Quat
                        var extendedViewCameraRotationQinv = _extendedViewCameraRotationQ;
						extendedViewCameraRotationQinv.Invert();
						var diffQ = Quaternion.Multiply(extendedViewCameraRotationQinv, GameplayCameraRotationFilteredQ);
                        var diffRot = Geometry.QuaternionToGtaRotation(diffQ);
						YawOffset = diffRot.Z;
                        PitchOffset = diffRot.X;

                        _aimAtGazeRequested = false;
					}
				}

                ApplyCameraRotation(true);

                var extraOffset = new Vector3(0, 0, 1f);
                CalculateDistanceToCharacter(extraOffset);

                extraOffset += new Vector3(0.5f, 0, 0);

                _extraOffset = Vector3.Lerp(_extraOffset, extraOffset, ExtraOffsetLerpScalar);
                ApplyCameraPosition(_extendedViewCamera, _extraOffset, false);
                ApplyCameraPosition(_forwardCamera, _extraOffset, false);
            }
            else
            {
                World.RenderingCamera = null;
            }
        }

        public void ProcessFirstPersonVehicle()
        {
            _aimTransitionState = 1;
            _isInThirdPersonAim = false;
            _aimAtGazeRequested = false;
            _lastTarget = null;

            if (_settings.ExtendedViewEnabled)
            {
                Game.Player.Character.IsVisible = false;

                if (World.RenderingCamera != _extendedViewCamera)
                {
                    World.RenderingCamera = _extendedViewCamera;
                }

                //ScriptHookExtensions.CamInheritRollVehicle(_extendedViewCamera, Game.Player.Character.CurrentVehicle);

                ApplyCameraRotation(false);

                DistanceToCharacter = 0;

                if (Game.Player.Character.IsInPlane)
                {
                    //var extraOffset = new Vector3(0, 0, 0.6f);
                    //ApplyCameraPosition(_extendedViewCamera, extraOffset, true);
                    //ApplyCameraPosition(_forwardCamera, extraOffset, true);

                    var extraOffset = new Vector3(_headXFiltered * HeadPositionScalar, 0, 0.6f);
                    ApplyCameraPosition(_extendedViewCamera, extraOffset, false);
                    ApplyCameraPosition(_forwardCamera, extraOffset, false);
                }
                else
                {
                    var extraOffset = new Vector3(_headXFiltered * HeadPositionScalar, 0, 0.6f);
                    ApplyCameraPosition(_extendedViewCamera, extraOffset, false);
                    ApplyCameraPosition(_forwardCamera, extraOffset, false);
                }
            }
            else
            {
                Game.Player.Character.IsVisible = true;
                World.RenderingCamera = null;
            }
        }

        private void CalculateDistanceToCharacter(Vector3 extraOffset)
		{
			var attachPointPosition = Game.Player.Character.Position + extraOffset;
			var currentDistanceToCharacter = Vector3.Distance(GameplayCamera.Position, attachPointPosition);
			DistanceToCharacter = Math.Min(100, Mathf.Lerp(DistanceToCharacter, currentDistanceToCharacter, DistanceToCharacterLerpScalar));
		}

		private void ApplyCameraPosition(Camera camera, Vector3 extraOffset, bool isRelative)
	    {
	        var pitch = Mathf.Deg2Rad * camera.Rotation.X;
	        var yaw = Mathf.Deg2Rad * camera.Rotation.Z;
			if (camera == _extendedViewCamera)
			{
				pitch = Mathf.Deg2Rad * _extendedViewCameraRotation.X;
				yaw = Mathf.Deg2Rad * _extendedViewCameraRotation.Z;
			}

			var delta = new Vector3(extraOffset.X, (float) (DistanceToCharacter*-Math.Cos(pitch)),
	            (float) (DistanceToCharacter*-Math.Sin(pitch)));
	        delta.Z = Math.Max(delta.Z, -1f) + extraOffset.Z;

	        var extendedViewCameraOffset = delta;
	        extendedViewCameraOffset.X = (float) (Math.Cos(yaw)*delta.X - Math.Sin(yaw)*delta.Y);
	        extendedViewCameraOffset.Y = (float) (Math.Sin(yaw)*delta.X + Math.Cos(yaw)*delta.Y);

            //Quat DONE
            delta = new Vector3(0, -DistanceToCharacter, 0) + extraOffset;
            extendedViewCameraOffset = _extendedViewCameraRotationQ * delta;//_extendedViewCameraRotationQ.RotateTransform(extraOffset);
                                                                                  //extendedViewCameraOffset.Z = Math.Max(extendedViewCameraOffset.Z, -1f) + extraOffset.Z;

            ScriptHookExtensions.AttachCamToEntity(camera, Game.Player.Character, extendedViewCameraOffset, isRelative);
	    }


        private void ApplyCameraRotation(bool noRoll)
		{
            //var timeSince = DateTime.UtcNow - _lastAimCameraAtTargetTime;
            _debugOutput.DebugText1.Caption = _aimTransitionState.ToString();

            //         Vector3 rot;

            //if (noRoll)
            //   {
            //             rot = Geometry.OffsetRotation(GameplayCameraRotationFiltered, -Pitch, -Yaw);
            //             rot.Y = 0;
            //         }
            //         else
            //         {
            //             rot = Math.Abs(GameplayCameraRotationFiltered.Y) > 90 ? Geometry.OffsetRotation(GameplayCameraRotationFiltered, -Pitch, Yaw) : Geometry.OffsetRotation(GameplayCameraRotationFiltered, -Pitch, -Yaw);
            //         }

            //         rot = Geometry.BoundRotationDeg(rot);

            //_extendedViewCamera.Rotation = rot;
            //_extendedViewCameraRotation = rot;

            //_forwardCamera.Rotation = GameplayCameraRotationFiltered;

            //Quat DONE
            if (noRoll)
            {
                var extendedViewCameraRotationTemp = Geometry.QuaternionToGtaRotation(GameplayCameraRotationFilteredQ);
                extendedViewCameraRotationTemp.X += -Pitch;
                extendedViewCameraRotationTemp.Z += -Yaw;
                _extendedViewCameraRotationQ = Geometry.GtaRotationToQuaternion(extendedViewCameraRotationTemp);
            }
            else
            {
                var extraQ = Geometry.GtaRotationToQuaternion(new Vector3(-Pitch, 0, -Yaw));
                _extendedViewCameraRotationQ = GameplayCameraRotationFilteredQ * extraQ;
                _extendedViewCameraRotation = Geometry.QuaternionToGtaRotation(_extendedViewCameraRotationQ);
            }

            //_debugOutput.DebugText3.Caption = "x " + _extendedViewCameraRotation;
            //_debugOutput.DebugText4.Caption = "y " + _extendedViewCameraRotation; 
            //_debugOutput.DebugText5.Caption = "z " + _extendedViewCameraRotation;

            _extendedViewCamera.Rotation = _extendedViewCameraRotation;

            _forwardCamera.Rotation = Geometry.QuaternionToGtaRotation(GameplayCameraRotationFilteredQ);
        }

        private void RotateGameplayCameraTowardsTarget()
        {
            if (!_lastTarget.HasValue) return;

            var dir = _lastTarget.Value - _forwardCamera.Position;
            //_yawToTarget = Geometry.DirectionToRotation(dir).Z;
            //_pitchToTarget = Geometry.DirectionToRotation(dir).X;

            //quat
			_yawToTarget = Geometry.DirectionToRotation(dir).Z;
            _pitchToTarget = Geometry.DirectionToRotation(dir).X;

            Game.Player.Character.Heading = _yawToTarget;

            GameplayCamera.RelativeHeading = 0;
            GameplayCamera.ClampPitch(_pitchToTarget, _pitchToTarget);
            GameplayCamera.RelativePitch = _pitchToTarget;
            
            //GameplayCameraRotationFiltered = Geometry.DirectionToRotation(dir);

            //Quat
            GameplayCameraRotationFilteredQ = Geometry.GtaRotationToQuaternion(Geometry.DirectionToRotation(dir));
            //GameplayCameraRotationFilteredQ = MathR.XLookRotation(dir);

			var deltaPitch = _forwardCamera.Rotation.Z - _yawToTarget;
            var deltaYaw = _forwardCamera.Rotation.X - _pitchToTarget;
            var minAngle = 0.01f;
            if (Math.Abs(deltaYaw) < minAngle && Math.Abs(deltaPitch) < minAngle)
            {
                _lastTarget = null;
            }
        }

        private void FilterGameplayCameraRotation()
        {
            //var gameplayCameraRotation = GameplayCamera.Rotation;
            //var diff = gameplayCameraRotation - GameplayCameraRotationFiltered;
            //var boundDiff = Geometry.BoundRotationDeg(diff);
            //GameplayCameraRotationFiltered = GameplayCameraRotationFiltered + boundDiff * GameplayCameraFilteringScalar;
            //GameplayCameraRotationFiltered = Geometry.BoundRotationDeg(GameplayCameraRotationFiltered);

            //Quat DONE
            var gameplayCameraRotationQ = Geometry.GtaRotationToQuaternion(GameplayCamera.Rotation);
            GameplayCameraRotationFilteredQ = Quaternion.Lerp(GameplayCameraRotationFilteredQ, gameplayCameraRotationQ, GameplayCameraFilteringScalar);
		}

        private void FilterVehicleRotation()
        {
            var vehicle = Game.Player.Character.CurrentVehicle;
            if (vehicle == null) return;
            //var diff = vehicle.Rotation - VehicleRotationFiltered;
            //var boundDiff = Geometry.BoundRotationDeg(diff);
            //VehicleRotationFiltered = Geometry.BoundRotationDeg(VehicleRotationFiltered + boundDiff * GameplayCameraFilteringScalar);

            //Quat DONE
            var vehicleRotationQ = Geometry.GtaRotationToQuaternion(vehicle.Rotation);
			VehicleRotationFilteredQ = Quaternion.Lerp(VehicleRotationFilteredQ, vehicleRotationQ, GameplayCameraFilteringScalar);
		}

		public void ProcessFirstPerson()
		{
			//TODO
			Game.Player.Character.IsVisible = true;
			World.RenderingCamera = null;
		}

        public void ProcessFirstPersonAim()
        {
            //TODO
            Game.Player.Character.IsVisible = true;
            World.RenderingCamera = null;
        }

        public void Update()
        {
            LateUpdate();
        }

        protected override void UpdateSettings()
        {
            GazeViewResponsiveness = _settings.Responsiveness;
            //GazeViewMinimumExtensionAngleDegrees = 10 + EyeTrackingPlugin.SettingsManager.GetSettings().ExtendedViewSettings.GazeViewMinimumExtensionAngleDegrees * 20;
            HeadViewSensitivityScale = _settings.ExtendedViewSensitivity;
        }

        protected override void UpdateTransform()
        {
            FilterHeadPosition();

            FilterGameplayCameraRotation();
            FilterVehicleRotation();

            if (_settings.ExtendedViewEnabled)
		    {
		        //CameraRotationWithoutExtendedView = _forwardCamera.Rotation;
		        CameraPositionWithoutExtendedView = _forwardCamera.Position;

				//Quat DONE
                CameraRotationWithoutExtendedViewQ = Geometry.GtaRotationToQuaternion(_forwardCamera.Rotation);
            }
			else
		    {
                //CameraRotationWithoutExtendedView = GameplayCameraRotationFiltered;
				CameraPositionWithoutExtendedView = GameplayCamera.Position;

				//Quat DONE
				CameraRotationWithoutExtendedViewQ = GameplayCameraRotationFilteredQ;
			}


			if (Game.Player.Character.IsInVehicle())
			{
				if (Game.IsControlPressed(Control.NextCamera))
				{
					_lastNotInVehicle = DateTime.UtcNow;
				}

				//vehicle
				var timeInVehicle = DateTime.UtcNow - _lastNotInVehicle;
				if ((timeInVehicle > TimeSpan.FromSeconds(0.5))
					&& _settings.FirstPersonModeEnabled)
				{
					ProcessFirstPersonVehicle();
				}
				else
				{
					ProcessThirdPerson();
				}
			}
			else
			{
				_lastNotInVehicle = DateTime.UtcNow;

                //on foot
                if (_settings.FirstPersonModeEnabled && GameState.IsFirstPersonPedCameraActive())
                {
                    if (_gameState.IsAimingWithGamepad || _gameState.IsAimingWithMouse)
                    {
                        ProcessFirstPersonAim();
                    }
                    else
                    {
                        ProcessFirstPerson();
                    }
                }
                else
                {
                    if (_gameState.IsAimingWithGamepad || _gameState.IsAimingWithMouse)
                    {
                        ProcessThirdPersonAim();
                    }
                    else
                    {
                        ProcessThirdPerson();
                    }
                }

            }

            IsAiming = _isInThirdPersonAim;
        }

        private void FilterHeadPosition()
        {
            _headXFiltered = Mathf.Lerp(_headXFiltered, TobiiAPI.GetHeadPose().Position.X, HeadPositionSensitivity);
        }

        public void AimCameraAtTarget(Vector3 target)
        {
            var centeredNormalizedGaze = new Vector2(TobiiAPI.GetGazePoint().X, TobiiAPI.GetGazePoint().Y) * 2 -
                                         new Vector2(1, 1);

            if (Vector2.Distance(centeredNormalizedGaze, _aiming.CrosshairPosition) < AimAtCrosshairDeadzoneSize) return;

            _lastTarget = target;
            _aimAtGazeRequested = true;
        }

        public void Dispose()
        {
            World.RenderingCamera = null;
            World.DestroyAllCameras();
        }
    }
}
