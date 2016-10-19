using System;
using System.Windows.Forms;
using Gta5EyeTracking.Features;
using Gta5EyeTracking.HidEmulation;
using Gta5EyeTracking.Menu;
using GTA;
using GTA.Math;
using SharpDX.XInput;

namespace Gta5EyeTracking
{
	public class ControlsProcessor: IDisposable
	{
		private readonly Settings _settings;
		private readonly ControllerEmulation _controllerEmulation;
		private readonly Aiming _aiming;
		private readonly ExtendedView _extendedView;
		private readonly RadialMenu _radialMenu;
		private readonly SettingsMenu _settingsMenu;
		private readonly GameState _gameState;
		private DateTime _lastTickTime;
		private readonly DebugOutput _debugOutput;
        private bool _shutDownRequestFlag;
        private bool _lastAimCameraAtTarget;

        public ControlsProcessor(Settings settings, 
			ControllerEmulation controllerEmulation, 
			Aiming aiming, 
			ExtendedView extendedView, 
			RadialMenu radialMenu, 
			SettingsMenu settingsMenu, 
            GameState gameState,
			DebugOutput debugOutput)
		{
			_settings = settings;
			_controllerEmulation = controllerEmulation;
			_aiming = aiming;
			_extendedView = extendedView;
			_radialMenu = radialMenu;
			_settingsMenu = settingsMenu;
		    _gameState = gameState;

		    _debugOutput = debugOutput;
			_controllerEmulation.OnModifyState += OnModifyControllerState;
		}

		public void Dispose()
		{
			if (_controllerEmulation != null)
			{
				_shutDownRequestFlag = true;
                _controllerEmulation.OnModifyState -= OnModifyControllerState;
			}
		}

		public void Update(DateTime tickStopwatch, Vector3 shootCoord, Vector3 shootCoordSnap, Vector3 shootMissileCoord, Ped ped, Entity missileTarget)
		{
			_lastTickTime = tickStopwatch;
			

            if (_gameState.IsInCharacterSelectionMenu)
			{
                //Reset
                _controllerEmulation.DeltaX = 0;
                _controllerEmulation.DeltaY = 0;
            }
			else if (_gameState.IsInRadialMenu)
			{
                _radialMenu.Update();
			}
			else
            {
                //Reset
                _controllerEmulation.DeltaX = 0;
                _controllerEmulation.DeltaY = 0;

                _extendedView.Update();
			}

			ProcessSettingsMenu();

			ProcessAimAtGaze(_settings.SnapAtTargetsEnabled ? shootCoordSnap : shootCoord);

			//Place crosshair in the center of the screen in some cases
			var gazeShootCoord = shootCoord;
			var crosshairCoord = ProcessShootCoord(shootCoord);
		    var isShootAtCenter = gazeShootCoord == shootCoord;

			ProcessFireAtGaze(crosshairCoord, gazeShootCoord);

			ProcessIncinerateAtGaze(shootCoordSnap);

			ProcessTaseAtGaze(shootCoordSnap);

			ProcessMissileAtGaze(shootMissileCoord, missileTarget);

			_aiming.Update(crosshairCoord, missileTarget, isShootAtCenter);
		}



	    private void ProcessMissileAtGaze(Vector3 shootMissileCoord, Entity missileTarget)
	    {
			if (!_settings.MissilesAtGazeEnabled) return;

			if (!(shootMissileCoord.Length() > 0) || !Geometry.IsInFrontOfThePlayer(shootMissileCoord)) return;

		    var controllerState = _controllerEmulation.ControllerState;

		    if (Game.IsKeyPressed(Keys.N)
				|| Game.IsKeyPressed(Keys.PageUp)
				|| (!_gameState.IsInCharacterSelectionMenu && !_gameState.IsInRadialMenu && !_gameState.IsMenuOpen && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.B)))
		    {
			    if (missileTarget != null)
			    {
				    _aiming.ShootMissile(missileTarget);
			    }
			    else
			    {
				    _aiming.ShootMissile(shootMissileCoord);
			    }
		    }
	    }

