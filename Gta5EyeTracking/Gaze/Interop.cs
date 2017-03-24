using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace Tobii.GameIntegration
{
    public static class Interop
    {
        public const string TobiiGameIntegrationCoreExtensionDll = "Tobii.GameIntegration.dll";

	    private const int BufferSize = 64;
		private static readonly List<GazePoint> GazePointsBuffer = new List<GazePoint>(BufferSize);
		private static readonly List<HeadPose> HeadPosesBuffer = new List<HeadPose>(BufferSize);

		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Start")]
		[return: MarshalAs(UnmanagedType.I1)]
		public static extern bool Start([MarshalAs(UnmanagedType.I1)] bool custom_thread);
		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetWindow")]
		public static extern void SetWindow(IntPtr hWnd);
		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Stop")]
		public static extern void Stop();
		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Update")]
		[return: MarshalAs(UnmanagedType.I1)]
		public static extern bool Update();
		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetNewGazePoints")]
		private static extern void GetNewGazePointsInternal(out IntPtr gazePoints, out int numberOfAvailableGazePoints, UnitType unitType);

	    public static List<GazePoint> GetNewGazePoints(UnitType unitType)
	    {
			GazePointsBuffer.Clear();

			int numberOfAvailableGazePoints;
			IntPtr ptr;
		    GetNewGazePointsInternal(out ptr, out numberOfAvailableGazePoints, unitType);
			var longPtr = ptr.ToInt64();
			for (var i = 0; i < numberOfAvailableGazePoints; i++)
			{
				var currentPtr = new IntPtr(longPtr);
				GazePointsBuffer.Add((GazePoint)Marshal.PtrToStructure(currentPtr, typeof(GazePoint)));
				longPtr += Marshal.SizeOf(typeof(GazePoint));
			}

			return GazePointsBuffer;
		}
		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetNewHeadPoses")]
		private static extern void GetNewHeadPosesInternal(out IntPtr headPoses, out int numberOfAvailableHeadPoses);

	    public static List<HeadPose> GetNewHeadPoses()
	    {
			HeadPosesBuffer.Clear();

			int numberOfAvailableHeadPoses;
			IntPtr ptr;
			GetNewHeadPosesInternal(out ptr, out numberOfAvailableHeadPoses);
			var longPtr = ptr.ToInt64();
			for (var i = 0; i < numberOfAvailableHeadPoses; i++)
			{
				var currentPtr = new IntPtr(longPtr);
				HeadPosesBuffer.Add((HeadPose)Marshal.PtrToStructure(currentPtr, typeof(HeadPose)));
				longPtr += Marshal.SizeOf(typeof(HeadPose));
			}

		    return HeadPosesBuffer;
	    }

		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "CustomThreadCode")]
		public static extern bool CustomThreadCode();
		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsInitialised")]
		[return: MarshalAs(UnmanagedType.I1)]
		public static extern bool IsInitialised();
		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsReady")]
		[return: MarshalAs(UnmanagedType.I1)]
		public static extern bool IsReady();
		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsConnected")]
		[return: MarshalAs(UnmanagedType.I1)]
		public static extern bool IsConnected();
		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetUserPresence")]
		public static extern UserPresence GetUserPresence();
		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "WasUpdated")]
		[return: MarshalAs(UnmanagedType.I1)]
		public static extern bool WasUpdated();
		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "TimeSinceLastGazePacket")]
		public static extern float TimeSinceLastGazePacket();
		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "TimeSinceLastHeadPacket")]
		public static extern float TimeSinceLastHeadPacket();
		[DllImport(TobiiGameIntegrationCoreExtensionDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetScreenSizeMm")]
		public static extern void GetScreenSizeMm(out int width, out int height);
    }

	public enum UnitType : int
	{
		SignedNormalized = 0,	//  gaze position, signed normalized, client window bottom, left = (-1, -1), client window top, right = (1, 1)
		Normalized,				//  gaze position, unsigned normalized, client window bottom, left = (0, 0), client window top, right = (1, 1)
		Mm,						//  gaze position, mm, client window bottom, left = (0, 0), client window top, right = (window_width_mm, window_height_mm)
		Pixels,					//  gaze position, pixel, client window bottom, left = (0, 0), client window top, right = (window_width_pixels, window_height_pixels)
		NumberOfUnitTypes
	}

	public enum UserPresence: int
	{
		Unknown = 0,
		Away,
		Present
	}

	public enum TrackingCapabilities : int
	{
		GazeTracking = 0,
		HeadTracking = 1
	};

	[StructLayout(LayoutKind.Sequential)]
	public struct GazePoint
	{
		public long TimeStampMicroSeconds;
		public float X;
		public float Y;
	}

	[StructLayout(LayoutKind.Sequential)]
    public struct HeadRotation
	{
		public float Yaw;
		public float Pitch;
		public float Roll;
	}

    [StructLayout(LayoutKind.Sequential)]
    public struct HeadPosition
	{
        public float X;
        public float Y;
        public float Z;
    }

	[StructLayout(LayoutKind.Sequential)]
	public struct HeadPose
	{
		public long TimeStampMicroSeconds;
		public HeadRotation Rotation;
		public HeadPosition Position;
	}
}