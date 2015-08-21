using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using EyeXFramework;
using Gta5EyeTracking.HidEmulation;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using SharpDX.XInput;
using Tobii.EyeX.Framework;

namespace Gta5EyeTracking
{
	public class Gta5EyeTracking: Script
	{
		private const string SettingsPath = "Gta5EyeTracking";
		private const string SettingsFileName = "settings.xml";

		private readonly EyeXHost _host;
		private readonly GazePointDataStream _lightlyFilteredGazePointDataProvider;

		private readonly Stopwatch _gazeStopwatch;
		private readonly Stopwatch _tickStopwatch;

		private Vector2 _lastNormalizedCenterDelta;

		private readonly Aiming _aiming;
		private readonly Freelook _freelook;

		private readonly MouseEmulation _mouseEmulation;
		private readonly ControllerEmulation _controllerEmulation;

		private Settings _settings;
		private readonly GazeVisualization _gazeVisualization;
		private readonly PedestrianInteraction _pedestrianInteraction;
		private readonly DebugOutput _debugOutput;
		private bool _isPaused;
		private bool _isInVehicle;
		private bool _isInAircraft;
		private Vector2 _gazePointDelta;
		private Vector2 _gazePlusJoystickDelta;
		private Vector2 _unfilteredgazePlusJoystickDelta;
		private float _headingToTarget;

		private readonly MenuPool _menuPool;
		private bool _menuOpen;
		private readonly SettingsMenu _settingsMenu;
		private double _aspectRatio;
		private readonly RadialMenu _radialMenu;
		private int _injectRightTrigger;
		private bool _lastControllerConnected;
		private bool _controllerEverConnected;

		private readonly ForegroundWindowWatcher _foregroundWindowWatcher;
		private bool _isWindowForeground;

		public Gta5EyeTracking()
		{
			_aspectRatio = 1;
			_host = new EyeXHost();
			_host.Start();
			_lightlyFilteredGazePointDataProvider = _host.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
			_lightlyFilteredGazePointDataProvider.Next += NewGazePoint;

			_menuPool = new MenuPool();

			LoadSettings();
			_settingsMenu = new SettingsMenu(_menuPool, _settings);
			_settingsMenu.DeadzoneMenu.OnItemSelect += (m, item, indx) =>
			{
				if (indx == 0)
				{
					_isDrawingDeadzone = true;
				}
				else
				{
					_settings.Deadzones.RemoveAt(indx - 1);
					_settingsMenu.DeadzoneMenu.RemoveItemAt(indx);
					_settingsMenu.DeadzoneMenu.RefreshIndex();
				}
			};

			_gazeVisualization = new GazeVisualization();
			_debugOutput = new DebugOutput();

			_aiming = new Aiming(_settings);

			_mouseEmulation = new MouseEmulation();
			_controllerEmulation = new ControllerEmulation();
			_controllerEmulation.OnModifyState += OnModifyControllerState;
			_freelook = new Freelook(_controllerEmulation, _mouseEmulation, _settings);
			_pedestrianInteraction = new PedestrianInteraction();
			_radialMenu = new RadialMenu(_controllerEmulation);

			_gazeStopwatch = new Stopwatch();
			_gazeStopwatch.Restart();

			_tickStopwatch = new Stopwatch();

			_foregroundWindowWatcher = new ForegroundWindowWatcher();
			_foregroundWindowWatcher.ForegroundWindowChanged += ForegroundWindowWatcherOnForegroundWindowChanged;
			_isWindowForeground = _foregroundWindowWatcher.IsWindowForeground();

			View.MenuTransitions = true;

			KeyDown += OnKeyDown;

			Tick += OnTick;
		}

		private void ForegroundWindowWatcherOnForegroundWindowChanged(object sender, ForegroundWindowChangedEventArgs foregroundWindowChangedEventArgs)
		{
			_isWindowForeground = foregroundWindowChangedEventArgs.GameIsForegroundWindow;
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.K)
			{
				_debugOutput.Visible = !_debugOutput.Visible;
				_gazeVisualization.Visible = !_gazeVisualization.Visible;
			}

			if (e.KeyCode == Keys.L)
			{
				_aiming.AlwaysShowCrosshair = !_aiming.AlwaysShowCrosshair;
			}


