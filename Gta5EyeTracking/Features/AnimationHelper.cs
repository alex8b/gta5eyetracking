using GTA;
using GTA.Native;

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
				return ((Group != null ? Group.GetHashCode() : 0) * 397) ^ (Name != null ? Name.GetHashCode() : 0);
			}
		}
	}

	public class AnimationHelper
	{
		private bool _wasPlayingAnimationLastFrame;
		private bool _wasPlayingAnimationThisFrame;
		private AnimationName _lastAnimation;

		public AnimationHelper()
		{
		}

		public static string GetWeaponAnimationGroup(WeaponHash hash)
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

		public void PlayShootingAnimation(float pitchToTarget)
		{
			var animation = new AnimationName();
			animation.Group = AnimationHelper.GetWeaponAnimationGroup(Game.Player.Character.Weapons.Current.Hash);
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
			_wasPlayingAnimationThisFrame = true;
		}

		public void PlayMindControlAnimation()
		{
			if (Game.Player.Character.IsInVehicle()) return;

			if (_lastAnimation == null)
			{
				var animation = new AnimationName();
				animation.Group = "random@mugging3";
				animation.Name = "handsup_standing_base";
				Util.PlayAnimation(Game.Player.Character, animation.Group, animation.Name, 40.0f, -1, false, 0, true);
				_lastAnimation = animation;
			}
			_wasPlayingAnimationThisFrame = true;
		}

		public void Process()
		{
			_wasPlayingAnimationLastFrame  = _wasPlayingAnimationThisFrame;
			if ((! _wasPlayingAnimationLastFrame 
			|| (Game.Player.Character.Weapons.Current.AmmoInClip  == 0))
				&& ( _lastAnimation  != null))
			{
				Game.Player.Character.Task.ClearAnimation(_lastAnimation.Group, _lastAnimation.Name);
				_lastAnimation = null;
			}
			_wasPlayingAnimationThisFrame = false;
		}
	}
}