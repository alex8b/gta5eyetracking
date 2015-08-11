using System.Runtime.InteropServices;

namespace Gta5EyeTracking.HidEmulation
{
	public class MouseEmulation : IHidEmulation
	{
		private double _deltaXOut;
		private double _deltaYOut;

		public double DeltaX
		{
			set
			{
				if (Enabled)
				{
					_deltaXOut = _deltaXOut + value;
				}
			}
		}

		public double DeltaY
		{
			set
			{
				if (Enabled)
				{
					_deltaYOut = _deltaYOut + value;					
				}

			}
		}

		public bool Enabled { get; set; }

		private static MouseApi.MouseKeybdHardwareInputUnion MouseInput(int x, int y, uint data, uint t, uint flag)
		{

			var mi = new MouseApi.MOUSEINPUT { dx = x, dy = y, mouseData = data, time = t, dwFlags = flag };
			var iu = new MouseApi.MouseKeybdHardwareInputUnion
			{
				mi = mi
			};
			return iu;
		}

		public void ProcessInput()
		{
			if ((int)_deltaXOut != 0 || (int)_deltaYOut != 0)
			{
				var input = new MouseApi.INPUT[1];
				input[0].type = MouseApi.INPUT_MOUSE;
				input[0].mkhi = MouseInput((int)_deltaXOut, (int)_deltaYOut, 0, 0, MouseApi.MOUSEEVENTF_MOVE);

				MouseApi.SendInput(1, input, Marshal.SizeOf(input[0].GetType()));

				if ((int)_deltaXOut != 0)
				{
					_deltaXOut = _deltaXOut - (int)_deltaXOut;
				}
				if ((int)_deltaYOut != 0)
				{
					_deltaYOut = _deltaYOut - (int)_deltaYOut;
				}
			}
		}
	}
}
