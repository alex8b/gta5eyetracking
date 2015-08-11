using System;
using System.Diagnostics;
using Gta5EyeTracking.HidEmulation;
using GTA.Math;

namespace Gta5EyeTracking
{
	public class RadialMenu
	{
		private readonly ControllerEmulation _controllerEmulation;
		private readonly Stopwatch _newRadialMenuRegionStopwatch;
		private int _lastRadialMenuRegion;

		public RadialMenu(ControllerEmulation controllerEmulation)
		{
			_controllerEmulation = controllerEmulation;
			_lastRadialMenuRegion = -1;
			_newRadialMenuRegionStopwatch = new Stopwatch();
		}

		public void Process(Vector2 gazeNormalizedCenterDelta, double aspectRatio)
		{
			const double radialMenuYOffset = 0.17;
			const double radialMenuInnerRadius = 0.23;
			const int numberOfSectors = 8;
			const int sectorSize = 360 / numberOfSectors;

			var deltaVector = new Vector2((float)(gazeNormalizedCenterDelta.X * aspectRatio), (float)(gazeNormalizedCenterDelta.Y + radialMenuYOffset));
			if (deltaVector.Length() < radialMenuInnerRadius) return;

			var angleRad = Math.Atan2(-deltaVector.Y, deltaVector.X);
			var angleDeg = Geometry.RadToDeg(angleRad);
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
			var alpha = Geometry.DegToRad(_lastRadialMenuRegion * sectorSize);
			var freelookDeltaVector = new Vector2((float)(Math.Cos(alpha)), (float)(-Math.Sin(alpha)));
			const double rotationalSpeed = 1;
			_controllerEmulation.DeltaX = freelookDeltaVector.X * rotationalSpeed;
			_controllerEmulation.DeltaY = freelookDeltaVector.Y * rotationalSpeed;
		}
	}
}
