using System;
using System.Diagnostics;
using Gta5EyeTracking.Crosshairs;
using Gta5EyeTracking.HomingMissiles;
using GTA;
using GTA.Math;
using GTA.Native;
using Tobii.EyeX.Client;

namespace Gta5EyeTracking.Features
{
	public class Aiming: DisposableBase
	{
    	public bool AlwaysShowCrosshair { get; set; }
		public bool MissileLockedCrosshairVisible { get; set; }

		private readonly MissileLockCrosshair _missileLockCrosshair;
		private readonly DotCrosshair _dotCrosshair;

		private readonly Stopwatch _shootStopWatch;

		private bool _drawCrosshair;
		private Vector2 _crosshairPosition;
		private readonly Settings _settings;
	    private readonly HomingMissilesHelper _homingMissilesHelper;

		public Aiming(Settings settings)
		{
			_settings = settings;
			_shootStopWatch = new Stopwatch();
			_shootStopWatch.Restart();

			_homingMissilesHelper = new HomingMissilesHelper();
			_missileLockCrosshair = new MissileLockCrosshair();
			_dotCrosshair = new DotCrosshair();
		}

	    protected override void DisposeManagedResources()
	    {
	        _homingMissilesHelper.Dispose();
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
			target = PutAboveGround(target);
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
		private static Vector3 PutAboveGround(Vector3 shootMissileCoord)
		{
			var playerDistToGround = Game.Player.Character.Position.Z - World.GetGroundHeight(Game.Player.Character.Position);
			var targetDir = shootMissileCoord - Game.Player.Character.Position;
			targetDir.Normalize();
			var justBeforeTarget = shootMissileCoord - targetDir;
			var targetDistToGround = shootMissileCoord.Z - World.GetGroundHeight(justBeforeTarget);
			var distToTarget = (Game.Player.Character.Position - shootMissileCoord).Length();
			if ((playerDistToGround < 2) && (playerDistToGround >= -0.5)) //on the ground 
			{
				if (((targetDistToGround < 2) && (targetDistToGround >= -0.5)) //shoot too low
					|| ((targetDistToGround < 5) && (targetDistToGround >= -0.5) && (distToTarget > 70.0)))
				//far away add near the ground
				{
					shootMissileCoord.Z = World.GetGroundHeight(justBeforeTarget) //ground level at target
										  + playerDistToGround; //offset
				}
			}
			return shootMissileCoord;
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

		public void Process()
		{
			if ((_settings.AimWithGazeEnabled && (GameplayCamera.IsAimCamActive))
                || AlwaysShowCrosshair)
            {
				_drawCrosshair = true;
			}

			if (_drawCrosshair
				&& !MissileLockedCrosshairVisible)
			{
				_dotCrosshair.Render();
			}

		    if (_settings.MissilesAtGazeEnabled 
				&& Game.Player.Character.IsInVehicle()
				&& MissileLockedCrosshairVisible)
		    {
			    _missileLockCrosshair.Render();
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

		    _dotCrosshair.Move(_crosshairPosition);
		    _missileLockCrosshair.Move(_crosshairPosition);
		}
	}
}