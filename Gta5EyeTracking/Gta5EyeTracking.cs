using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Gta5EyeTracking.Crosshairs;
using Gta5EyeTracking.Features;
using Gta5EyeTracking.HidEmulation;
using Gta5EyeTracking.Menu;
using GTA;
using GTA.Math;
using NativeUI;

// TODO:
// Roll and yaw after airplane
// Aim at gaze in a single transition
// Windowed mode
// Bug: stuck in fire animation

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
        private EyeTrackingHost _tobiiTracker;

        //Features
        private readonly GameState _gameState;
		private Aiming _aiming;
		private ExtendedView _extendedView;
		private readonly RadialMenu _radialMenu;

		//Hids
		private readonly MouseEmulation _mouseEmulation;
		private ControllerEmulation _controllerEmulation;

		//Debug
		private readonly DefaultCrosshair _debugGazeVisualization;
		private readonly DebugOutput _debugOutput;

		//Menu
		private readonly MenuPool _menuPool;
		private bool _menuOpen;
		private SettingsMenu _settingsMenu;
		private readonly IntroScreen _introScreen;

		//Window
		private ForegroundWindowWatcher _foregroundWindowWatcher;
		private bool _isWindowForeground;

		//Disposing
        private bool _shutDownRequestFlag;
        private readonly ManualResetEvent _shutDownRequestedEvent;
		private readonly AnimationHelper _animationHelper;

	    public Gta5EyeTracking()
		{
            Debug.Log("Begin Initialize");
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
            _tobiiTracker = new EyeTrackingHost();

            //Menu
            _menuPool = new MenuPool();
			_settingsMenu = new SettingsMenu(_menuPool, _settings);

			_introScreen = new IntroScreen(_menuPool, _settings);

			//Debug
			_debugGazeVisualization = new DefaultCrosshair(Color.FromArgb(220, 255, 0, 0), Color.FromArgb(220, 0, 255, 255));
			_debugOutput = new DebugOutput();

			//Hids
			_mouseEmulation = new MouseEmulation();
			_controllerEmulation = new ControllerEmulation();

            //Features
            _gameState = new GameState(_controllerEmulation, _menuPool);
            _animationHelper = new AnimationHelper();
			_aiming = new Aiming(_settings, _animationHelper, _gameState);
			_extendedView = new ExtendedView(_settings, _gameState, _aiming, _debugOutput);
			_radialMenu = new RadialMenu(_controllerEmulation);

			//Window
			_foregroundWindowWatcher = new ForegroundWindowWatcher();
			_foregroundWindowWatcher.ForegroundWindowChanged += ForegroundWindowWatcherOnForegroundWindowChanged;
			_isWindowForeground = _foregroundWindowWatcher.IsWindowForeground();

			//General
			_gazeProjector = new GazeProjector(_settings);
			_controlsProcessor = new ControlsProcessor(_settings,_controllerEmulation,_aiming,_extendedView,_radialMenu,_settingsMenu, _gameState, _debugOutput);

			KeyDown += OnKeyDown;
			Tick += OnTick;
			Aborted += OnAborted;
			AppDomain.CurrentDomain.ProcessExit += AppDomainOnProcessExit;
			AppDomain.CurrentDomain.DomainUnload += AppDomainOnProcessExit;
			Debug.Log("End Initialize");
		}

        private void AppDomainOnProcessExit(object sender, EventArgs eventArgs)
		{
			ShutDown();
		}

		private void OnAborted(object sender, EventArgs eventArgs)
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

		private void ShutDown()
        {
			Debug.Log("Begin ShutDown");
			_shutDownRequestFlag = true;
	        KeyDown -= OnKeyDown;
			Tick -= OnTick;
			Aborted -= OnAborted;
			AppDomain.CurrentDomain.ProcessExit -= AppDomainOnProcessExit;

			_shutDownRequestedEvent.WaitOne(100);
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
	            _settingsMenu = null;
            }

			//Features
            if (_aiming != null)
            {
                _aiming.Dispose();
	            _aiming = null;
            }

			if (_extendedView != null)
			{
				_extendedView.Dispose();
				_extendedView = null;
			}
			
			//Hids
			if (_controllerEmulation != null)
			{
				_controllerEmulation.Enabled = false;
				_controllerEmulation.RemoveHooks();
				_controllerEmulation = null;
			}

            if (_tobiiTracker != null)
            {
                _tobiiTracker.Dispose();
                _tobiiTracker = null;
            }
			Debug.Log("End ShutDown");
		}

		public void OnTick(object sender, EventArgs e)
		{
			if (_shutDownRequestFlag) return;

			_tobiiTracker.Update();

            _debugGazeVisualization.Move(new Vector2(TobiiAPI.GetGazePoint().X * UI.WIDTH, TobiiAPI.GetGazePoint().Y * UI.HEIGHT));

            _controllerEmulation.Enabled = !Game.IsPaused;
			_mouseEmulation.Enabled = !Game.IsPaused && !_menuPool.IsAnyMenuOpen() &&_isWindowForeground;

			//CheckFreelookDevice();

			SaveSettingsOnMenuClosed();

			Game.Player.Character.CanBeKnockedOffBike = _settings.DontFallFromBikesEnabled; //Bug in Script hook

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


			var controllerState = _controllerEmulation.ControllerState;
			const float joystickRadius = 0.1f;

			var joystickDelta = new Vector2(controllerState.Gamepad.RightThumbX, -controllerState.Gamepad.RightThumbY) *
								(1.0f / 32768.0f) * joystickRadius;

            _gameState.Update();

			var centeredNormalizedGaze = new Vector2(TobiiAPI.GetGazePoint().X, TobiiAPI.GetGazePoint().Y) * 2 - new Vector2(1, 1);

			_gazeProjector.FindGazeProjection(
				centeredNormalizedGaze, 
				joystickDelta,
                out shootCoord, 
				out shootCoordSnap, 
				out shootMissileCoord, 
				out ped, 
				out missileTarget);

			_controlsProcessor.Update(_lastTickTime, shootCoord, shootCoordSnap, shootMissileCoord, ped, missileTarget);

			//_aiming.TurnHead(ped, shootCoord);
			_menuPool.ProcessMenus();

			
			_animationHelper.Process();

			if (_debugOutput.Visible)
			{
				_debugGazeVisualization.Render();
			}

			_debugOutput.Process();

			_mouseEmulation.ProcessInput();

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
			var trackingActive = true;
            //TODO
			//(_tobiiTracker.EyeTrackingDeviceStatus.IsValid &&
			//_tobiiTracker.EyeTrackingDeviceStatus.Value == EyeTrackingDeviceStatus.Tracking);
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
	}
}