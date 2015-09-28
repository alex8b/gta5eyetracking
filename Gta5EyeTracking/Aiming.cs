using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Gta5EyeTracking.HomingMissiles;
using GTA;
using GTA.Math;
using GTA.Native;
using Tobii.EyeX.Client;

namespace Gta5EyeTracking
{
	public class Aiming: DisposableBase
	{
    	public bool AlwaysShowCrosshair { get; set; }

        private bool _missileLockedCrosshairVisible;
        public bool MissileLockedCrosshairVisible
	    {
	        get { return _missileLockedCrosshairVisible; }
	        set
	        {
	            _missileLockedCrosshairVisible = value;
	            _uiContainerMissileLockedCrosshair.Enabled = value;
	            _uiContainerCrosshair.Enabled = !value;
	        }
	    }

	    private readonly Stopwatch _shootStopWatch;
		private UIContainer _uiContainerCrosshair;
        private UIContainer _uiContainerMissileLockedCrosshair;

        private bool _drawCrosshair;
		private Vector2 _crosshairPosition;
		private readonly Settings _settings;
	    private readonly HomingMissilesHelper _homingMissilesHelper;

        private int _missileLockedCrosshairColorDelta;
        private readonly Stopwatch _missileLockedCrosshairAnimateStopwatch;
	    private TimeSpan _missileLockedCrosshairAnimateFrameTime;

	    public Aiming(Settings settings)
		{
			_settings = settings;
			_shootStopWatch = new Stopwatch();
			_shootStopWatch.Restart();

            _missileLockedCrosshairAnimateStopwatch = new Stopwatch();
            _missileLockedCrosshairAnimateStopwatch.Restart();
            _missileLockedCrosshairAnimateFrameTime = TimeSpan.FromSeconds(0.02);

            _homingMissilesHelper = new HomingMissilesHelper();
			CreateCrosshair();
	        CreateMissileLockedCrosshair();
		}

	    protected override void DisposeManagedResources()
	    {
	        _homingMissilesHelper.Dispose();
	    }

	    private void CreateCrosshair()
		{
			_uiContainerCrosshair = new UIContainer(new Point(0, 0), new Size(4, 4), Color.FromArgb(0, 0, 0, 0));
			var crosshair1 = new UIRectangle(new Point(0, 0), new Size(4, 4), Color.FromArgb(220, 0, 0, 0));
			_uiContainerCrosshair.Items.Add(crosshair1);
			var crosshair2 = new UIRectangle(new Point(1, 1), new Size(2, 2), Color.FromArgb(220, 255, 255, 255));
			_uiContainerCrosshair.Items.Add(crosshair2);
		}

        private void CreateMissileLockedCrosshair()
        {
            _uiContainerMissileLockedCrosshair = new UIContainer(new Point(0, 0), new Size(40, 40), Color.FromArgb(0, 0, 0, 0));
            var color = Color.FromArgb(220, 255, 50, 50);
            var crosshair1 = new UIRectangle(new Point(0, 0), new Size(5, 2), color);
            _uiContainerMissileLockedCrosshair.Items.Add(crosshair1);
            var crosshair2 = new UIRectangle(new Point(0, 38), new Size(5, 2), color);
            _uiContainerMissileLockedCrosshair.Items.Add(crosshair2);
            var crosshair3 = new UIRectangle(new Point(0, 2), new Size(2, 3), color);
            _uiContainerMissileLockedCrosshair.Items.Add(crosshair3);
            var crosshair4 = new UIRectangle(new Point(38, 2), new Size(2, 3), color);
            _uiContainerMissileLockedCrosshair.Items.Add(crosshair4);

            var crosshair5 = new UIRectangle(new Point(35, 0), new Size(5, 2), color);
            _uiContainerMissileLockedCrosshair.Items.Add(crosshair5);
            var crosshair6 = new UIRectangle(new Point(35, 38), new Size(5, 2), color);
            _uiContainerMissileLockedCrosshair.Items.Add(crosshair6);
            var crosshair7= new UIRectangle(new Point(0, 35), new Size(2, 3), color);
            _uiContainerMissileLockedCrosshair.Items.Add(crosshair7);
            var crosshair8 = new UIRectangle(new Point(38, 35), new Size(2, 3), color);
            _uiContainerMissileLockedCrosshair.Items.Add(crosshair8);
        }
        
        private void AnimateMissileLockedCrosshair()
        {
            var color1 = Color.FromArgb(220, 255, 50, 50);
            var color2 = Color.FromArgb(220, 255, 205, 0);
            var delta = 0.0;

            if (_missileLockedCrosshairAnimateStopwatch.Elapsed > _missileLockedCrosshairAnimateFrameTime)
            {
                _missileLockedCrosshairColorDelta++;
                _missileLockedCrosshairAnimateStopwatch.Restart();
            }

            if (_missileLockedCrosshairColorDelta > 200)
            {
                _missileLockedCrosshairColorDelta = 0;
                delta = _missileLockedCrosshairColorDelta * 0.01;
            }
            else if (_missileLockedCrosshairColorDelta > 100)
            {
                delta = (200 - _missileLockedCrosshairColorDelta)*0.01;
            }
            else
            {
                delta = _missileLockedCrosshairColorDelta * 0.01;
            }

            var a = color1.A + (color2.A - color1.A)*delta;
            var r = color1.R + (color2.R - color1.R) * delta;
            var g = color1.G + (color2.G - color1.G) * delta;
            var b = color1.B + (color2.B - color1.B) * delta;
            var color = Color.FromArgb((int) a, (int) r, (int) g, (int) b);
            foreach (var el in _uiContainerMissileLockedCrosshair.Items)
            {
                el.Color = color;
            }
        }

