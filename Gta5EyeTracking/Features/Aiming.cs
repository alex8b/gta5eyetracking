using System;
using System.Diagnostics;
using Gta5EyeTracking.Crosshairs;
using Gta5EyeTracking.HomingMissiles;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Gta5EyeTracking.Features
{
	public class Aiming: IDisposable
	{
		public bool IsMissileLockedCrosshairVisible { get; private set; }
        public bool IsCrosshairVisible { get; private set; }
        public Vector2 CrosshairPosition { get; private set; }
	    public bool IsCrosshairAtCenter { get; set; }

	    private readonly MissileLockCrosshair _missileLockCrosshair;
		private readonly DefaultCrosshair _dotCrosshair;

		private readonly Stopwatch _shootStopWatch;

		private Vector2 _crosshairPosition;
		private readonly Settings _settings;
		private readonly AnimationHelper _animationHelper;
	    private readonly GameState _gameState;

	    private readonly HomingMissilesHelper _homingMissilesHelper;
		private DateTime _lastTime;
		private TimeSpan _timeDelta;

		public Aiming(Settings settings, AnimationHelper animationHelper, GameState gameState)
		{
			_settings = settings;
			_animationHelper = animationHelper;
		    _gameState = gameState;

		    _shootStopWatch = new Stopwatch();
			_shootStopWatch.Restart();

			_homingMissilesHelper = new HomingMissilesHelper();
			_missileLockCrosshair = new MissileLockCrosshair();
			_dotCrosshair = new DefaultCrosshair();
		}

		public void Dispose()
	    {
	        _homingMissilesHelper.Dispose();
	    }

		public void Shoot(Vector3 target)
		{
			IsCrosshairVisible = true;
			var weaponPos = Game.Player.Character.Position;

			//take velocity into account
			if (Game.Player.Character.IsInVehicle())
			{
				var vehicle = Game.Player.Character.CurrentVehicle;
				weaponPos += vehicle.Velocity*0.06f;
				var fireRateTime = TimeSpan.FromSeconds(0.2);
				if (_shootStopWatch.Elapsed > fireRateTime)
				{
					World.ShootBullet(weaponPos, target, Game.Player.Character, new Model(Game.Player.Character.Weapons.Current.Hash),
						1);
					_shootStopWatch.Restart();
				}
			}
			else
			{
				var dir = target - Game.Player.Character.GetBoneCoord(Bone.SKEL_R_Hand);
				var pitchToTarget = Geometry.DirectionToRotation(dir).X;

				_animationHelper.PlayShootingAnimation(pitchToTarget);

				ScriptHookExtensions.SetPedShootsAtCoord(Game.Player.Character, target);
				RotatePlayerCharacterTowardsTarget(target);
			}
		}

		public void ShootBullet(Vector3 target)
		{
			IsCrosshairVisible = true;
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
				World.ShootBullet(weaponPos, target, Game.Player.Character, Game.Player.Character.Weapons.Current.Hash, 1);
				_shootStopWatch.Restart();
			}
			if (!Game.Player.Character.IsInVehicle())
			{
				_animationHelper.PlayMindControlAnimation();
			}
		}

		public void RotatePlayerCharacterTowardsTarget(Vector3 target)
		{
			var dir = target - Game.Player.Character.GetBoneCoord(Bone.SKEL_R_Hand);
			var headingToTarget = Geometry.DirectionToRotation(dir).Z;
			if (!(Game.Player.Character.IsWalking
			      || Game.Player.Character.IsRunning)
			    && dir.Length() > 1.5)
			{
				var deltaHeading = headingToTarget - Game.Player.Character.Heading;
			    var deltaHeadingBound = Geometry.BoundRotationDeg(deltaHeading);
				var velocity = 6;
				Game.Player.Character.Heading += deltaHeadingBound * velocity*(float) _timeDelta.TotalSeconds;
			}
		}

		public void Tase(Vector3 target)
		{
			IsCrosshairVisible = true;
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
				World.ShootBullet(shockPos, target, Game.Player.Character, WeaponHash.StunGun, 1);
				_shootStopWatch.Restart();				
			}
			if (!Game.Player.Character.IsInVehicle())
			{
				_animationHelper.PlayMindControlAnimation();
			}
		}

		public void ShootMissile(Vector3 target)
		{
			target = PutAboveGround(target);
			IsCrosshairVisible = true;
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
			if (!Game.Player.Character.IsInVehicle())
			{
				RotatePlayerCharacterTowardsTarget(target);
				_animationHelper.PlayMindControlAnimation();
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
			if (!Game.Player.Character.IsInVehicle())
			{
				RotatePlayerCharacterTowardsTarget(target.Position);
				_animationHelper.PlayMindControlAnimation();
			}
		}

		public void Incinerate(Vector3 target)
		{
			//var dist = (target - Game.Player.Character.Position).Length();
			//if (dist > 3)
			World.AddExplosion(target, ExplosionType.Molotov1, 2, 0);
			if (!Game.Player.Character.IsInVehicle())
			{
				_animationHelper.PlayMindControlAnimation();
			}
		}

		public void Update(Vector3 shootCoord, Entity missileTarget, bool isShootAtCenter)
		{
            var time = DateTime.UtcNow;
            _timeDelta = time - _lastTime;
            _lastTime = time;

		    IsCrosshairAtCenter = isShootAtCenter;

            if (_settings.MissilesAtGazeEnabled
                && Game.Player.Character.IsInVehicle()
                && missileTarget != null)
            {
                Vector2 screenCoords;
                if (Geometry.WorldToScreenRel(missileTarget.Position, out screenCoords))
                {
                    MoveCrosshair(screenCoords);
                    IsMissileLockedCrosshairVisible = true;
                }
            }
            else
            {
                Vector2 screenCoords;
                if (Geometry.WorldToScreenRel(shootCoord, out screenCoords))
                {
                    MoveCrosshair(screenCoords);
                    IsMissileLockedCrosshairVisible = false;
                }
            }



		    if ((_settings.ExtendedViewEnabled || _settings.FireAtGazeEnabled)
				&& !(!_gameState.IsInVehicle && GameState.IsFirstPersonPedCameraActive())
				&& (GameplayCamera.IsAimCamActive 
					|| _gameState.IsAimingWithMouse
					|| _gameState.IsAimingWithGamepad
					|| _gameState.IsShootingWithMouse
					|| _gameState.IsShootingWithGamepad
					|| (_settings.AlwaysShowCrosshairEnabled && !_gameState.IsInVehicle))
				&& !_gameState.IsMeleeWeapon
				&& !_gameState.IsSniperWeaponAndZoomed
				&& !_gameState.IsInRadialMenu
				&& !_gameState.IsInCharacterSelectionMenu)
		    {
		        IsCrosshairVisible = true;
		    }
		    else
		    {
                IsCrosshairVisible = false;
            }

			if (IsCrosshairVisible)
			{
				UI.HideHudComponentThisFrame((HudComponent)14);
			}

			if (IsCrosshairVisible
				&& !IsMissileLockedCrosshairVisible)
			{
				_dotCrosshair.Render();
			}

		    if (_settings.MissilesAtGazeEnabled 
				&& Game.Player.Character.IsInVehicle()
				&& IsMissileLockedCrosshairVisible)
		    {
			    _missileLockCrosshair.Render();
            }

            _homingMissilesHelper.Process();
		}

        private void MoveCrosshair(Vector2 screenCoords)
	    {
	        CrosshairPosition = screenCoords;

			var crosshairPosition = new Vector2(UI.WIDTH * 0.5f + screenCoords.X * UI.WIDTH * 0.5f, UI.HEIGHT * 0.5f + screenCoords.Y * UI.HEIGHT * 0.5f);
			const float w = 1f;//Filtering is done earlier 0.6f;
			_crosshairPosition = new Vector2(_crosshairPosition.X + (crosshairPosition.X - _crosshairPosition.X) * w,
				_crosshairPosition.Y + (crosshairPosition.Y - _crosshairPosition.Y) * w);
	        var centerPosition = new Vector2(UI.WIDTH*0.5f, UI.HEIGHT*0.5f);
	        var deadzone = 2f;
            if ((_crosshairPosition - centerPosition).Length() < deadzone)
            {
                _crosshairPosition = centerPosition;
            }
		    _dotCrosshair.Move(_crosshairPosition);
		    _missileLockCrosshair.Move(_crosshairPosition);
		}
	}
}