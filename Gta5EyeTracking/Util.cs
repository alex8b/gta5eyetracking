using System;
using System.Reflection;
using System.Runtime.InteropServices;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Gta5EyeTracking
{
	public static class Util
	{
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
	}
}