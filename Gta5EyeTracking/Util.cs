using System;
using System.IO;
using System.Runtime.InteropServices;
using GTA;
using GTA.Math;
using GTA.Native;
using Matrix = SharpDX.Matrix;
using MemoryAccess;

namespace Gta5EyeTracking
{
	public static class Util
	{
        public const string SettingsPath = "Gta5EyeTracking";

		public static void SetPedShootsAtCoord(Ped ped, Vector3 target)
		{
			Function.Call(Hash.SET_PED_SHOOTS_AT_COORD, new InputArgument[5]
				  {
					new InputArgument(ped),
					new InputArgument(target.X),
					new InputArgument(target.Y),
					new InputArgument(target.Z),
					new InputArgument(true)
				  }
			);
		}


		public static void PlayAnimation(Ped mPed, string animSet, string animName, float speed, int duration, [MarshalAs(UnmanagedType.U1)] bool lastAnimation, float playbackRate)
		{
			var inputArgumentArray1 = new InputArgument[1];
			InputArgument inputArgument1 = new InputArgument(animSet);
			inputArgumentArray1[0] = inputArgument1;
			Function.Call(Hash.REQUEST_ANIM_DICT, inputArgumentArray1);
			DateTime dateTime = DateTime.Now + new TimeSpan(0, 0, 0, 0, 1000);
			InputArgument[] inputArgumentArray2 = new InputArgument[1];
			InputArgument inputArgument2 = new InputArgument(animSet);
			inputArgumentArray2[0] = inputArgument2;
			if (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, inputArgumentArray2))
			{
				InputArgument[] inputArgumentArray3;
				do
				{
					Script.Yield();
					if (!(DateTime.Now >= dateTime))
					{
						inputArgumentArray3 = new InputArgument[1];
						InputArgument inputArgument3 = new InputArgument(animSet);
						inputArgumentArray3[0] = inputArgument3;
					}
					else
						goto label_4;
				}
				while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, inputArgumentArray3));
				goto label_3;
			label_4:
				return;
			}
		label_3:
			var inputArgumentArray4 = new InputArgument[11];
			var inputArgument4 = new InputArgument(mPed.Handle);
			inputArgumentArray4[0] = inputArgument4;
			var inputArgument5 = new InputArgument(animSet);
			inputArgumentArray4[1] = inputArgument5;
			var inputArgument6 = new InputArgument(animName);
			inputArgumentArray4[2] = inputArgument6;
			var inputArgument7 = new InputArgument(speed);
			inputArgumentArray4[3] = inputArgument7;
			var inputArgument8 = new InputArgument(-8.0f);
			inputArgumentArray4[4] = inputArgument8;
			var inputArgument9 = new InputArgument(duration);
			inputArgumentArray4[5] = inputArgument9;
			var inputArgument10 = new InputArgument(48);
			inputArgumentArray4[6] = inputArgument10;
			InputArgument inputArgument11 = new InputArgument(playbackRate);
			inputArgumentArray4[7] = inputArgument11;
			InputArgument inputArgument12 = new InputArgument(0);
			inputArgumentArray4[8] = inputArgument12;
			InputArgument inputArgument13 = new InputArgument(0);
			inputArgumentArray4[9] = inputArgument13;
			InputArgument inputArgument14 = new InputArgument(0);
			inputArgumentArray4[10] = inputArgument14;
			Function.Call(Hash.TASK_PLAY_ANIM, inputArgumentArray4);
		}

		public static bool IsThrowable(Weapon weapon)
		{
			return weapon.Hash == WeaponHash.Grenade
					|| weapon.Hash == WeaponHash.Molotov
					|| weapon.Hash == WeaponHash.SmokeGrenade
					|| weapon.Hash == WeaponHash.Snowball;
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

        public static void Log(string message)
	    {
		    var now = DateTime.Now;
            var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingsPath);
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

		
            var logpath = Path.Combine(folderPath, "log.txt");

		    try
		    {
			    var fs = new FileStream(logpath, FileMode.Append, FileAccess.Write, FileShare.Read);
			    var sw = new StreamWriter(fs);

			    try
			    {
				    sw.Write("[" + now.ToString("HH:mm:ss") + "] ");

					sw.Write(message);

				    sw.WriteLine();
			    }
			    finally
			    {
				    sw.Close();
				    fs.Close();
			    }
		    }
		    catch
		    {
			    return;
		    }
	    }

        public static Matrix GetCameraMatrix()
        {
            IntPtr baseAddress = System.Diagnostics.Process.GetCurrentProcess().MainModule.BaseAddress;
            int length = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleMemorySize;
            Matrix matrix = CCamera.GetCurrentCameraMatrix(baseAddress, length);

            if (matrix != Matrix.Zero)
                return matrix;
            else
            {
                Log("ERROR: Matrix haven't returned anything!");
                return Matrix.Identity;
            }

        }
    }
}