			if (e.KeyCode == Keys.F8) 
			{
				if (View.ActiveMenus == 0)
				{
					_settingsMenu.OpenMenu();
				}
				else
				{
					_settingsMenu.CloseMenu();
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose();

			SaveSettings();

			if (_lightlyFilteredGazePointDataProvider != null)
			{
				_lightlyFilteredGazePointDataProvider.Next -= NewGazePoint;
				_lightlyFilteredGazePointDataProvider.Dispose();
			}

			if (_host != null)
			{
				_host.Dispose();
			}
			
			if (_controllerEmulation != null)
			{
				_controllerEmulation.OnModifyState -= OnModifyControllerState;
				_controllerEmulation.Dispose();
			}

			if (_gazeVisualization != null)
			{
				_gazeVisualization.Dispose();
			}

			if (_foregroundWindowWatcher != null)
			{
				_foregroundWindowWatcher.Dispose();
			}

            if (_aiming != null)
            {
                _aiming.Dispose();
            }
		}

		private void LoadSettings()
		{
			try
			{
				var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingsPath);
				var filePath = Path.Combine(folderPath, SettingsFileName);
				System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(Settings));
				var file = new StreamReader(filePath);
				var settings = (Settings)reader.Deserialize(file);
				_settings = settings;
				file.Close();
			}
			catch (Exception e)
			{
				//Failed
				_settings = new Settings();
			}

			if (_settings == null)
			{
				_settings = new Settings();
			}
		}

		private void SaveSettings()
		{
			try
			{
				var writer = new System.Xml.Serialization.XmlSerializer(typeof (Settings));
				var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingsPath);
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}

