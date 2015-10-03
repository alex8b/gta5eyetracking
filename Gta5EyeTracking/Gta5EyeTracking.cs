﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EyeXFramework;
using Gta5EyeTracking.Crosshairs;
using Gta5EyeTracking.Deadzones;
using Gta5EyeTracking.HidEmulation;
using GTA;
using GTA.Math;
using NativeUI;
using Tobii.EyeX.Framework;

namespace Gta5EyeTracking
{
	public class Gta5EyeTracking: Script
	{
		//General
		private readonly Stopwatch _tickStopwatch;
		private readonly GazeProjector _gazeProjector;
		private readonly ControlsProcessor _controlsProcessor;

		//Settings
		private readonly Settings _settings;
		private readonly SettingsStorage _settingsStorage;

		//Gaze
		private readonly EyeXHost _host;
		private readonly GazePointDataStream _lightlyFilteredGazePointDataProvider;
		private readonly Stopwatch _gazeStopwatch;
		private Vector2 _lastNormalizedCenterDelta;
		private double _aspectRatio;

		//Features
		private readonly Aiming _aiming;
		private readonly Freelook _freelook;
		private readonly RadialMenu _radialMenu;
		private readonly PedestrianInteraction _pedestrianInteraction;

		//Hids
		private readonly MouseEmulation _mouseEmulation;
		private readonly ControllerEmulation _controllerEmulation;
		private bool _lastControllerConnected;
		private bool _controllerEverConnected;

		//Debug
		private readonly DotCrosshair _debugGazeVisualization;
		private readonly DebugOutput _debugOutput;
		private bool _showDebugGazeVisualization;

		//Menu
		private readonly MenuPool _menuPool;
		private bool _menuOpen;
		private readonly SettingsMenu _settingsMenu;
		private readonly DeadzoneEditor _deadzoneEditor;

		//Window
		private readonly ForegroundWindowWatcher _foregroundWindowWatcher;
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

			//Gaze
			_aspectRatio = 1;
			_host = new EyeXHost();
			_host.Start();
			_lightlyFilteredGazePointDataProvider = _host.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
			_lightlyFilteredGazePointDataProvider.Next += NewGazePoint;
			_gazeStopwatch = new Stopwatch();
			_gazeStopwatch.Restart();

			//Menu
			_menuPool = new MenuPool();
			_settingsMenu = new SettingsMenu(_menuPool, _settings);
		    _deadzoneEditor = new DeadzoneEditor(_settings,_settingsMenu);
			_settingsMenu.ShutDownRequested += SettingsMenuOnShutDownRequested;

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
			_tickStopwatch = new Stopwatch();
			_gazeProjector = new GazeProjector(_settings);
			_controlsProcessor = new ControlsProcessor(_settings,_controllerEmulation,_aiming,_freelook,_radialMenu,_settingsMenu, _menuPool, _tickStopwatch);

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
			if (e.KeyCode == Keys.K)
			{
				_debugOutput.Visible = !_debugOutput.Visible;
				_showDebugGazeVisualization = !_showDebugGazeVisualization;
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

	    protected override void Dispose(bool disposing)
	    {
	        base.Dispose();
	        ShutDown();
	    }

        private void ShutDown()
        {
            _shutDownRequestFlag = true;
	        KeyDown -= OnKeyDown;
			Tick -= OnTick;
            Task.Run(() =>
            {
                _shutDownRequestedEvent.WaitOne(1000);
                Util.Log("Begin ShutDown");
				_settingsStorage.SaveSettings(_settings);

				//General
	            if (_controlsProcessor != null)
	            {
		            _controlsProcessor.Dispose();
	            }

				//Window
				if (_foregroundWindowWatcher != null)
				{
					_foregroundWindowWatcher.Dispose();
				}

				//Hids
				if (_controllerEmulation != null)
                {
                    _controllerEmulation.Enabled = false;
                    //_controllerEmulation.Dispose();
                    //TODO: Crash!
                }

				//Menu
                if (_settingsMenu != null)
                {
                    _settingsMenu.ShutDownRequested -= SettingsMenuOnShutDownRequested;
                }

				//Gaze
                if (_lightlyFilteredGazePointDataProvider != null)
                {
                    _lightlyFilteredGazePointDataProvider.Next -= NewGazePoint;
                    _lightlyFilteredGazePointDataProvider.Dispose();
                }

                if (_host != null)
                {
                    _host.Dispose();
                }

				//Features
                if (_aiming != null)
                {
                    _aiming.Dispose();
                }
                Util.Log("End ShutDown");
            });
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
            _gazeStopwatch.Restart();
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
			

			Vector3 shootCoord;
			Vector3 shootCoordSnap;
			Vector3 shootMissileCoord;
			Ped ped;
			Entity missileTarget;

			var controllerState = _controllerEmulation.ControllerState;
			const float joystickRadius = 0.1f;

			var joystickDelta = new Vector2(controllerState.Gamepad.RightThumbX, -controllerState.Gamepad.RightThumbY) *
								(1.0f / 32768.0f) * joystickRadius;

			_gazeProjector.FindGazeProjection(
				_lastNormalizedCenterDelta,
				joystickDelta,
                out shootCoord, 
				out shootCoordSnap, 
				out shootMissileCoord, 
				out ped, 
				out missileTarget);

			_controlsProcessor.Process(_lastNormalizedCenterDelta, _aspectRatio, shootCoord, shootCoordSnap, shootMissileCoord, ped, missileTarget);
            
			//_aiming.TurnHead(ped, shootCoord);
			_menuPool.ProcessMenus();

			_aiming.Process();

			if (_showDebugGazeVisualization)
			{
				_debugGazeVisualization.Render();
			}
			
			_debugOutput.Process();

			_pedestrianInteraction.Process(ped, _tickStopwatch.Elapsed);

            _deadzoneEditor.Process();

			_mouseEmulation.ProcessInput();
			_tickStopwatch.Restart();

		    if (_shutDownRequestFlag)
		    {
                _shutDownRequestedEvent.Set();		        
		    }
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
			if (_gazeStopwatch.Elapsed > maxAwayTime)
			{
				//_lastNormalizedCenterDelta = new Vector2();
				//if (!Game.IsPaused) Game.Pause(true); //TODO: doesn't work
			}
		}
	}
}