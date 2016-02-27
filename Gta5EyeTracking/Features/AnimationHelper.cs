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

		public static AnimationName GetWeaponAnimation(WeaponHash hash, float pitchToTarget)
		{
			var animation = new AnimationName();
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

			animation.Group = "weapons@rifle@hi@";
			//Heavy
			if (hash == WeaponHash.GrenadeLauncher)
			{
				animation.Group = "weapons@heavy@grenade_launcher";
			}
			if (hash == WeaponHash.RPG)
			{
				animation.Group = "weapons@heavy@rpg";
			}
			if (hash == WeaponHash.Minigun)
			{
				animation.Group = "weapons@heavy@minigun";
			}
			if (hash == WeaponHash.Firework)
			{
				animation.Group = "weapons@heavy@rpg";
			}
			if (hash == WeaponHash.Railgun)
			{
				animation.Group = "weapons@rifle@lo@pump";
			}

			//Pistols
			if (hash == WeaponHash.Pistol)
			{
				animation.Group = "weapons@pistol@";
			}
			if (hash == WeaponHash.CombatPistol)
			{
				animation.Group = "weapons@pistol@";
			}
			if (hash == WeaponHash.Pistol50)
			{
				animation.Group = "weapons@pistol@pistol_50";
			}
			if (hash == WeaponHash.SNSPistol)
			{
				animation.Group = "weapons@pistol@";
			}
			if (hash == WeaponHash.HeavyPistol)
			{
				animation.Group = "weapons@pistol@";
			}
			if (hash == WeaponHash.VintagePistol)
			{
				animation.Group = "weapons@pistol@";
			}
			if (hash == WeaponHash.MarksmanPistol)
			{
				animation.Group = "weapons@pistol@";
			}
			if (hash == WeaponHash.Revolver)
			{
				animation.Group = "weapons@pistol@";
			}
			if (hash == WeaponHash.APPistol)
			{
				animation.Group = "weapons@pistol@ap_pistol";
			}
			if (hash == WeaponHash.StunGun)
			{
				animation.Group = "weapons@pistol@";
			}

			//SMG
			if (hash == WeaponHash.MicroSMG)
			{
				animation.Group = "weapons@submg@micro_smg";
			}
			if (hash == WeaponHash.MachinePistol)
			{
				animation.Group = "weapons@rifle@lo@smg";
			}
			if (hash == WeaponHash.SMG)
			{
				animation.Group = "weapons@rifle@lo@smg";
			}
			if (hash == WeaponHash.AssaultSMG)
			{
				animation.Group = "weapons@submg@assault_smg";
			}
			if (hash == WeaponHash.CombatPDW)
			{
				animation.Group = "weapons@rifle@lo@smg";
			}
			if (hash == WeaponHash.MG)
			{
				animation.Group = "weapons@submg@";
			}
			if (hash == WeaponHash.CombatMG)
			{
				animation.Group = "weapons@submg@";
			}
			if (hash == WeaponHash.Gusenberg)
			{
				animation.Group = "weapons@rifle@lo@smg";
			}

			//Shotguns
			if (hash == WeaponHash.PumpShotgun)
			{
				animation.Group = "weapons@rifle@lo@pump";
			}
			if (hash == WeaponHash.SawnOffShotgun)
			{
				animation.Group = "weapons@rifle@lo@pump";
			}
			if (hash == WeaponHash.BullpupShotgun)
			{
				animation.Group = "weapons@rifle@lo@shotgun_bullpup";
			}
			if (hash == WeaponHash.AssaultShotgun)
			{
				animation.Group = "weapons@rifle@lo@shotgun_assault";
			}
			if (hash == WeaponHash.Musket)
			{
				animation.Group = "weapons@rifle@lo@pump";
			}
			if (hash == WeaponHash.HeavyShotgun)
			{
				animation.Group = "weapons@rifle@lo@pump";
			}

			//Rifles
			if (hash == WeaponHash.AssaultRifle)
			{
				animation.Group = "weapons@rifle@hi@";
			}
			if (hash == WeaponHash.CarbineRifle)
			{
				animation.Group = "weapons@rifle@lo@";
			}
			if (hash == WeaponHash.AdvancedRifle)
			{
				animation.Group = "weapons@rifle@hi@";
			}
			if (hash == WeaponHash.SpecialCarbine)
			{
				animation.Group = "weapons@rifle@lo@";
			}
			if (hash == WeaponHash.BullpupRifle)
			{
				animation.Group = "weapons@rifle@hi@";
			}

			//Sniper
			if (hash == WeaponHash.SniperRifle)
			{
				animation.Group = "weapons@rifle@hi@sniper_rifle";
			}
			if (hash == WeaponHash.HeavySniper)
			{
				animation.Group = "weapons@rifle@lo@sniper_heavy";
			}
			if (hash == WeaponHash.MarksmanRifle)
			{
				animation.Group = "weapons@rifle@hi@sniper_rifle";
			}
			
			return animation;
		}

		public void PlayShootingAnimation(float pitchToTarget)
		{
			var animation = GetWeaponAnimation(Game.Player.Character.Weapons.Current.Hash, pitchToTarget);
			
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