	    private void ProcessFireAtGaze(Vector3 shootCoord, Vector3 gazeShootCoord)
	    {
			if (!(shootCoord.Length() > 0) || !Geometry.IsInFrontOfThePlayer(shootCoord)) return;

			var controllerState = _controllerEmulation.ControllerState;

		    if (!_gameState.IsInVehicle
				&& !Geometry.IsFirstPersonPedCameraActive()
				&& !_gameState.IsSniperWeaponAndZoomed
				&& !_gameState.IsThrowableWeapon
				&& !_gameState.IsMeleeWeapon)
		    {
			    if (_gameState.IsShootingWithGamepad
					|| _gameState.IsShootingWithMouse)
			    {
					_aiming.Shoot(shootCoord);
				}
				else if (Game.IsKeyPressed(Keys.B)
					|| (User32.IsKeyPressed(VirtualKeyStates.VK_XBUTTON1)))

				{ 
					_aiming.Shoot(gazeShootCoord);
				}
			}

		    if (_gameState.IsInVehicle)
		    {
				if ((!_gameState.IsMenuOpen && User32.IsKeyPressed(VirtualKeyStates.VK_LBUTTON))
					|| (!_gameState.IsInAircraft && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder)))
			    {
					_aiming.ShootBullet(gazeShootCoord);
					//TODO: separate flag in settings
				}
				else if (Game.IsKeyPressed(Keys.B)
					|| User32.IsKeyPressed(VirtualKeyStates.VK_XBUTTON1))
			    {
					_aiming.ShootBullet(gazeShootCoord);
				}
		    }
	    }

	    private Vector3 ProcessShootCoord(Vector3 shootCoord)
	    {
	        if (!_gameState.IsInVehicle &&
                (!_settings.FireAtGazeEnabled
	            || _gameState.IsMeleeWeapon
				|| _gameState.IsThrowableWeapon
				|| _gameState.IsSniperWeaponAndZoomed
				|| _gameState.IsAimingWithMouse
				|| _gameState.IsShootingWithMouse))
	        {
	            var source3D = _extendedView.CameraPositionWithoutExtendedView;
	            var rotation = _extendedView.CameraRotationWithoutExtendedView;
	            var dir = Geometry.RotationToDirection(rotation);
	            var target3D = source3D + dir*1000;

	            Entity hitEntity;
	            shootCoord = Geometry.RaycastEverything(out hitEntity, target3D, source3D);
	        }
	        return shootCoord;
	    }

	    private void ProcessAimAtGaze(Vector3 aimCoord)
	    {
	        if (_settings.AimAtGazeEnabled
	            && !_gameState.IsInVehicle
                && (_gameState.IsAimingWithGamepad || _gameState.IsAimingWithMouse))
	        {
	            if (!_lastAimCameraAtTarget)
	            {
		            _extendedView.AimCameraAtTarget(aimCoord);
				}
	            _lastAimCameraAtTarget = true;
	        }
	        else
	        {
	            _lastAimCameraAtTarget = false;
	        }
	    }

	    private void ProcessSettingsMenu()
	    {
            var controllerState = _controllerEmulation.ControllerState;
            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftThumb)
	            && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Start)
	            && !_gameState.IsMenuOpen)
	        {
	            _settingsMenu.OpenMenu();
	        }
	    }

	    private void ProcessTaseAtGaze(Vector3 shootCoordSnap)
	    {
		    if (!_settings.TaseAtGazeEnabled) return;

            var controllerState = _controllerEmulation.ControllerState;
            if (Game.IsKeyPressed(Keys.H)
	            || Game.IsKeyPressed(Keys.PageDown)
	            || (!_gameState.IsInAircraft && !_gameState.IsMenuOpen && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder)))
	        {
	            _aiming.Tase(shootCoordSnap);
	        }
	    }

	    private void ProcessIncinerateAtGaze(Vector3 shootCoordSnap)
	    {
			if (!_settings.IncinerateAtGazeEnabled) return;

			var controllerState = _controllerEmulation.ControllerState;
            if (Game.IsKeyPressed(Keys.J)
	            || User32.IsKeyPressed(VirtualKeyStates.VK_XBUTTON2)
	            || (!_gameState.IsMenuOpen && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A)))
	        {
	            _aiming.Incinerate(shootCoordSnap);
	        }
	    }



	    private void OnModifyControllerState(object sender, ModifyStateEventArgs modifyStateEventArgs)
		{
			if (_shutDownRequestFlag) return;
			if (_gameState.IsPaused) return;
			var timePausedThershold = TimeSpan.FromSeconds(0.5);
			if (DateTime.UtcNow - _lastTickTime > timePausedThershold) return;

			var state = modifyStateEventArgs.State;

			var disableA = false;
			var disableB = false;
			var disableLeftShoulder = false;
			var disableRightShoulder = false;
			var disableLeftThumb = false;
			var disableRightStick = false;
			var disableStart = false;
			var disableRightTrigger = false;

			if (_gameState.IsInVehicle)
			{
				if (_gameState.IsInAircraft)
				{
					if (_settings.MissilesAtGazeEnabled) disableB = true;
					if (_settings.IncinerateAtGazeEnabled) disableA = true;
				}
				else
				{
					if (_settings.FireAtGazeEnabled) disableLeftShoulder = true;
					if (_settings.MissilesAtGazeEnabled) disableB = true;
					if (_settings.TaseAtGazeEnabled) disableRightShoulder = true;
					if (_settings.IncinerateAtGazeEnabled) disableA = true;
				}
			}
			else
			{
				if (_settings.FireAtGazeEnabled)
				{
					disableLeftThumb = true;
					if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftThumb))
					{
						disableRightStick = true;
					}
					if (!_gameState.IsPaused
					    && !_gameState.IsInRadialMenu
						&& !_gameState.IsMeleeWeapon
						&& !_gameState.IsThrowableWeapon
						&& !_gameState.IsSniperWeaponAndZoomed)
					{
						disableRightTrigger = true;
					}
				}
				if (_settings.MissilesAtGazeEnabled) disableB = true;
				if (_settings.TaseAtGazeEnabled) disableRightShoulder = true;
				if (_settings.IncinerateAtGazeEnabled) disableA = true;
			}

			//Toggle menu
			if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftThumb)
				&& state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Start))
			{
				disableStart = true;
				disableLeftThumb = true;
			}

			if (_gameState.IsMenuOpen)
			{
				disableA = false;
				disableB = false;
				//disableStart = false;
			}

			if (disableRightTrigger)
			{
				state.Gamepad.RightTrigger = 0;
			}

			if (disableStart && state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Start))
			{
				state.Gamepad.Buttons &= ~GamepadButtonFlags.Start;
			}

			if (disableLeftThumb && state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftThumb))
			{
				state.Gamepad.Buttons &= ~GamepadButtonFlags.LeftThumb;
			}

			if (disableRightStick)
			{
				state.Gamepad.RightThumbX = 0;
				state.Gamepad.RightThumbY = 0;
			}

			if (disableLeftShoulder && state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder))
			{
				state.Gamepad.Buttons &= ~GamepadButtonFlags.LeftShoulder;
			}
			if (disableRightShoulder && state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder))
			{
				state.Gamepad.Buttons &= ~GamepadButtonFlags.RightShoulder;
			}

			if (disableA && state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
			{
				state.Gamepad.Buttons &= ~GamepadButtonFlags.A;
			}

			if (disableB && state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.B))
			{
				state.Gamepad.Buttons &= ~GamepadButtonFlags.B;
			}

			modifyStateEventArgs.State = state;
		}

		public void KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.K)
			{
				_debugOutput.Visible = !_debugOutput.Visible;
			}

			if (e.KeyCode == Keys.F8)
			{
				if (!_gameState.IsMenuOpen)
				{
					_settingsMenu.OpenMenu();
				}
				else
				{
					_settingsMenu.CloseMenu();
				}
			}
		}
	}
}
