using System;
using System.Runtime.InteropServices;
using GTA;
using GTA.Math;
using GTA.Native;
using Matrix = SharpDX.Matrix;

namespace Gta5EyeTracking
{
	public static class ScriptHookExtensions
	{
		public static void AttachCamToPedBone(Camera camera, Ped ped, int boneIndex, Vector3 offset)
		{
			Function.Call(Hash.ATTACH_CAM_TO_PED_BONE/*_0x61A3DBA14AB7F411*/, camera.Handle, ped.Handle, boneIndex, offset.X, offset.Y, offset.Z, false);
		}

		public static void AttachCamToEntity(Camera camera, Entity entity, Vector3 offset, bool isRelative)
		{
			Function.Call(Hash.ATTACH_CAM_TO_ENTITY/*._0xFEDB7D269E8C60E3*/, camera.Handle, entity.Handle, offset.X, offset.Y, offset.Z, isRelative);
		}

		public static void SetPedShootsAtCoord(Ped ped, Vector3 target)
		{
			Function.Call(Hash.SET_PED_SHOOTS_AT_COORD, ped, target.X, target.Y, target.Z, true);
		}


		public static void PlayAnimation(Ped mPed, string animSet, string animName, float speed, int duration, [MarshalAs(UnmanagedType.U1)] bool lastAnimation, float playbackRate, bool loop)
		{
			Function.Call(Hash.REQUEST_ANIM_DICT, animSet);
			var dateTime = DateTime.UtcNow + new TimeSpan(0, 0, 0, 0, 1000);
			while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animSet))
			{
				Script.Yield();
				if (DateTime.UtcNow >= dateTime)
				{
					return;
				}
			}
			var flags = loop ? 49 : 48;
            Function.Call(Hash.TASK_PLAY_ANIM, mPed.Handle,
				animSet,
				animName,
				speed,
				-8.0f,
				duration,
				flags,
				playbackRate,
				0,
				0,
				0);
		}

		public static bool IsThrowable(WeaponHash hash)
		{
			return hash == WeaponHash.Grenade
				|| hash == WeaponHash.StickyBomb
				|| hash == WeaponHash.ProximityMine
				|| hash == WeaponHash.SmokeGrenade //Tear Gas
				|| hash == WeaponHash.BZGas
				|| hash == WeaponHash.Molotov
				|| hash == WeaponHash.FireExtinguisher
				|| hash == WeaponHash.PetrolCan
				|| hash == WeaponHash.Snowball
				;
		}

		public static bool IsMelee(WeaponHash hash)
		{
			return hash == WeaponHash.Unarmed
				|| hash == WeaponHash.Knife
				|| hash == WeaponHash.Nightstick
				|| hash == WeaponHash.Hammer				
				|| hash == WeaponHash.Bat
				|| hash == WeaponHash.Crowbar
				|| hash == WeaponHash.GolfClub
				|| hash == WeaponHash.Dagger
				|| hash == WeaponHash.Hatchet
				|| hash == WeaponHash.KnuckleDuster
				|| hash == WeaponHash.Machete
				|| hash == WeaponHash.Flashlight
				|| hash == WeaponHash.SwitchBlade
				;
		}

		public static bool IsSniper(WeaponHash hash)
		{
			return hash == WeaponHash.SniperRifle
			    || hash == WeaponHash.HeavySniper
				|| hash == WeaponHash.MarksmanRifle
				|| hash == WeaponHash.HomingLauncher //special
				;
		}

		public static double GetTimeCoef()
	    {
	        return 1.0; // change in slow motion;
	    }

	    public static void PlaySoundFromEntity(int soundId, string soundName, Entity ent, string category)
	    {
	        if (category == "")
	        {
	            Function.Call(Hash.PLAY_SOUND_FROM_ENTITY, soundId, soundName, ent, 0, 0, 0);
	        }
	        else
	        {
	            Function.Call(Hash.PLAY_SOUND_FROM_ENTITY, soundId, soundName, ent, category, 0, 0);
	        }
	    }

        public static void StopSound(int soundId)
        {
            Function.Call(Hash.STOP_SOUND, soundId);
        }

        public static void PtfxRequestAsset(string sAsset)
        {
            Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, sAsset);
        }

        public static int GetFollowPedCamViewMode()
        {
            return Function.Call<int>(Hash.GET_FOLLOW_PED_CAM_VIEW_MODE);
        }

		public static int GetFollowVehicleCamViewMode()
		{
			return Function.Call<int>(Hash.GET_FOLLOW_VEHICLE_CAM_VIEW_MODE);
		}

		public static void SetGamePlayCamRawPitch(float pitch)
        {
            Function.Call(Hash._SET_GAMEPLAY_CAM_RAW_PITCH, pitch);
        }

        public static bool IsEntityAVehicle(Entity entity)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, entity);
        }

        public static bool IsEntityAPed(Entity entity)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_A_PED, entity);
        }

        public static int PtfxStartOnEntity(Entity ent, string sPTFX, string sAsset, Vector3 offset, Vector3 rot, double size = 1.0)
        {
            if (sAsset != "")
            {
                Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, sAsset); 
            }

            return Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_ON_ENTITY, sPTFX, ent, offset.X, offset.Y, offset.Z, rot.X, rot.Y, rot.Z, size, 0, 0, 0);
        }

        public static void PtfxStop(int fxID)
        {
            Function.Call(Hash.STOP_PARTICLE_FX_LOOPED, fxID);
        }

        public static int GetSoundId()
        {
            return Function.Call<int>(Hash.GET_SOUND_ID);
        }
	}
}