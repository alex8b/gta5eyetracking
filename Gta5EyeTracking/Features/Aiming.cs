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
	public class AnimationName
	{
		public string Group;
		public string Name;

		public bool Equals(AnimationName other)
		{
			if (other == null) return false;
			return string.Equals(Group, other.Group) && string.Equals(Name, other.Name);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Group != null ? Group.GetHashCode() : 0)*397) ^ (Name != null ? Name.GetHashCode() : 0);
			}
		}
	}

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

		private bool _wasPlayingAnimationLastFrame;
		private bool _wasPlayingAnimationThisFrame;
		private AnimationName _lastAnimation;
		public Aiming(Settings settings)
		{
			_settings = settings;
			_shootStopWatch = new Stopwatch();
			_shootStopWatch.Restart();

			_homingMissilesHelper = new HomingMissilesHelper();
			_missileLockCrosshair = new MissileLockCrosshair();
			_dotCrosshair = new DotCrosshair();
			_lastAnimation = new AnimationName();
		}

	    protected override void DisposeManagedResources()
	    {
	        _homingMissilesHelper.Dispose();
	    }

		public string GetWeaponGroup(WeaponHash hash)
		{
			if (hash == WeaponHash.Minigun)
			{
				return "weapons@heavy@minigun";
			}
			if (hash == WeaponHash.GrenadeLauncher)
			{
				return "weapons@heavy@grenade_launcher";
			}
			if (hash == WeaponHash.RPG)
			{
				return "weapons@heavy@rpg";
			}
			if (hash == WeaponHash.RPG)
			{
				return "weapons@heavy@rpg";
			}
			if (hash == WeaponHash.APPistol)
			{
				return "weapons@pistol@ap_pistol";
			}
			if (hash == WeaponHash.AssaultRifle)
			{
				return "weapons@rifle@hi@";
			}
			if (hash == WeaponHash.SMG)
			{
				return "weapons@rifle@lo@smg";
			}
			return "weapons@rifle@hi@";
		}
		public void Shoot(Vector3 target)
		{
			_drawCrosshair = true;
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
				var headingToTarget = Geometry.DirectionToRotation(dir).Z;
				var pitchToTarget = Geometry.DirectionToRotation(dir).X;

				var animation = new AnimationName();
				animation.Group = GetWeaponGroup(Game.Player.Character.Weapons.Current.Hash);
				if (pitchToTarget < -45)
				{
					animation.Name = "fire_low";
				}
				else if (pitchToTarget > 30)
				{
					animation.Name = "fire_high";
				}
				else
				{
					animation.Name = "fire_med";
				}
				if ((!_wasPlayingAnimationLastFrame
						|| !animation.Equals(_lastAnimation))
					&& (Game.Player.Character.Weapons.Current.AmmoInClip > 0))
				{
					if (_lastAnimation != null)
					{
						Game.Player.Character.Task.ClearAnimation(_lastAnimation.Group, _lastAnimation.Name);
					}
					Util.PlayAnimation(Game.Player.Character, animation.Group, animation.Name, 8.0f, -1, false, 0, true);
					_lastAnimation = animation;
				}

				Util.SetPedShootsAtCoord(Game.Player.Character, target);
				if (!(Game.Player.Character.IsWalking
					|| Game.Player.Character.IsRunning)
					&& dir.Length() > 1.5)
				{
					Game.Player.Character.Heading = headingToTarget;
				}

				_wasPlayingAnimationThisFrame = true;
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
				World.ShootBullet(shockPos, target, Game.Player.Character, WeaponHash.StunGun, 1);
				_shootStopWatch.Restart();				
			}
			PlayMindControlAnimation();
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
			PlayMindControlAnimation();
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
			PlayMindControlAnimation();
        }

		public void Incinerate(Vector3 target)
		{
			//var dist = (target - Game.Player.Character.Position).Length();
			//if (dist > 3)
			World.AddExplosion(target, ExplosionType.Molotov1, 2, 0);
			PlayMindControlAnimation();
		}

		public void Water(Vector3 target)
		{
			_drawCrosshair = true;
			var dist = (target - Game.Player.Character.Position).Length();
			if (dist > 3)
			{
				World.AddExplosion(target, ExplosionType.WaterHydrant, 2, 0);
				PlayMindControlAnimation();
			}
		}

		private void PlayMindControlAnimation()
		{
			if (Game.Player.Character.IsInVehicle()) return;

			if (_lastAnimation == null)
			{
				var animation = new AnimationName();
				animation.Group = "random@mugging3";
				animation.Name = "handsup_standing_base";
				Util.PlayAnimation(Game.Player.Character, animation.Group, animation.Name, 8.0f, -1, false, 0, true);
				_lastAnimation = animation;
			}
			_wasPlayingAnimationThisFrame = true;
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
			var isMeleeWeapon = Util.IsMelee(Game.Player.Character.Weapons.Current.Hash);
			var isThrowableWeapon = Util.IsThrowable(Game.Player.Character.Weapons.Current.Hash);
			if ((_settings.AimWithGazeEnabled 
					&& GameplayCamera.IsAimCamActive
					&& !isMeleeWeapon
					&& !isThrowableWeapon)
                || AlwaysShowCrosshair)
            {
				_drawCrosshair = true;
			}

			if (_drawCrosshair)
			{
				UI.HideHudComponentThisFrame((HudComponent)14);
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
			_wasPlayingAnimationLastFrame = _wasPlayingAnimationThisFrame;
			if ((!_wasPlayingAnimationLastFrame 
					|| (Game.Player.Character.Weapons.Current.AmmoInClip == 0))
				&& (_lastAnimation != null))
			{
				Game.Player.Character.Task.ClearAnimation(_lastAnimation.Group, _lastAnimation.Name);
				_lastAnimation = null;
			}
			_wasPlayingAnimationThisFrame = false;
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