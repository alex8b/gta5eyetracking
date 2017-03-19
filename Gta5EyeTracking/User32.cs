using System;
using System.Runtime.InteropServices;

namespace Gta5EyeTracking
{
	public enum VirtualKeyStates : int
	{
		VK_LBUTTON = 0x01,
		VK_RBUTTON = 0x02,
		VK_CANCEL = 0x03,
		VK_MBUTTON = 0x04,
		//
		VK_XBUTTON1 = 0x05,
		VK_XBUTTON2 = 0x06,

		VK_MENU = 0x12,
		VK_LMENU = 0xA4
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct POINT
	{
		public int x;
		public int y;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}

	public static class User32
	{
		public const int KEY_TOGGLED = 0x1;

		public const int KEY_PRESSED = 0x8000;

		[DllImport("USER32.dll")]
		public static extern short GetKeyState(VirtualKeyStates nVirtKey);

		public static bool IsKeyPressed(VirtualKeyStates nVirtKey)
		{
			return Convert.ToBoolean(GetKeyState(nVirtKey) & KEY_PRESSED);
		}

		[DllImport("user32.dll")]
		public static extern bool GetClientRect(IntPtr hwnd, ref RECT windowClientRect);
	}
}
