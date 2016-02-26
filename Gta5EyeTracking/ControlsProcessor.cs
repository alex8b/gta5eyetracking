using System;
using System.Diagnostics;
using System.Windows.Forms;
using Gta5EyeTracking.Features;
using Gta5EyeTracking.HidEmulation;
using Gta5EyeTracking.Menu;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using SharpDX.XInput;
using Tobii.EyeX.Client;

namespace Gta5EyeTracking
{
	public class ControlsProcessor: DisposableBase
	{
		private readonly Settings _settings;
		private readonly ControllerEmulation _controllerEmulation;
		private readonly Aiming _aiming;
		private readonly Freelook _freelook;
		private readonly RadialMenu _radialMenu;
		private readonly SettingsMenu _settingsMenu;
		private readonly MenuPool _menuPool;
		private DateTime _lastTickTime;
		private readonly DebugOutput _debugOutput;


		private bool _isPaused;
		private bool _isInVehicle;
		private bool _isInAircraft;
		private bool _isInRadialMenu;

		private int _injectRightTrigger;
		private bool _menuOpen;
		private bool _shutDownRequestFlag;
		private bool _isMeleeWeapon;
		private bool _isThrowableWeapon;


		public ControlsProcessor(Settings settings, 
			ControllerEmulation controllerEmulation, 
			Aiming aiming, 
			Freelook freelook, 
			RadialMenu radialMenu, 
			SettingsMenu settingsMenu, 
			MenuPool menuPool, 
			DebugOutput debugOutput)
		{
			_settings = settings;
			_controllerEmulation = controllerEmulation;
			_aiming = aiming;
			_freelook = freelook;
			_radialMenu = radialMenu;
			_settingsMenu = settingsMenu;
			_menuPool = menuPool;
			_debugOutput = debugOutput;
			_controllerEmulation.OnModifyState += OnModifyControllerState;
		}

		protected override void Dispose(bool disposing)
		{
			if (_controllerEmulation != null)
			{
				_shutDownRequestFlag = true;
                _controllerEmulation.OnModifyState -= OnModifyControllerState;
			}
		}

		public void Process(DateTime tickStopwatch, Vector2 gazePoint, double aspectRatio, Vector3 shootCoord, Vector3 shootCoordSnap, Vector3 shootMissileCoord, Ped ped, Entity missileTarget)
		{
			_lastTickTime = tickStopwatch;
			_isPaused = Game.IsPaused;
			_isInVehicle = Game.Player.Character.IsInVehicle();
			_isInAircraft = Game.Player.Character.IsInPlane || Game.Player.Character.IsInHeli;
			_isMeleeWeapon = Util.IsMelee(Game.Player.Character.Weapons.Current.Hash);
			_isThrowableWeapon = Util.IsThrowable(Game.Player.Character.Weapons.Current.Hash);

            _menuOpen = _menuPool.IsAnyMenuOpen();

			var controllerState = _controllerEmulation.ControllerState;

			if (!_menuOpen
				&& (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadDown)
				|| User32.IsKeyPressed(VirtualKeyStates.VK_LMENU)))
			{
				//character selection
			}
			else if (!_isInVehicle && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder))
			{
				_radialMenu.Process(gazePoint, aspectRatio);
			}
			else
			{
				_freelook.Process(gazePoint, ped, aspectRatio);
			}