        public void Shoot(Vector3 target)
		{
			_drawCrosshair = true;
			var weaponPos = Game.Player.Character.Position;

			//take velocity into account
			if (Game.Player.Character.IsInVehicle())
			{
				var vehicle = Game.Player.Character.CurrentVehicle;
				weaponPos += vehicle.Velocity * 0.06f;
			}

			var fireRateTime = TimeSpan.FromSeconds(0.2);
			if (_shootStopWatch.Elapsed > fireRateTime)
			{
				//Util.PlayAnimation(Game.Player.Character, "weapons@rifle@lo@smg", "fire_med", 8.0f, -1, false, 0);
				//Game.Player.Character.Task.ClearAll();
				//Game.Player.Character.Task.ShootAt(target, (int)fireRateTime.TotalMilliseconds + 50);
				World.ShootBullet(weaponPos, target, Game.Player.Character, new Model(Game.Player.Character.Weapons.Current.Hash), 1);
				_shootStopWatch.Restart();
			}
		}

		public void Tase(Vector3 target)
		{
			_drawCrosshair = true;
			var weaponPos = Game.Player.Character.Position;

			//take velocity into account
			if (Game.Player.Character.IsInVehicle())
			{
				var vehicle = Game.Player.Character.CurrentVehicle;
				weaponPos += vehicle.Velocity * 0.06f;
			}

			var directionVector = (target - weaponPos);
			directionVector.Normalize();
			var shockPos = target - directionVector;

			var fireRateTime = TimeSpan.FromSeconds(0.2);
			if (_shootStopWatch.Elapsed > fireRateTime)
			{
				//Util.PlayAnimation(Game.Player.Character, "weapons@rifle@lo@smg", "fire_med", 8.0f, -1, false, 0);

				World.ShootBullet(shockPos, target, Game.Player.Character, WeaponHash.StunGun, 1);
				_shootStopWatch.Restart();
			}
		}

		public void ShootMissile(Vector3 target)
		{
		    _drawCrosshair = true;
			var weaponPos = Game.Player.Character.Position;

			//take velocity into account
			if (Game.Player.Character.IsInVehicle())
			{
				var vehicle = Game.Player.Character.CurrentVehicle;
				weaponPos += vehicle.Velocity * 0.06f;
			}

			var fireRateTime = TimeSpan.FromSeconds(0.2);
			if (_shootStopWatch.Elapsed > fireRateTime)
			{
				//World.ShootBullet(weaponPos, target, Game.Player.Character, WeaponHash.HomingLauncher, 1);
                _homingMissilesHelper.Launch(target);
				_shootStopWatch.Restart();
			}
		}

        public void ShootMissile(Entity target)
        {
            var weaponPos = Game.Player.Character.Position;

            //take velocity into account
            if (Game.Player.Character.IsInVehicle())
            {
                var vehicle = Game.Player.Character.CurrentVehicle;
                weaponPos += vehicle.Velocity * 0.06f;
            }

            var fireRateTime = TimeSpan.FromSeconds(0.2);
            if (_shootStopWatch.Elapsed > fireRateTime)
            {
                //World.ShootBullet(weaponPos, target, Game.Player.Character, WeaponHash.HomingLauncher, 1);
                _homingMissilesHelper.Launch(target);
                _shootStopWatch.Restart();
            }
        }

		public void Incinerate(Vector3 target)
		{
			//var dist = (target - Game.Player.Character.Position).Length();
			//if (dist > 3)
			{
				World.AddExplosion(target, ExplosionType.Molotov1, 2, 0);
			}
		}

		public void Water(Vector3 target)
		{
			_drawCrosshair = true;
			var dist = (target - Game.Player.Character.Position).Length();
			if (dist > 3)
			{
				World.AddExplosion(target, ExplosionType.WaterHydrant, 2, 0);
			}
		}

		public void Process()
		{
			if ((_settings.AimWithGazeEnabled && (GameplayCamera.IsAimCamActive))
                || AlwaysShowCrosshair)
            {
				_drawCrosshair = true;
			}

			if (_drawCrosshair)
			{
				_uiContainerCrosshair.Draw();
            }

		    if (_settings.MissilesAtGazeEnabled && Game.Player.Character.IsInVehicle())
		    {
                _uiContainerMissileLockedCrosshair.Draw();
                AnimateMissileLockedCrosshair();
            }
		    
            _homingMissilesHelper.Process();
			_drawCrosshair = false;
		}

	    public void MoveCrosshair(Vector2 screenCoords)
		{
			var uiWidth = UI.WIDTH;
			var uiHeight = UI.HEIGHT;

			var crosshairPosition = new Vector2(uiWidth * 0.5f + screenCoords.X * uiWidth * 0.5f, uiHeight * 0.5f + screenCoords.Y * uiHeight * 0.5f);
			const float w = 1;//Filtering is done earlier 0.6f;
			_crosshairPosition = new Vector2(_crosshairPosition.X + (crosshairPosition.X - _crosshairPosition.X) * w,
				_crosshairPosition.Y + (crosshairPosition.Y - _crosshairPosition.Y) * w);

			_uiContainerCrosshair.Position = new Point((int)_crosshairPosition.X - _uiContainerCrosshair.Size.Width / 2, (int)_crosshairPosition.Y - -_uiContainerCrosshair.Size.Height / 2);
            _uiContainerMissileLockedCrosshair.Position = new Point((int)_crosshairPosition.X - _uiContainerMissileLockedCrosshair.Size.Width / 2, (int)_crosshairPosition.Y - _uiContainerMissileLockedCrosshair.Size.Height / 2);
        }
	}
}