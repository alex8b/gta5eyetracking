using System;
using System.Diagnostics;
using Gta5EyeTracking.HidEmulation;
using GTA.Math;

namespace Gta5EyeTracking.Features
{
	public class RadialMenu
	{
		private readonly ControllerEmulation _controllerEmulation;
	    private readonly ITobiiTracker _tobiiTracker;
	    private readonly Stopwatch _newRadialMenuRegionStopwatch;
		private int _lastRadialMenuRegion;

		public RadialMenu(ControllerEmulation controllerEmulation, ITobiiTracker tobiiTracker)
		{
			_controllerEmulation = controllerEmulation;
		    _tobiiTracker = tobiiTracker;
		    _lastRadialMenuRegion = -1;
			_newRadialMenuRegionStopwatch = new Stopwatch();
		}

		public void Update()
		{
			const float radialMenuYOffset = 0.17f;
			const float radialMenuInnerRadius = 0.23f;
			const int numberOfSectors = 8;
			const int sectorSize = 360 / numberOfSectors;

			var deltaVector = new Vector2(_tobiiTracker.GazeX * _tobiiTracker.AspectRatio, (float)(_tobiiTracker.GazeY + radialMenuYOffset));
			if (deltaVector.Length() < radialMenuInnerRadius) return;

			var angleRad = (float)Math.Atan2(-deltaVector.Y, deltaVector.X);
			var angleDeg = Mathf.Rad2Deg * angleRad;
			var region = (int)Math.Floor(360 + angleDeg + sectorSize * 0.5) / sectorSize;
			region = region % numberOfSectors;
			if (region < 0) region += numberOfSectors;

			if (_lastRadialMenuRegion == region)
			{
				_newRadialMenuRegionStopwatch.Reset();
			}
			else
			{
				_newRadialMenuRegionStopwatch.Start();
			}
			var switchTime = TimeSpan.FromMilliseconds(60);
			if (_newRadialMenuRegionStopwatch.Elapsed > switchTime)
			{
				_lastRadialMenuRegion = region;
				_newRadialMenuRegionStopwatch.Reset();
			}

			if (_lastRadialMenuRegion < 0) return;
			var alpha = Mathf.Deg2Rad * (_lastRadialMenuRegion * sectorSize);
			var freelookDeltaVector = new Vector2((float)(Math.Cos(alpha)), (float)(-Math.Sin(alpha)));
			const double rotationalSpeed = 1;
			_controllerEmulation.DeltaX = freelookDeltaVector.X * rotationalSpeed;
			_controllerEmulation.DeltaY = freelookDeltaVector.Y * rotationalSpeed;
		}
	}
}
