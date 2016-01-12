using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EyeXFramework;
using Gta5EyeTracking.Crosshairs;
using Gta5EyeTracking.Deadzones;
using Gta5EyeTracking.Features;
using Gta5EyeTracking.HidEmulation;
using Gta5EyeTracking.Menu;
using GTA;
using GTA.Math;
using NativeUI;
using Tobii.EyeX.Framework;

namespace Gta5EyeTracking
{
	public class Gta5EyeTracking: Script
	{
		//General
		private DateTime _lastTickTime;
		private readonly GazeProjector _gazeProjector;
		private ControlsProcessor _controlsProcessor;

		//Statistics
		private readonly GoogleAnalyticsApi _googleAnalyticsApi;
		private bool _gameSessionStartedRecorded;

		//Settings
		private readonly Settings _settings;
		private readonly SettingsStorage _settingsStorage;

		//Gaze
		private EyeXHost _host;
		private GazePointDataStream _lightlyFilteredGazePointDataProvider;
		private DateTime _lastGazeTime;
		private Vector2 _lastNormalizedCenterDelta;
		private double _aspectRatio;

		//Features
		private Aiming _aiming;
		private readonly Freelook _freelook;
		private readonly RadialMenu _radialMenu;
		private readonly PedestrianInteraction _pedestrianInteraction;

		//Hids
		private readonly MouseEmulation _mouseEmulation;
		private ControllerEmulation _controllerEmulation;
		private bool _lastControllerConnected;
		private bool _controllerEverConnected;

		//Debug
		private readonly DotCrosshair _debugGazeVisualization;
		private readonly DebugOutput _debugOutput;

		//Menu
		private readonly MenuPool _menuPool;
		private bool _menuOpen;
		private SettingsMenu _settingsMenu;
		private readonly DeadzoneEditor _deadzoneEditor;
		private readonly IntroScreen _introScreen;

		//Window
		private ForegroundWindowWatcher _foregroundWindowWatcher;
		private bool _isWindowForeground;

		//Disposing
        private bool _shutDownRequestFlag;
        private readonly ManualResetEvent _shutDownRequestedEvent;

		public Gta5EyeTracking()
		{
            Util.Log("Begin Initialize");

			//Disposing
			_shutDownRequestedEvent = new ManualResetEvent(false);

			//Settings
			_settingsStorage = new SettingsStorage();
			_settings = _settingsStorage.LoadSettings();

			//Statistics
			var version = Assembly.GetExecutingAssembly().GetName().Version;
			var versionString = version.Major + "." + version.Minor + "." + version.Build;
			_googleAnalyticsApi = new GoogleAnalyticsApi("UA-68420530-1", _settings.UserGuid, "GTA V Eye Tracking Mod", "gta5eyetracking", versionString);

			//Gaze
			_aspectRatio = 1;
			_host = new EyeXHost();
			_host.Start();
			_lightlyFilteredGazePointDataProvider = _host.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
			_lightlyFilteredGazePointDataProvider.Next += NewGazePoint;
			_lastGazeTime = DateTime.UtcNow;

			//Menu
			_menuPool = new MenuPool();
			_settingsMenu = new SettingsMenu(_menuPool, _settings);
		    _deadzoneEditor = new DeadzoneEditor(_settings,_settingsMenu);
			_settingsMenu.ShutDownRequested += SettingsMenuOnShutDownRequested;

			_introScreen = new IntroScreen(_menuPool, _settings);
			_introScreen.ShutDownRequested += SettingsMenuOnShutDownRequested;

			//Debug
			_debugGazeVisualization = new DotCrosshair(Color.FromArgb(220, 255, 0, 0), Color.FromArgb(220, 0, 255, 255));
			_debugOutput = new DebugOutput();

			//Hids
			_mouseEmulation = new MouseEmulation();
			_controllerEmulation = new ControllerEmulation();

			//Features
			_aiming = new Aiming(_settings);
			_freelook = new Freelook(_controllerEmulation, _mouseEmulation, _settings);
			_radialMenu = new RadialMenu(_controllerEmulation);
			_pedestrianInteraction = new PedestrianInteraction(_settings);

			//Window
			_foregroundWindowWatcher = new ForegroundWindowWatcher();
			_foregroundWindowWatcher.ForegroundWindowChanged += ForegroundWindowWatcherOnForegroundWindowChanged;
			_isWindowForeground = _foregroundWindowWatcher.IsWindowForeground();

			//General
			_gazeProjector = new GazeProjector(_settings);
			_controlsProcessor = new ControlsProcessor(_settings,_controllerEmulation,_aiming,_freelook,_radialMenu,_settingsMenu, _menuPool, _debugOutput);

			KeyDown += OnKeyDown;
			Tick += OnTick;
			
			Util.Log("End Initialize");
		}

