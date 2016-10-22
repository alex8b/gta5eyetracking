using System;
using GTA;
using GTA.Math;
using Control = GTA.Control;

namespace Gta5EyeTracking.Features
{
	public class ExtendedView: IDisposable
	{
        public Vector3 CameraPositionWithoutExtendedView { get; private set; }
        public Vector3 CameraRotationWithoutExtendedView { get; private set; }
        public Vector3 GameplayCameraRotationFiltered { get; private set; }
        public Vector3 VehicleRotationFiltered { get; private set; }

        //Head
        private const float HeadPositionSensitivity = 0.5f;
		private const float HeadRotationSensitivity = 0.1f;
		private const float HeadRotationScalar = Mathf.Rad2Deg * 0.5f;
		private const float HeadPositionScalar = 0.001f;
        private const float HeadRotationDeadZoneDeg = 0;

        //Gaze
        private const float AimAtCrosshairDeadzoneSize = 0.05f;
		private const float ViewCenteringDeadzoneSize = 0.1f;
        private const float MaxEyeLeftYaw = 35.0f;
        private const float MaxEyeRightYaw = 35.0f;
        private const float MaxEyeUpPitch = 25.0f;
        private const float MaxEyeDownPitch = 10.0f;
        private const float Responsiveness = 0.7f;
	    private const bool ScaleScreenShiftByBasePitch = true;

        private const float SensitivityGradientScale = 0.5f;
        private const float SensitivityGradientDegree = 2.5f;
        private const float SensitivityGradientFalloffPoint = 0.7f;


        private const float TimeDeltaConstant = 0.015f;
        private const float GameplayCameraFilteringScalar = 0.1f;
        private const float SensitivityScalar = 20;
        private const float AimTransitionDuration = 1f;
	    private const float AimTransitionLerpScalar = 0.25f;

        private const float ExtraOffsetLerpScalar = 0.1f;
        private const float DistanceToCharacterLerpScalar = 0.1f;

        private readonly Settings _settings;
	    private readonly GameState _gameState;
	    private readonly Aiming _aiming;
	    private readonly DebugOutput _debugOutput;
	    private readonly ITobiiTracker _tobiiTracker;

		private float _headYawFiltered;
		private float _headPitchFiltered;
		private float _headXFiltered;
		private float _headYFiltered;
		private float _gazeYaw;
        private float _gazePitch;

	    private Vector2 _infiniteScreenViewTargetRel;

        private readonly Camera _extendedViewCamera;
        private readonly Camera _forwardCamera;

		private DateTime _lastAimCameraAtTargetTime;
		private DateTime _lastNotInVehicle;
		private float _distanceToCharacter;
        
		private float _aimTransitionState;
		private float _scaleGazePitch;
		private float _scaleGazeYaw;
		private Vector3 _extraOffset;

		private Vector3? _lastTarget;
		private float _pitchToTarget;
		private float _yawOffset;
		private float _pitchOffset;
        private float _yawToTarget;
	    private bool _isInThirdPersonAim;


	    public ExtendedView(Settings settings,
            GameState gameState,
			ITobiiTracker tobiiTracker,
            Aiming aiming,
            DebugOutput debugOutput
            )
		{
			_settings = settings;
		    _gameState = gameState;
		    _aiming = aiming;
		    _debugOutput = debugOutput;
		    _tobiiTracker = tobiiTracker;

			_aimTransitionState = 1;

            _extendedViewCamera = World.CreateCamera(new Vector3(), Vector3.Zero, 60f);
            _forwardCamera = World.CreateCamera(new Vector3(), Vector3.Zero, 60f);
        }