			_isInRadialMenu = (!Game.Player.Character.IsInVehicle()
								&& controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder));

			if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftThumb)
				&& controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Start)
				&& !_menuOpen)
			{
				_settingsMenu.OpenMenu();
			}

			if (shootCoord.Length() > 0 && Geometry.IsInFrontOfThePlayer(shootCoord))
			{
				var injectRightTrigger = 0;
				if (_settings.FreelookDevice == FeeelookDevice.Gamepad
					&& _settings.AimWithGazeEnabled
					&& !Game.Player.Character.IsInVehicle()
					&& Game.IsKeyPressed(Keys.B))
				{
					injectRightTrigger += 1;
				}

				//if (_settings.FreelookDevice == FeeelookDevice.Mouse)
				//{
				//	Game.Player.Character.Task.AimAt(shootCoord, 250); //raise the gun
				//}

				if (_settings.FreelookDevice == FeeelookDevice.Gamepad
					&& _settings.ThirdPersonFreelookEnabled
					&& Game.Player.Character.IsInVehicle()
					&& Game.IsKeyPressed(Keys.W))
				{
					_injectRightTrigger += 1; //TODO: block keyboard
				}

				_injectRightTrigger = injectRightTrigger;

				if (_settings.AimWithGazeEnabled
					&& ((!_isInVehicle
						&& !_isMeleeWeapon
						&& !_isThrowableWeapon
						&& ((!_menuOpen && User32.IsKeyPressed(VirtualKeyStates.VK_LBUTTON))
							|| (!_isInRadialMenu && controllerState.Gamepad.RightTrigger > 0))
							|| (Game.IsKeyPressed(Keys.B)))
						|| (_isInVehicle
							&& (!_menuOpen && User32.IsKeyPressed(VirtualKeyStates.VK_LBUTTON)))
						))
				{
					_aiming.Shoot(shootCoord);
				}

				if ((_settings.MissilesAtGazeEnabled
						&& Game.Player.Character.IsInVehicle())
						&& missileTarget != null)
				{
					Vector2 screenCoords;
					if (Geometry.WorldToScreenRel(missileTarget.Position, out screenCoords))
					{
						_aiming.MoveCrosshair(screenCoords);
						_aiming.MissileLockedCrosshairVisible = true;
					}
				}
				else
				{
					Vector2 screenCoords;
					if (Geometry.WorldToScreenRel(shootCoord, out screenCoords))
					{
						_aiming.MoveCrosshair(screenCoords);
						_aiming.MissileLockedCrosshairVisible = false;
					}
				}

				if (_settings.AimWithGazeEnabled
					&& _isInVehicle
					&& (Game.IsKeyPressed(Keys.B)
						|| (User32.IsKeyPressed(VirtualKeyStates.VK_XBUTTON1))
						|| (!_isInAircraft && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder))))
				{
					_aiming.Shoot(shootCoord);
				}

				if (_settings.IncinerateAtGazeEnabled
					&& (Game.IsKeyPressed(Keys.J)
						|| (User32.IsKeyPressed(VirtualKeyStates.VK_XBUTTON2))
						|| (!_menuOpen && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))))
				{
					_aiming.Incinerate(shootCoordSnap);
				}

				if (_settings.TaseAtGazeEnabled
					&& (Game.IsKeyPressed(Keys.H)
						|| Game.IsKeyPressed(Keys.PageUp)
						|| (!_isInAircraft && !_menuOpen && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder))))
				{
					_aiming.Tase(shootCoordSnap);
				}

				if (Game.IsKeyPressed(Keys.U))
				{
					_aiming.Water(shootCoord);
				}
			}

			if (shootMissileCoord.Length() > 0 && Geometry.IsInFrontOfThePlayer(shootMissileCoord))
			{
				if (_settings.MissilesAtGazeEnabled
					&& (Game.IsKeyPressed(Keys.N)
					|| Game.IsKeyPressed(Keys.PageDown)
					|| (!_isInRadialMenu && !_menuOpen && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.B))))
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
		}

		private void OnModifyControllerState(object sender, ModifyStateEventArgs modifyStateEventArgs)
		{
			if (_shutDownRequestFlag) return;
			if (_isPaused) return;
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

			if (_isInVehicle)
			{
				if (_isInAircraft)
				{
					if (_settings.MissilesAtGazeEnabled) disableB = true;
					if (_settings.IncinerateAtGazeEnabled) disableA = true;
				}
				else
				{
					if (_settings.AimWithGazeEnabled) disableLeftShoulder = true;
					if (_settings.MissilesAtGazeEnabled) disableB = true;
					if (_settings.TaseAtGazeEnabled) disableRightShoulder = true;
					if (_settings.IncinerateAtGazeEnabled) disableA = true;
				}
			}
			else
			{
				if (_settings.AimWithGazeEnabled)
				{
					disableLeftThumb = true;
					if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftThumb))
					{
						disableRightStick = true;
					}
					if (!_isPaused
					    && !_isInRadialMenu
						&& !_isMeleeWeapon
						&& !_isThrowableWeapon)
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

			if (_menuOpen)
			{
				disableA = false;
				disableB = false;
				//disableStart = false;
			}

			if (_injectRightTrigger > 0)
			{
				state.Gamepad.RightTrigger = 255;
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

			if (e.KeyCode == Keys.L)
			{
				_aiming.AlwaysShowCrosshair = !_aiming.AlwaysShowCrosshair;
			}

			if (e.KeyCode == Keys.F8)
			{
				if (!_menuPool.IsAnyMenuOpen())
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