	    private void SettingsMenuOnShutDownRequested(object sender, EventArgs eventArgs)
	    {
	        ShutDown();
	    }

	    private void ForegroundWindowWatcherOnForegroundWindowChanged(object sender, ForegroundWindowChangedEventArgs foregroundWindowChangedEventArgs)
		{
			_isWindowForeground = foregroundWindowChangedEventArgs.GameIsForegroundWindow;
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			_controlsProcessor.KeyDown(sender, e);
		}

	    protected override void Dispose(bool disposing)
	    {
			Util.Log("Begin Dispose");
			ShutDown();
			Util.Log("End Dispose");
		}

        private void ShutDown()
        {
            _shutDownRequestFlag = true;
	        KeyDown -= OnKeyDown;
			Tick -= OnTick;

			_shutDownRequestedEvent.WaitOne(100);
            Util.Log("Begin ShutDown");
			_settingsStorage.SaveSettings(_settings);

			//General
			RecordGameSessionEnded();

            if (_controlsProcessor != null)
	        {
		        _controlsProcessor.Dispose();
		        _controlsProcessor = null;
	        }

			//Window
			if (_foregroundWindowWatcher != null)
			{
				_foregroundWindowWatcher.Dispose();
				_foregroundWindowWatcher = null;
			}

			//Menu
            if (_settingsMenu != null)
            {
                _settingsMenu.ShutDownRequested -= SettingsMenuOnShutDownRequested;
	            _settingsMenu = null;
            }

			//Gaze
            if (_lightlyFilteredGazePointDataProvider != null)
            {
                _lightlyFilteredGazePointDataProvider.Next -= NewGazePoint;
                _lightlyFilteredGazePointDataProvider.Dispose();
	            _lightlyFilteredGazePointDataProvider = null;
            }

            if (_host != null)
            {
                _host.Dispose();
	            _host = null;
            }

			//Features
            if (_aiming != null)
            {
                _aiming.Dispose();
	            _aiming = null;
            }

			//Hids
			if (_controllerEmulation != null)
			{
				_controllerEmulation.Enabled = false;
				_controllerEmulation.RemoveHooks();
				_controllerEmulation = null;
			}
			Util.Log("End ShutDown");
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
			_debugGazeVisualization.Move(_lastNormalizedCenterDelta);
			_lastGazeTime = DateTime.UtcNow;
		}

