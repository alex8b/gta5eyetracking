using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using EasyHook;
using SharpDX.XInput;
using Tobii.EyeX.Client;

namespace Gta5EyeTracking.HidEmulation
{
	public class ModifyStateEventArgs : EventArgs
	{
		public State State { get; set; }

		public ModifyStateEventArgs(State state)
		{
			State = state;
		}
	}

	public class ControllerEmulation
	{
		public event EventHandler<ModifyStateEventArgs> OnModifyState = delegate {  };
		public double DeltaX { get; set; }
		public double DeltaY { get; set; }

		public State ControllerState { get; private set; }
		public bool ControllerConnected { get; private set; }

		public bool Enabled { get; set; }

		private List<LocalHook> _hooks;

		private State _fakeState;
		public ControllerEmulation()
		{
			ControllerState = new State();
			_fakeState = new State();
			CreateHooks();
		}

		private void CreateHooks()
		{
            Util.Log("Begin CreateHooks");
			try
			{
				_hooks = new List<LocalHook>();
				HookXInput();
				HookCom();
				foreach (var hook in _hooks)
				{
					hook.ThreadACL.SetExclusiveACL(new Int32[1]);
				}
			}
			catch
			{
                Util.Log("Failed to create hooks");
			}
            Util.Log("End CreateHooks");
		}

	    public void RemoveHooks()
	    {
			Util.Log("Begin RemoveHooks");
			Thread.Sleep(100);
			if (_hooks == null) return;
            foreach (var hook in _hooks)
            {
                if (hook != null)
                {
                    try
                    {
                        hook.Dispose();
                    }
                    catch
                    {
                        Util.Log("Disposing hook failed.");
                    }
                }
            }
			_hooks.Clear();
			NativeAPI.LhWaitForPendingRemovals();
			Util.Log("End RemoveHooks");
		}


		private void HookXInput()
		{
			try
			{
				var module = NativeAPI.GetModuleHandle("xinput1_3.dll");
				if (IntPtr.Zero == module) return;

				//LocalHook.EnableRIPRelocation();

				var hook = LocalHook.Create(LocalHook.GetProcAddress("xinput1_3.dll", "XInputGetState"),
					new DXInputGetState(XInputGetState_Hooked),
					this);
				
                _hooks.Add(hook);
                //_hooks.Add(LocalHook.Create(LocalHook.GetProcAddress("xinput1_3.dll", "XInputEnable"), 
                //    new DXInputEnable(XInputEnable_Hooked), 
                //    this));
                //_hooks.Add(LocalHook.Create(LocalHook.GetProcAddress("xinput1_3.dll", "XInputGetBatteryInformation"),
                //    new DXInputGetBatteryInformation(XInputGetBatteryInformation_Hooked),
                //    this));
                //_hooks.Add(LocalHook.Create(LocalHook.GetProcAddress("xinput1_3.dll", "XInputGetCapabilities"),
                //    new DXInputGetCapabilities(XInputGetCapabilities_Hooked),
                //    this));
                //_hooks.Add(LocalHook.Create(LocalHook.GetProcAddress("xinput1_3.dll", "XInputGetDSoundAudioDeviceGuids"),
                //    new DXInputGetDSoundAudioDeviceGuids(XInputGetDSoundAudioDeviceGuids_Hooked),
                //    this));
                //_hooks.Add(LocalHook.Create(LocalHook.GetProcAddress("xinput1_3.dll", "XInputGetKeystroke"),
                //    new DXInputGetKeystroke(XInputGetKeystroke_Hooked),
                //    this));
                //_hooks.Add(LocalHook.Create(LocalHook.GetProcAddress("xinput1_3.dll", "XInputSetState"),
                //    new DXInputSetState(XInputSetState_Hooked),
                //    this));
			}
			catch
			{
				//Hooking failed
                Util.Log("Hooking XInput failed.");
			}
		}

		private void HookCom()
		{
			//TODO: hook com, enumerate devices IG_
		}

