using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Tobii.EyeX.Client;

namespace Gta5EyeTracking
{
    public interface IForegroundWindowWatcher : IDisposable
    {
        event EventHandler<ForegroundWindowChangedEventArgs> ForegroundWindowChanged;
    }

    public class ForegroundWindowChangedEventArgs : EventArgs
    {
        public bool GameIsForegroundWindow { get; set; }
    }

    internal static class WinEventsNativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();
    }

    internal class ForegroundWindowWatcher : DisposableBase, IForegroundWindowWatcher
    {
        public event EventHandler<ForegroundWindowChangedEventArgs> ForegroundWindowChanged = delegate { };

        private readonly IntPtr _eventHook;
        private IntPtr _gameWindowHandle;
        // This field prevents garbage collection of the delegate
        private readonly WinEventsNativeMethods.WinEventDelegate _callback;

        // These constants are documented at http://msdn.microsoft.com/en-us/library/windows/desktop/dd318066(v=vs.85).aspx
        const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        const uint WINEVENT_OUTOFCONTEXT = 0;

        public ForegroundWindowWatcher()
        {
            _callback = PublishWindowChangeEvent;
            _eventHook = WinEventsNativeMethods.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _callback, 0, 0, WINEVENT_OUTOFCONTEXT);
        }

	    public bool IsWindowForeground()
	    {
		    return Process.GetCurrentProcess().MainWindowHandle == WinEventsNativeMethods.GetForegroundWindow();
	    }

        public void SetGameWindowHandle(IntPtr handle)
        {
			_gameWindowHandle = handle;
        }

        protected override void DisposeManagedResources()
        {
            WinEventsNativeMethods.UnhookWinEvent(_eventHook);
        }

        private void PublishWindowChangeEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
	        _gameWindowHandle = Process.GetCurrentProcess().MainWindowHandle;

            var foregroundIsNowGameHwnd = _gameWindowHandle == hwnd;

            ForegroundWindowChanged(this, new ForegroundWindowChangedEventArgs
            {
                GameIsForegroundWindow = foregroundIsNowGameHwnd
            });
        }
    }
}