		public void OnTick(object sender, EventArgs e)
		{
			if (_shutDownRequestFlag) return;

			_controllerEmulation.Enabled = !Game.IsPaused;
			_mouseEmulation.Enabled = !Game.IsPaused && !_menuPool.IsAnyMenuOpen() &&_isWindowForeground;

			CheckFreelookDevice();

			SaveSettingsOnMenuClosed();

			Game.Player.Character.CanBeKnockedOffBike = _settings.DontFallFromBikesEnabled; //Bug in Script hook

			CheckUserPresense();

			if (Game.IsPaused) return;

			if (!_settings.UserAgreementAccepted)
			{
				_introScreen.OpenMenu();
            }

			RecordGameSessionStarted();

			Vector3 shootCoord;
			Vector3 shootCoordSnap;
			Vector3 shootMissileCoord;
			Ped ped;
			Entity missileTarget;

			//Util.Log("0 - " + DateTime.UtcNow.Ticks);

			var controllerState = _controllerEmulation.ControllerState;
			const float joystickRadius = 0.1f;

			var joystickDelta = new Vector2(controllerState.Gamepad.RightThumbX, -controllerState.Gamepad.RightThumbY) *
								(1.0f / 32768.0f) * joystickRadius;

			//Util.Log("1 - " + DateTime.UtcNow.Ticks);

			_gazeProjector.FindGazeProjection(
				_lastNormalizedCenterDelta,
				joystickDelta,
                out shootCoord, 
				out shootCoordSnap, 
				out shootMissileCoord, 
				out ped, 
				out missileTarget);

			//Util.Log("2 - " + DateTime.UtcNow.Ticks);

			_controlsProcessor.Process(_lastTickTime, _lastNormalizedCenterDelta, _aspectRatio, shootCoord, shootCoordSnap, shootMissileCoord, ped, missileTarget);

			//Util.Log("3 - " + DateTime.UtcNow.Ticks);


			//_aiming.TurnHead(ped, shootCoord);
			_menuPool.ProcessMenus();

			//Util.Log("4 - " + DateTime.UtcNow.Ticks);

			_aiming.Process();

			//Util.Log("5 - " + DateTime.UtcNow.Ticks);

			if (_debugOutput.Visible)
			{
				_debugGazeVisualization.Render();
			}

			_debugOutput.Process();

			//Util.Log("6 - " + DateTime.UtcNow.Ticks);

			_pedestrianInteraction.Process(ped, DateTime.UtcNow - _lastTickTime);

			//Util.Log("7 - " + DateTime.UtcNow.Ticks);


			_deadzoneEditor.Process();

			_mouseEmulation.ProcessInput();

			//Util.Log("8 - " + DateTime.UtcNow.Ticks);

			_lastTickTime = DateTime.UtcNow;

			if (_shutDownRequestFlag)
		    {
                _shutDownRequestedEvent.Set();		        
		    }
		}

		private void RecordGameSessionStarted()
		{
			if (!_settings.UserAgreementAccepted || !_settings.SendUsageStatistics || _gameSessionStartedRecorded) return;
			
			_googleAnalyticsApi.TrackEvent("gamesession", "started", "Game Session Started");
			var trackingActive = (_host.EyeTrackingDeviceStatus.IsValid &&
			                    _host.EyeTrackingDeviceStatus.Value == EyeTrackingDeviceStatus.Tracking);
			if (trackingActive)
			{
				_googleAnalyticsApi.TrackEvent("gamesession", "devicesconnected", "Device Connected", 1);
			}
			else
			{
				_googleAnalyticsApi.TrackEvent("gamesession", "devicesdisconnected", "Device Disconnected", 1);
			}
			
			_gameSessionStartedRecorded = true;
		}

		private void RecordGameSessionEnded()
		{
			if (!_settings.UserAgreementAccepted || !_settings.SendUsageStatistics) return;
			_googleAnalyticsApi.TrackEvent("gamesession", "ended", "Game Session Ended");
		}

		private void SaveSettingsOnMenuClosed()
		{
			var lastMenuOpen = _menuOpen;
			_menuOpen = _menuPool.IsAnyMenuOpen();

			if (lastMenuOpen && !_menuOpen)
			{
				_settingsStorage.SaveSettings(_settings);
			}
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

		private void CheckUserPresense()
		{
			var maxAwayTime = TimeSpan.FromSeconds(2);
			if (DateTime.UtcNow - _lastGazeTime > maxAwayTime)
			{
		//		_lastNormalizedCenterDelta = new Vector2();
		//		if (!Game.IsPaused) Game.Pause(true); //TODO: doesn't work
			}
		}
	}
}