		const uint ERROR_SUCCESS = 0;
		const uint ERROR_DEVICE_NOT_CONNECTED = 1167;

		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
		delegate uint DXInputGetState(int dwUserIndex, out State pState);
		delegate void DXInputGetStateAsync(int dwUserIndex, out State pState);
		uint XInputGetState_Hooked(int dwUserIndex, out State state)
		{
			state = new State();
			try
			{

				var controller1 = new Controller(UserIndex.One);
				var controller2 = new Controller(UserIndex.Two);
				var controller3 = new Controller(UserIndex.Three);
				var controller4 = new Controller(UserIndex.Four);

				var controller = new Controller((UserIndex)dwUserIndex);

				if (!controller1.IsConnected
					&& !controller2.IsConnected
					&& !controller3.IsConnected
					&& !controller4.IsConnected
					&& ((UserIndex)dwUserIndex == UserIndex.One
						|| (UserIndex)dwUserIndex == UserIndex.Any))
				{
					ControllerConnected = false;
					_fakeState.PacketNumber = _fakeState.PacketNumber++;
					state = _fakeState;
				}
				else if (controller.IsConnected)
				{
					ControllerConnected = controller.IsConnected;
					state = controller.GetState();
				}
				else
				{
					return ERROR_DEVICE_NOT_CONNECTED;
				}

				ControllerState = state;
				if (Enabled)
				{
					state = ProcessState(state);
				}

				return ERROR_SUCCESS;
			}
			catch
			{
				return ERROR_DEVICE_NOT_CONNECTED;
			}
		}

		private State ProcessState(State state)
		{
			var eventArgs = new ModifyStateEventArgs(state);
			OnModifyState(this, eventArgs);

			state = eventArgs.State;

			var rtXorig = state.Gamepad.RightThumbX;
			var rtYorig = state.Gamepad.RightThumbY;
			var deadzone = 8689;
			if (Math.Abs(rtYorig) < deadzone) //deadzone for xbox controller
			{
				rtYorig = 0;
			}

			int rtX = rtXorig + (int)(DeltaX * 32676);
			int rtY = rtYorig + (int)(-DeltaY * 32676);
			rtX = Math.Max(Math.Min(rtX, short.MaxValue), short.MinValue);
			rtY = Math.Max(Math.Min(rtY, short.MaxValue), short.MinValue);

			state.Gamepad.RightThumbX = (short)rtX;
			state.Gamepad.RightThumbY = (short)rtY;

			return state;
		}


		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
		delegate uint DXInputSetState(int dwUserIndex, ref Vibration pVibration);
		delegate void DXInputSetStateAsync(int dwUserIndex, ref Vibration pVibration);
		uint XInputSetState_Hooked(int dwUserIndex, ref Vibration pVibration)
		{
			var controller = new Controller((UserIndex)dwUserIndex);
			if (controller.IsConnected)
			{
				try
				{
					controller.SetVibration(pVibration);
				}
				catch
				{
					return ERROR_DEVICE_NOT_CONNECTED;
				}
			}

			return ERROR_SUCCESS;
		}

		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
		delegate uint DXInputGetKeystroke(int dwUserIndex, int dwReserved, out Keystroke pKeystroke);
		delegate void DXInputGetKeystrokeAsync(int dwUserIndex, int dwReserved, out Keystroke pKeystroke);

