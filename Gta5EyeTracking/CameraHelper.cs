using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX;

namespace Gta5EyeTracking
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct CViewPortGame
	{
		public unsafe fixed byte _0x0000[588];
		public unsafe fixed float mViewMatrix[16]; //0x024C 
	};//Size=0x028C

	public static class CameraHelper
	{
		private static IntPtr _gPViewPortGame = IntPtr.Zero;
		private static IntPtr GetViewPortGame(IntPtr baseAddress, int length)
		{
			if (_gPViewPortGame == IntPtr.Zero)
			{
				SigScan.Classes.SigScan sigScan = new SigScan.Classes.SigScan(Process.GetCurrentProcess(), baseAddress, length);
				IntPtr matricesManagerInc = sigScan.FindPattern(new byte[] { 0x48, 0x8B, 0x15, 0xFF, 0xFF, 0xFF, 0xFF, 0x48, 0x8D, 0x2D, 0xFF, 0xFF, 0xFF, 0xFF, 0x48, 0x8B, 0xCD }, "xxx????xxx????xxx", 0);
				if (matricesManagerInc != IntPtr.Zero)
				{
					var offset = Marshal.PtrToStructure<int>(new IntPtr(matricesManagerInc.ToInt64() + 3));
					var ptr = new IntPtr(offset + matricesManagerInc.ToInt64() + 7);
					_gPViewPortGame = new IntPtr(Marshal.PtrToStructure<long>(ptr));
				}
			}

			return _gPViewPortGame;
		}

		public static Matrix GetCameraMatrix()
		{
			IntPtr baseAddress = System.Diagnostics.Process.GetCurrentProcess().MainModule.BaseAddress;
			int length = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleMemorySize;

			var viewPortGamePtr = GetViewPortGame(baseAddress, length);

			if (viewPortGamePtr != IntPtr.Zero)
			{
				var viewPortGame = Marshal.PtrToStructure<CViewPortGame>(viewPortGamePtr);
				unsafe
				{
					var matrix = viewPortGame.mViewMatrix;
					return new Matrix(matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5], matrix[6], matrix[7], matrix[8],
						matrix[9], matrix[10], matrix[11], matrix[12], matrix[13], matrix[14], matrix[15]);
				}
			}

			return Matrix.Identity;
		}
	}
}