				var filePath = Path.Combine(folderPath, SettingsFileName);
				var wfile = new StreamWriter(filePath);
				writer.Serialize(wfile, _settings);
				wfile.Close();
			}
			catch (Exception e)
			{
				//Failed
			}
		}

		private void NewGazePoint(object sender, GazePointEventArgs gazePointEventArgs)
		{
			const double screenExtensionFactor = 0;
			var screenExtensionX = _host.ScreenBounds.Value.Width * screenExtensionFactor;
			var screenExtensionY = _host.ScreenBounds.Value.Height * screenExtensionFactor;

			var gazePointX = gazePointEventArgs.X + screenExtensionX / 2;
			var gazePointY = gazePointEventArgs.Y + screenExtensionY / 2;

			var screenWidth = _host.ScreenBounds.Value.Width + screenExtensionX;
			var screenHeight = _host.ScreenBounds.Value.Height + screenExtensionY;

			if (screenHeight > 0)
			{
				_aspectRatio = screenWidth/screenHeight;
			}

			var normalizedGazePointX = (float)Math.Min(Math.Max((gazePointX / screenWidth), 0.0), 1.0);
			var normalizedGazePointY = (float)Math.Min(Math.Max((gazePointY / screenHeight), 0.0), 1.0);

			var normalizedCenterDeltaX = (normalizedGazePointX - 0.5f) * 2.0f;
			var normalizedCenterDeltaY = (normalizedGazePointY - 0.5f) * 2.0f;
			if (float.IsNaN(normalizedCenterDeltaX) || float.IsNaN(normalizedCenterDeltaY)) return;

			_lastNormalizedCenterDelta = new Vector2(normalizedCenterDeltaX, normalizedCenterDeltaY);
			_gazeVisualization.MovePoint(_lastNormalizedCenterDelta);
			_gazeStopwatch.Restart();
		}

		public void OnTick(object sender, EventArgs e)
		{
			_controllerEmulation.Enabled = !Game.IsPaused;
			_mouseEmulation.Enabled = !Game.IsPaused && !_menuPool.IsAnyMenuOpen() &&_isWindowForeground;

			CheckFreelookDevice();

			var lastMenuOpen = _menuOpen;
			_menuOpen = _menuPool.IsAnyMenuOpen();

			if (lastMenuOpen && !_menuOpen)
			{
				SaveSettings();
			}

			_isPaused = Game.IsPaused;
			Game.Player.Character.CanBeKnockedOffBike = _settings.DontFallFromBikesEnabled; //Bug in Script hook
			CheckUserPresense();

			if (Game.IsPaused) return;
			
			_isInVehicle = Game.Player.Character.IsInVehicle();
			_isInAircraft = Game.Player.Character.IsInPlane|| Game.Player.Character.IsInHeli;

			Vector3 shootCoord;
			Vector3 shootCoordSnap;
			Vector3 shootMissileCoord;
			Ped ped;
		    Entity target;
            FindGazeProjection(out shootCoord, out shootCoordSnap, out shootMissileCoord, out ped, out target);

			ProcessControls(shootCoord, shootCoordSnap, shootMissileCoord, target);
            
			//TurnHead(ped, shootCoord);
			_menuPool.ProcessMenus();

			_aiming.Process();

			_gazeVisualization.Process();
			_debugOutput.Process();

			if (_settings.PedestrianInteractionEnabled)
			{
				if (ped != null && ped.Handle != Game.Player.Character.Handle)
				{
					_pedestrianInteraction.ProcessLookingAtPedestrion(ped, _tickStopwatch.Elapsed);
				}

				_pedestrianInteraction.Process();
			}

			DrawDeadzones();
			DeadzoneCreator();
			DeadzoneMonitor();

			_mouseEmulation.ProcessInput();
			_tickStopwatch.Restart();
		}

		private void CheckFreelookDevice()
		{
			var controllerConnected = _controllerEmulation.ControllerConnected;
			_controllerEverConnected = _controllerEverConnected || controllerConnected;
			if (controllerConnected && !_lastControllerConnected)
			{
				_settings.FreelookDevice = FeeelookDevice.Gamepad;
				_settingsMenu.ReloadSettings();
			}

			if (!_controllerEverConnected)
			{
				_settings.FreelookDevice = FeeelookDevice.Mouse;
				_settingsMenu.ReloadSettings();
			}

			_lastControllerConnected = controllerConnected;
		}

		private void DrawDeadzones()
		{
			if(!_settingsMenu.DeadzoneMenu.Visible) return;
			foreach (Deadzone z in _settings.Deadzones)
			{
				var res = UIMenu.GetScreenResolutionMantainRatio();
				Point pos = new Point(Convert.ToInt32(((z.Position.X + 1)/2)*res.Width), Convert.ToInt32(((z.Position.Y + 1) / 2) * res.Height));
				Vector2 endPoint = new Vector2(z.Position.X + z.Size.Width, z.Position.Y + z.Size.Height);
				Size size = new Size(Convert.ToInt32(((endPoint.X + 1) /2) * res.Width - pos.X),
					Convert.ToInt32(((endPoint.Y + 1) / 2) * res.Height - pos.Y));
				new UIResRectangle(pos, size, z.Color).Draw();
			}
		}

		private bool _isDrawingDeadzone = false;
		private Vector2? _firstPoint;
		private Vector2? _secondPoint;

		private void DeadzoneCreator()
		{
			if(!_isDrawingDeadzone) return;
			var mouseX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)GTA.Control.CursorX);
			var mouseY = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)GTA.Control.CursorY);
			if (_firstPoint == null && Game.IsControlJustPressed(0, GTA.Control.Attack))
				_firstPoint = new Vector2((mouseX*2) - 1, (mouseY*2) - 1);
			if (_secondPoint == null && Game.IsControlJustReleased(0, GTA.Control.Attack))
				_secondPoint = new Vector2((mouseX*2) - 1, (mouseY*2) - 1);
			if(!_firstPoint.HasValue) return;
			var res = UIMenu.GetScreenResolutionMantainRatio();
			Point pos = new Point(Convert.ToInt32(((_firstPoint.Value.X + 1)/2)*res.Width), Convert.ToInt32(((_firstPoint.Value.Y + 1) / 2) * res.Height));
			Size size;
				size = new Size(Convert.ToInt32(mouseX * res.Width - ((_firstPoint.Value.X + 1) / 2) * res.Width),
					Convert.ToInt32(mouseY * res.Height - ((_firstPoint.Value.Y + 1) / 2) * res.Height));
			new UIResRectangle(pos, size, Color.FromArgb(150, 200, 200, 20)).Draw();
		}

		private void DeadzoneMonitor()
		{
			if(!_isDrawingDeadzone) return;
			if (_firstPoint.HasValue && _secondPoint.HasValue)
			{
				_settings.Deadzones.Add(new Deadzone(_firstPoint.Value.X, _firstPoint.Value.Y, _secondPoint.Value.X - _firstPoint.Value.X, _secondPoint.Value.Y - _firstPoint.Value.Y));
				_settingsMenu.DeadzoneMenu.AddItem(new UIMenuItem("Deadzone #" + _settings.Deadzones.Count, "Select to remove."));
				_settingsMenu.DeadzoneMenu.RefreshIndex();
				_firstPoint = null;
				_secondPoint = null;
				_isDrawingDeadzone = false;
			}
		}

		private void TurnHead(Ped ped, Vector3 shootCoord)
		{
			if (ped != null && ped.Handle != Game.Player.Character.Handle)
			{
				if (!Geometry.IsFirstPersonCameraActive())
				{
					Game.Player.Character.Task.LookAt(ped);
				}
			}
			else
			{
				if (!Geometry.IsFirstPersonCameraActive())
				{
					Game.Player.Character.Task.LookAt(shootCoord);
				}
			}
		}

        private void FindGazeProjection(out Vector3 shootCoord, out Vector3 shootCoordSnap, out Vector3 shootMissileCoord, out Ped ped, out Entity target)
        {
            target = null;
			const float joystickRadius = 0.1f;

			var controllerState = _controllerEmulation.ControllerState;

			var joystickDelta = new Vector2(controllerState.Gamepad.RightThumbX, -controllerState.Gamepad.RightThumbY)*
								(1.0f/32768.0f)*joystickRadius;

			var w = (float)(1 - _settings.GazeFiltering * 0.9);
			_gazePointDelta = new Vector2(_gazePointDelta.X + (_lastNormalizedCenterDelta.X - _gazePointDelta.X) * w,
				_gazePointDelta.Y + (_lastNormalizedCenterDelta.Y - _gazePointDelta.Y) * w);

			_gazePlusJoystickDelta = _gazePointDelta + joystickDelta;
			_unfilteredgazePlusJoystickDelta = _lastNormalizedCenterDelta;

			var hitUnfiltered = Geometry.RaycastEverything(_unfilteredgazePlusJoystickDelta);
			shootMissileCoord = hitUnfiltered;
			shootCoordSnap = hitUnfiltered;

			var hitFiltered = Geometry.RaycastEverything(_gazePlusJoystickDelta);
			shootCoord = hitFiltered;


			ped = Geometry.RaycastPed(_unfilteredgazePlusJoystickDelta);
			if ((ped != null)
				&& (ped.Handle != Game.Player.Character.Handle))
			{
				shootCoordSnap = ped.GetBoneCoord(Bone.SKEL_L_Clavicle);
                target = ped;
				if (_settings.SnapAtPedestriansEnabled)
				{
					shootCoord = shootCoordSnap;
                    
				}
			}
			else
			{
				var vehicle = Geometry.RaycastVehicle(_unfilteredgazePlusJoystickDelta);
				if (vehicle != null
					&& !((Game.Player.Character.IsInVehicle())
						&& (vehicle.Handle == Game.Player.Character.CurrentVehicle.Handle)))
				{
					shootCoordSnap = vehicle.Position + vehicle.Velocity * 0.06f;
					shootMissileCoord = shootCoordSnap;
				    target = vehicle;
				}
			}
			var playerDistToGround = Game.Player.Character.Position.Z - World.GetGroundHeight(Game.Player.Character.Position);
			var targetDir = shootMissileCoord - Game.Player.Character.Position;
			targetDir.Normalize();
			var justBeforeTarget = shootMissileCoord - targetDir;
			var targetDistToGround = shootMissileCoord.Z - World.GetGroundHeight(justBeforeTarget);
			var distToTarget = (Game.Player.Character.Position - shootMissileCoord).Length();
			if ((playerDistToGround < 2) && (playerDistToGround >= -0.5)) //on the ground 
			{
				
				if (((targetDistToGround < 2) && (targetDistToGround >= -0.5)) //shoot too low
					|| ((targetDistToGround < 5) && (targetDistToGround >= -0.5) && (distToTarget > 70.0))) //far away add near the ground
				{
					shootMissileCoord.Z = World.GetGroundHeight(justBeforeTarget) //ground level at target
						+ playerDistToGround; //offset
				}
			}

			if (!_menuOpen 
				&& (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadDown)
				|| User32.IsKeyPressed(VirtualKeyStates.VK_LMENU)))
			{
				//character selection
			}
			else if (!_isInVehicle && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder))
			{
				_radialMenu.Process(_lastNormalizedCenterDelta, _aspectRatio);
			}
			else
			{
				_freelook.Process(_lastNormalizedCenterDelta, ped, _aspectRatio);
			}
           
		}

		private void ProcessControls(Vector3 shootCoord, Vector3 shootCoordSnap, Vector3 shootMissileCoord, Entity target)
		{
			var controllerState = _controllerEmulation.ControllerState;

			var radialMenuActive = (!Game.Player.Character.IsInVehicle()
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
						&& ((!_menuOpen && User32.IsKeyPressed(VirtualKeyStates.VK_LBUTTON))
							|| (!radialMenuActive && controllerState.Gamepad.RightTrigger > 0))
							|| (Game.IsKeyPressed(Keys.B)))
						|| (_isInVehicle
							&& (!_menuOpen && User32.IsKeyPressed(VirtualKeyStates.VK_LBUTTON)))
						))
				{
					var dir = shootCoord - Game.Player.Character.Position;
					_headingToTarget = Geometry.DirectionToRotation(dir).Z;
					//Game.Player.Character.Heading = _headingToTarget;

					//Game.Player.Character.Rotation = new Vector3(Game.Player.Character.Rotation.X, Game.Player.Character.Rotation.Y, _headingToTarget);
					//UI.ShowSubtitle("he " + Math.Round(Game.Player.Character.Heading,2));
					Util.SetPedShootsAtCoord(Game.Player.Character, shootCoord);
					//Game.Player.Character.Rotation = new Vector3(Game.Player.Character.Rotation.X, Game.Player.Character.Rotation.Y, _headingToTarget);
				}

				Vector2 screenCoords;
				if (Geometry.WorldToScreenRel(shootCoord, out screenCoords))
				{
					_aiming.MoveCrosshair(screenCoords);
				}	

				_debugOutput.DebugText2.Caption = "Cr: " + Math.Round(shootCoord.X, 1) + " | " + Math.Round(shootCoord.Y, 1) + " | " +
									Math.Round(shootCoord.Z, 1);

				if (_settings.AimWithGazeEnabled 
					&& _isInVehicle
					&& (Game.IsKeyPressed(Keys.B)
						|| (User32.IsKeyPressed(VirtualKeyStates.VK_XBUTTON1))
						|| ( !_isInAircraft && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder))))
				{
					_aiming.Shoot(shootCoord);
				}

				if (_settings.IncinerateAtGazeEnabled
					&& (Game.IsKeyPressed(Keys.J)
						|| (User32.IsKeyPressed(VirtualKeyStates.VK_XBUTTON2))
						|| (!_isInAircraft && !_menuOpen && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
						|| (_isInAircraft && !_menuOpen && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))))
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
					|| (!radialMenuActive && !_menuOpen && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.B))))
				{
				    if (target != null)
				    {
				        _aiming.ShootMissile(target);
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
			if (_isPaused) return;
			var timePausedThershold = TimeSpan.FromSeconds(0.5);
			if (_tickStopwatch.Elapsed > timePausedThershold) return;

			var state = modifyStateEventArgs.State;

			var disableA = false;
			var disableB = false;
			var disableLeftShoulder = false;
			var disableRightShoulder = false;
			var disableLeftThumb = false;
			var disableRightStick = false;
			var disableStart = false;

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

			if (_injectRightTrigger>0)
			{
				state.Gamepad.RightTrigger = 255;
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

			if (disableB &&  state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.B))
			{
				state.Gamepad.Buttons &= ~GamepadButtonFlags.B;
			}

			modifyStateEventArgs.State = state;
		}

		private void CheckUserPresense()
		{
			var maxAwayTime = TimeSpan.FromSeconds(2);
			if (_gazeStopwatch.Elapsed > maxAwayTime)
			{
				//_lastNormalizedCenterDelta = new Vector2();
				if (!Game.IsPaused) Game.Pause(); //TODO: doesn't work
			}
		}
	}
}