		uint XInputGetKeystroke_Hooked(int dwUserIndex, int dwReserved, out Keystroke pKeystroke)
		{
			pKeystroke = new Keystroke();
			var controller = new Controller((UserIndex)dwUserIndex);
			if (controller.IsConnected)
			{
				try
				{
					var result = controller.GetKeystroke(DeviceQueryType.Any, out pKeystroke);
					return (uint) result.Code;
				}
				catch
				{
					return ERROR_DEVICE_NOT_CONNECTED;
				}
			}
			else
			{
				pKeystroke.UserIndex = UserIndex.One;
			}

			return ERROR_SUCCESS;
		}

		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
		delegate uint DXInputGetBatteryInformation(int dwUserIndex, int devType, out BatteryInformation pBatteryInformation);
		delegate void DXInputGetBatteryInformationAsync(int dwUserIndex, int devType, out BatteryInformation pBatteryInformation);
		uint XInputGetBatteryInformation_Hooked(int dwUserIndex, int devType, out BatteryInformation pBatteryInformation)
		{
			pBatteryInformation = new BatteryInformation();

			var controller = new Controller((UserIndex)dwUserIndex);
			if (controller.IsConnected)
			{
				try
				{
					pBatteryInformation = controller.GetBatteryInformation((BatteryDeviceType) devType);
				}
				catch
				{
					return ERROR_DEVICE_NOT_CONNECTED;
				}
			}
			else
			{
				pBatteryInformation.BatteryLevel = BatteryLevel.Full;
			}
			
			return ERROR_SUCCESS;
		}

		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
		delegate uint DXInputGetDSoundAudioDeviceGuids(int dwUserIndex, out Guid pDSoundRenderGuid, out Guid pDSoundCaptureGuid);
		delegate void DXInputGetDSoundAudioDeviceGuidsAsync(int dwUserIndex, out Guid pDSoundRenderGuid, out Guid pDSoundCaptureGuid);
		uint XInputGetDSoundAudioDeviceGuids_Hooked(int dwUserIndex, out Guid pDSoundRenderGuid, out Guid pDSoundCaptureGuid)
		{
			pDSoundRenderGuid = new Guid();
			pDSoundCaptureGuid = new Guid();

			var controller = new Controller((UserIndex)dwUserIndex);
			if (controller.IsConnected)
			{
				try
				{
					pDSoundRenderGuid = controller.SoundRenderGuid;
					pDSoundCaptureGuid = controller.SoundCaptureGuid;
				}
				catch
				{
					return ERROR_DEVICE_NOT_CONNECTED;
				}
			}
			
			return ERROR_SUCCESS;
		}

		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
		delegate uint DXInputEnable(bool enable);
		delegate void DXInputEnableAsync(bool enable);
		uint XInputEnable_Hooked(bool enable)
		{
			Controller.SetReporting(enable);
			
			return ERROR_SUCCESS;
		}

		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
		delegate uint DXInputGetCapabilities(int dwUserIndex, DeviceQueryType dwFlags, out Capabilities pCapabilities);
		delegate void DXInputGetCapabilitiesAsync(int dwUserIndex, DeviceQueryType dwFlags, out Capabilities pCapabilities);
		uint XInputGetCapabilities_Hooked(int dwUserIndex, DeviceQueryType dwFlags, out Capabilities pCapabilities)
		{
			pCapabilities = new Capabilities();
			var controller = new Controller((UserIndex)dwUserIndex);
			if (controller.IsConnected)
			{
				try
				{
					pCapabilities = controller.GetCapabilities(dwFlags);
				}
				catch
				{
					return ERROR_DEVICE_NOT_CONNECTED;
				}
			}
			else 
			{
				if ((UserIndex) dwUserIndex == UserIndex.One
					|| (UserIndex) dwUserIndex == UserIndex.Any)
				{
					pCapabilities.Flags = CapabilityFlags.None;
					pCapabilities.Type = DeviceType.Gamepad;
					pCapabilities.SubType = DeviceSubType.Gamepad;

					pCapabilities.Gamepad.Buttons = GamepadButtonFlags.A | GamepadButtonFlags.B | GamepadButtonFlags.Back |
													GamepadButtonFlags.DPadDown
													| GamepadButtonFlags.DPadLeft | GamepadButtonFlags.DPadRight | GamepadButtonFlags.DPadUp |
													GamepadButtonFlags.LeftShoulder | GamepadButtonFlags.LeftThumb
													| GamepadButtonFlags.RightShoulder | GamepadButtonFlags.RightThumb | GamepadButtonFlags.Start |
													GamepadButtonFlags.X | GamepadButtonFlags.Y;

					pCapabilities.Gamepad.LeftTrigger = 0xFF;
					pCapabilities.Gamepad.RightTrigger = 0xFF;

					pCapabilities.Gamepad.LeftThumbX = short.MaxValue;
					pCapabilities.Gamepad.LeftThumbY = short.MaxValue;
					pCapabilities.Gamepad.RightThumbX = short.MaxValue;
					pCapabilities.Gamepad.RightThumbY = short.MaxValue;

					pCapabilities.Vibration.LeftMotorSpeed = 0xFF;
					pCapabilities.Vibration.RightMotorSpeed = 0xFF;
				}
			}

			return ERROR_SUCCESS;
		}
	}
}