		public void ProcessThirdPerson()
		{
			_aimTransitionState = Math.Min(1, _aimTransitionState + TimeDeltaConstant / AimTransitionDuration);
            _isInThirdPersonAim = false;

            Game.Player.Character.IsVisible = true;

			if (!_gameState.IsSniperWeaponAndZoomed && _settings.ExtendedViewEnabled)
			{
				if (World.RenderingCamera != _extendedViewCamera)
				{
					World.RenderingCamera = _extendedViewCamera;
				}

                if (_tobiiTracker.IsHeadTracking)
                {
                    _scaleGazePitch = Math.Abs(_headPitchFiltered);
                    _scaleGazeYaw = Math.Abs(_headYawFiltered);
                }
                else
                {
                    _scaleGazePitch = 1;
					_scaleGazeYaw = 1;
                }

                var extraOffset = new Vector3(0, 0, 1f);
				if (Game.Player.Character.IsInVehicle())
				{
                    extraOffset = Game.Player.Character.IsInPlane ? new Vector3(0, 0, 3f) : new Vector3(0, 0, 2f);
					_scaleGazePitch = 1;
					_scaleGazeYaw = 1;
				}

				ProcessExtendedView(true);

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
            _aimTransitionState = Math.Max(0, _aimTransitionState - TimeDeltaConstant / AimTransitionDuration);
            _isInThirdPersonAim = true;

            Game.Player.Character.IsVisible = true;

            if (!_gameState.IsSniperWeaponAndZoomed && _settings.ExtendedViewEnabled)
            {
                if (_tobiiTracker.IsHeadTracking)
                {
                    _scaleGazePitch = Math.Abs(_headPitchFiltered);
                    _scaleGazeYaw = Math.Abs(_headYawFiltered);
                }
                else
                {
                    _scaleGazePitch = 1;
                    _scaleGazeYaw = 1;
                }

                if (World.RenderingCamera != _extendedViewCamera)
                {
                    World.RenderingCamera = _extendedViewCamera;
                }

                ProcessExtendedView(true);

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

            if (_settings.ExtendedViewEnabled)
            {
                Game.Player.Character.IsVisible = false;

                if (World.RenderingCamera != _extendedViewCamera)
                {
                    World.RenderingCamera = _extendedViewCamera;
                }

                //ScriptHookExtensions.CamInheritRollVehicle(_extendedViewCamera, Game.Player.Character.CurrentVehicle);

                _scaleGazePitch = 1;
                _scaleGazeYaw = 1;

                ProcessExtendedView(false);

                _distanceToCharacter = 0;

                if (Game.Player.Character.IsInPlane)
                {
                    var extraOffset = new Vector3(0, 0, 0.6f);
                    ApplyCameraPosition(_extendedViewCamera, extraOffset, true);
                    ApplyCameraPosition(_forwardCamera, extraOffset, true);
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
			_distanceToCharacter = Math.Min(100, Mathf.Lerp(_distanceToCharacter, currentDistanceToCharacter, DistanceToCharacterLerpScalar));
		}

		private void ApplyCameraPosition(Camera camera, Vector3 extraOffset, bool isRelative)
	    {
	        var pitch = Mathf.Deg2Rad * camera.Rotation.X;
	        var yaw = Mathf.Deg2Rad * camera.Rotation.Z;

	        var delta = new Vector3(extraOffset.X, (float) (_distanceToCharacter*-Math.Cos(pitch)),
	            (float) (_distanceToCharacter*-Math.Sin(pitch)));
	        delta.Z = Math.Max(delta.Z, -1f) + extraOffset.Z;

	        var extendedViewCameraOffset = delta;
	        extendedViewCameraOffset.X = (float) (Math.Cos(yaw)*delta.X - Math.Sin(yaw)*delta.Y);
	        extendedViewCameraOffset.Y = (float) (Math.Sin(yaw)*delta.X + Math.Cos(yaw)*delta.Y);

	        ScriptHookExtensions.AttachCamToEntity(camera, Game.Player.Character, extendedViewCameraOffset, isRelative);
	    }


        public float EaseInEaseOutTransform(float time, float a, float b, float duration)
        {
            time /= duration / 2;
            if (time < 1) return b / 2 * time * time + a;
            time--;
            return -b / 2 * (time * (time - 2) - 1) + a;
        }

        public float SensitivityTransform(float value)
		{
			var sign = Math.Sign(value);
			var x = Math.Min(Math.Max(0.0f, Math.Abs(value)), 1.0f);

			var a = SensitivityGradientDegree;
			var b = 1 / SensitivityGradientFalloffPoint;

			var t = Math.Min(Math.Floor(b * x), 1);
			return (float) (sign * ((1 - t) * (Math.Pow(b * x, a) / b) + t * (1 - (Math.Pow((b / (b - 1)) * (1 - x), a) / (b / (b - 1))))) * SensitivityGradientScale);
		}

		private void ProcessExtendedView(bool noRoll)
		{
            UpdateViewTarget(new Vector2(_tobiiTracker.GazeX, _tobiiTracker.GazeY));
            UpdateInfiniteScreenAngles();

            ApplyCameraRotation(noRoll);
		}

		private void ApplyCameraRotation(bool noRoll)
		{
            //var timeSince = DateTime.UtcNow - _lastAimCameraAtTargetTime;
		    var lerpScalar = 1f;
		    _debugOutput.DebugText1.Caption = _aimTransitionState.ToString();

            if (_isInThirdPersonAim)
			{
			    if (_aimTransitionState > 0)
			    {
			        lerpScalar = AimTransitionLerpScalar;
			        RotateGameplayCameraTowardsTarget();
			    }
                _pitchOffset = 0;
			    _yawOffset = 0;
			}
			else
            {
                _lastTarget = null;
			    if (_aimTransitionState < 1)
			    {
                    lerpScalar = AimTransitionLerpScalar;
                }
                _pitchOffset = -_gazePitch * _scaleGazePitch - _headPitchFiltered * HeadRotationScalar;
                _yawOffset = -_gazeYaw * _scaleGazeYaw + Math.Sign(_headYawFiltered) * Math.Max(0, Math.Abs(_headYawFiltered * HeadRotationScalar) - HeadRotationDeadZoneDeg);
            }

		    Vector3 rot;

		    if (noRoll)
		    {
                rot = Geometry.OffsetRotation(GameplayCameraRotationFiltered, _pitchOffset, _yawOffset);
                rot.Y = 0;
		    }
		    else
		    {
                rot = Math.Abs(GameplayCameraRotationFiltered.Y) > 90 ? Geometry.OffsetRotation(GameplayCameraRotationFiltered, _pitchOffset, -_yawOffset) : Geometry.OffsetRotation(GameplayCameraRotationFiltered, _pitchOffset, _yawOffset);
            }

            rot = Geometry.BoundRotationDeg(rot);

            var deltaRot = rot - _extendedViewCamera.Rotation;
            var deltaRotBound = Geometry.BoundRotationDeg(deltaRot);

            _extendedViewCamera.Rotation = _extendedViewCamera.Rotation + deltaRotBound * lerpScalar;
            _forwardCamera.Rotation = GameplayCameraRotationFiltered;
        }

	    private void RotateGameplayCameraTowardsTarget()
	    {
		    if (!_lastTarget.HasValue) return;

	        var dir = _lastTarget.Value - _forwardCamera.Position;
	        _yawToTarget = Geometry.DirectionToRotation(dir).Z;
	        _pitchToTarget = Geometry.DirectionToRotation(dir).X;

	        Game.Player.Character.Heading = _yawToTarget;

	        GameplayCamera.RelativeHeading = 0;
	        GameplayCamera.ClampPitch(_pitchToTarget, _pitchToTarget);
	        GameplayCamera.RelativePitch = _pitchToTarget;
	        GameplayCameraRotationFiltered = Geometry.DirectionToRotation(dir);

	        var deltaPitch = _forwardCamera.Rotation.Z - _yawToTarget;
            var deltaYaw = _forwardCamera.Rotation.X - _pitchToTarget;
	        var minAngle = 0.01f;
            if (Math.Abs(deltaYaw) < minAngle && Math.Abs(deltaPitch) < minAngle)
            {
                _lastTarget = null;
            }
        }

        //This function will lerp the view target closer to the gaze point
        private void UpdateViewTarget(Vector2 normalizedCenteredGazeCoordinates)
        {
            normalizedCenteredGazeCoordinates.X = Mathf.Clamp(normalizedCenteredGazeCoordinates.X, -1.0f, 1.0f);
            normalizedCenteredGazeCoordinates.Y = Mathf.Clamp(normalizedCenteredGazeCoordinates.Y, -1.0f, 1.0f);

            //Track view target towards gaze point according to our curve function
            Vector2 viewTargetToGazeDelta = normalizedCenteredGazeCoordinates - _infiniteScreenViewTargetRel;
            var normalizedViewTargetDistance = Mathf.Clamp(viewTargetToGazeDelta.Length(), 0.0f, 1.0f);
            var viewTargetStepSize = SensitivityTransform(normalizedViewTargetDistance);
            Vector2 viewTargetToGazeDirection = viewTargetToGazeDelta;
            viewTargetToGazeDirection.Normalize();

            _infiniteScreenViewTargetRel.X += viewTargetToGazeDirection.X * viewTargetStepSize * _settings.ExtendedViewSensitivity * SensitivityScalar * TimeDeltaConstant;
            _infiniteScreenViewTargetRel.Y += viewTargetToGazeDirection.Y * viewTargetStepSize * _settings.ExtendedViewSensitivity * SensitivityScalar * TimeDeltaConstant;

            //If you are at an extreme view angle and quickly want to return to the center, it is nice to be able to look at a given stimulus (usually the crosshair)
            bool userLookingInCenteringDeadzone = !(_aiming.IsCrosshairVisible && _aiming.IsCrosshairAtCenter) ? normalizedCenteredGazeCoordinates.Length() < ViewCenteringDeadzoneSize
                 : Vector2.Distance(normalizedCenteredGazeCoordinates,_aiming.CrosshairPosition) < ViewCenteringDeadzoneSize;

            if (userLookingInCenteringDeadzone)
            {
                _infiniteScreenViewTargetRel.X = Mathf.Lerp(_infiniteScreenViewTargetRel.X, 0.0f, _settings.ExtendedViewSensitivity * SensitivityScalar * TimeDeltaConstant);
                _infiniteScreenViewTargetRel.Y = Mathf.Lerp(_infiniteScreenViewTargetRel.Y, 0.0f, _settings.ExtendedViewSensitivity * SensitivityScalar * TimeDeltaConstant);
            }
        }

        //This function will translate the current view target to target orientation angles and lerp the camera orientation towards it.
        private void UpdateInfiniteScreenAngles()
        {
            //Translate gaze offset to angles along our curve
            float _maxYawToUse = _infiniteScreenViewTargetRel.X >= 0 ? MaxEyeRightYaw : MaxEyeLeftYaw;
            float _targetYaw = Mathf.Clamp(_infiniteScreenViewTargetRel.X * _maxYawToUse, -MaxEyeLeftYaw, MaxEyeRightYaw);

            float _maxPitchToUse = _infiniteScreenViewTargetRel.Y >= 0 ? MaxEyeDownPitch : MaxEyeUpPitch;
            float _targetPitch = Mathf.Clamp(_infiniteScreenViewTargetRel.Y * _maxPitchToUse, -MaxEyeUpPitch, MaxEyeDownPitch);
            if (ScaleScreenShiftByBasePitch)
            {
            //TODO
            //    float cameraPitchWithin90Range = transform.parent.localRotation.eulerAngles.x > 90 ?
            //        transform.parent.localRotation.eulerAngles.x - 360 : transform.parent.localRotation.eulerAngles.x;
            //    float pitchShiftMinus1To1 = cameraPitchWithin90Range / 90.0f;
            //    float pitchShift01 = (pitchShiftMinus1To1 + 1) / 2.0f;
            //    float pitchShiftScale = AmountOfScreenShiftDependingOnCameraPitch.Evaluate(pitchShift01);
            //    _targetYaw *= pitchShiftScale;
            }

            //Rotate current angles toward our target angles
            //Please note that depending on preference, a slerp here might be a better fit because of angle spacing errors when using lerp with angles
            _gazeYaw = Mathf.LerpAngle(_gazeYaw, _targetYaw, _settings.ExtendedViewSensitivity * SensitivityScalar * Responsiveness * TimeDeltaConstant);
            _gazePitch = Mathf.LerpAngle(_gazePitch, _targetPitch, _settings.ExtendedViewSensitivity * SensitivityScalar * Responsiveness * TimeDeltaConstant);
        }

        private void FilterGameplayCameraRotation()
		{
			var gameplayCameraRotation = GameplayCamera.Rotation;
			var diff = gameplayCameraRotation - GameplayCameraRotationFiltered;
			var boundDiff = Geometry.BoundRotationDeg(diff);
			GameplayCameraRotationFiltered = GameplayCameraRotationFiltered + boundDiff*GameplayCameraFilteringScalar;
            GameplayCameraRotationFiltered = Geometry.BoundRotationDeg(GameplayCameraRotationFiltered);

        }

        private void FilterVehicleRotation()
        {
            var vehicle = Game.Player.Character.CurrentVehicle;
            if (vehicle == null) return;
            var characterRotation = vehicle.Rotation;
            var diff = characterRotation - VehicleRotationFiltered;
            var boundDiff = Geometry.BoundRotationDeg(diff);
            VehicleRotationFiltered = Geometry.BoundRotationDeg(VehicleRotationFiltered + boundDiff * GameplayCameraFilteringScalar);
        }

	    public void ProcessFirstPerson()
		{
			//TODO
			Game.Player.Character.IsVisible = true;

			_gazeYaw = 0;
			_gazePitch = 0;
			World.RenderingCamera = null;
		}

		public void ProcessFirstPersonAim()
		{
            //TODO
            Game.Player.Character.IsVisible = true;

			_gazeYaw = 0;
            _gazePitch = 0;
			World.RenderingCamera = null;
		}

		public void Update()
		{
			FilterHeadPos();

			FilterGameplayCameraRotation();
		    FilterVehicleRotation();

            if (_settings.ExtendedViewEnabled)
		    {
		        CameraRotationWithoutExtendedView = _forwardCamera.Rotation;
		        CameraPositionWithoutExtendedView = _forwardCamera.Position;
		    }
		    else
		    {
                CameraRotationWithoutExtendedView = GameplayCameraRotationFiltered;
                CameraRotationWithoutExtendedView = GameplayCamera.Position;
            }


            if (Game.Player.Character.IsInVehicle())
			{
				if (Game.IsControlPressed(0, Control.NextCamera))
				{
					_lastNotInVehicle = DateTime.UtcNow;
				}

				//vehicle
				var timeInVehicle = DateTime.UtcNow - _lastNotInVehicle;
				if ((timeInVehicle > TimeSpan.FromSeconds(0.5))
					&& GameState.IsFirstPersonVehicleCameraActive())
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
                if (GameState.IsFirstPersonPedCameraActive())
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
		}

		private void FilterHeadPos()
		{
			_headYawFiltered = Mathf.Lerp(_headYawFiltered, _tobiiTracker.Yaw, HeadRotationSensitivity);
			_headPitchFiltered = Mathf.Lerp(_headPitchFiltered, -_tobiiTracker.Pitch, HeadRotationSensitivity);
			_headXFiltered = Mathf.Lerp(_headXFiltered, _tobiiTracker.X, HeadPositionSensitivity);
			_headYFiltered = Mathf.Lerp(_headYFiltered, _tobiiTracker.Y, HeadPositionSensitivity);
		}

		public void AimCameraAtTarget(Vector3 target)
		{
		    if (Vector2.Distance(new Vector2(_tobiiTracker.GazeX, _tobiiTracker.GazeY), _aiming.CrosshairPosition) <
				AimAtCrosshairDeadzoneSize) return;

            _lastTarget = target;
		    _lastAimCameraAtTargetTime = DateTime.UtcNow;
		}

		public void Dispose()
		{
			World.RenderingCamera = null;
			World.DestroyAllCameras();
		}
	}
}
