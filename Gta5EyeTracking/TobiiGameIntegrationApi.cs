using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Tobii.GameIntegration.Net.Extensions;


namespace Tobii.GameIntegration.Net
{
	public class ExtendedViewSettings{};
	namespace Extensions
	{
		internal static class IntPtrExtensions
		{
			public static bool IsZero(this IntPtr @this)
			{
				return @this.Equals(IntPtr.Zero);
			}

			public static bool IsNotZero(this IntPtr @this)
			{
				return !@this.IsZero();
			}

			public static IntPtr Add(this IntPtr pointer, int offset)
			{
				var pointerInt = IntPtr.Size == 8 ? pointer.ToInt64() : pointer.ToInt32();
				return new IntPtr(pointerInt + offset);
			}

			public static List<T> ToList<T>(this IntPtr pointerToFirstItem, int numberOfItems)
			{
				if(pointerToFirstItem.IsNotZero())
				{
					var items = new List<T>(numberOfItems);
					var itemSize = Marshal.SizeOf(typeof(T));

					for(var i = 0; i < numberOfItems; i++)
					{
						var itemPointer = pointerToFirstItem.Add((i * itemSize));
						items.Add((T) Marshal.PtrToStructure(itemPointer, typeof(T)));
					}

					return items;
				}

				return default(List<T>);
			}

			public static string ToAnsiString(this IntPtr pointerToAnsiString)
			{
				return pointerToAnsiString.IsNotZero() ? Marshal.PtrToStringAnsi(pointerToAnsiString) : default(string);
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct TobiiRectangle
	{
		private int _left;
		private int _top;
		private int _right;
		private int _bottom;

		public Int32 Left
		{
			get { return _left; }
			set { _left = value; }
		}

		public Int32 Top
		{
			get { return _top; }
			set { _top = value; }
		}

		public Int32 Right
		{
			get { return _right; }
			set { _right = value; }
		}

		public Int32 Bottom
		{
			get { return _bottom; }
			set { _bottom = value; }
		}
	}

	public enum TrackerType
	{
		None = 0,
		PC,
		HeadMountedDisplay
	}

    public enum StreamType
    {
        Presence = 0,
        Head = 1,           
        GazeOS = 2,         
        Gaze = 3,
        Foveation = 4,
        EyeInfo = 5,
        HMD = 6,
        UnfilteredGaze = 7,
        Count = 8
    }

	[Flags]
	public enum CapabilityFlags
	{
		None = 0,
		Presence = 1 << 0,
		Head = 1 << 1,
		Gaze = 1 << 2,
		Foveation = 1 << 3,
		EyeInfo = 1 << 4,
		HMD = 1 << 5
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct WidthHeight
	{
		private int _Width;
		private int _Height;

		public int Width => _Width;

		public int Height => _Height;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct TrackerInfoInterop
	{
		private readonly TrackerType _type;
		private readonly CapabilityFlags _capabilities;
		private readonly TobiiRectangle _displayRectInOSCoordinates;
		private readonly WidthHeight _displaySizeMm;
		private readonly IntPtr _urlPointer;
		private readonly IntPtr _friendlyNamePointer;
		private readonly IntPtr _monitorNameInOSPointer;
		private readonly IntPtr _modelNamePointer;
		private readonly IntPtr _generationPointer;
		private readonly IntPtr _serialNumberPointer;
		private readonly IntPtr _firmwareVersionPointer;
		private readonly bool _isAttached;

		public TrackerType Type => _type;
		public CapabilityFlags Capabilities => _capabilities;
		public TobiiRectangle DisplayRectInOSCoordinates => _displayRectInOSCoordinates;
		public WidthHeight DisplaySizeMm => _displaySizeMm;
		public IntPtr Url => _urlPointer;
		public IntPtr FriendlyName => _friendlyNamePointer;
		public IntPtr MonitorNameInOS => _monitorNameInOSPointer;
		public IntPtr ModelName => _modelNamePointer;
		public IntPtr Generation => _generationPointer;
		public IntPtr SerialNumber => _serialNumberPointer;
		public IntPtr FirmwareVersion => _firmwareVersionPointer;
		public bool IsAttached => _isAttached;
	}

	public class TrackerInfo
	{
		internal TrackerInfo(TrackerInfoInterop trackerInfoInterop)
		{
			Type = trackerInfoInterop.Type;
			Capabilities = trackerInfoInterop.Capabilities;
			DisplayRectInOSCoordinates = trackerInfoInterop.DisplayRectInOSCoordinates;
			DisplaySizeMm = trackerInfoInterop.DisplaySizeMm;
			Url = trackerInfoInterop.Url.ToAnsiString();
			FriendlyName = trackerInfoInterop.FriendlyName.ToAnsiString();
			MonitorNameInOS = trackerInfoInterop.MonitorNameInOS.ToAnsiString();
			ModelName = trackerInfoInterop.ModelName.ToAnsiString();
			Generation = trackerInfoInterop.Generation.ToAnsiString();
			SerialNumber = trackerInfoInterop.SerialNumber.ToAnsiString();
			FirmwareVersion = trackerInfoInterop.FirmwareVersion.ToAnsiString();
			IsAttached = trackerInfoInterop.IsAttached;
		}

		internal static TrackerInfo Create(TrackerInfoInterop trackerInfoInterop)
		{
			return new TrackerInfo(trackerInfoInterop);
		}

		public TrackerType Type { get;  }
		public CapabilityFlags Capabilities { get; }
		public TobiiRectangle DisplayRectInOSCoordinates { get; }
		public WidthHeight DisplaySizeMm { get; }
		public string Url { get; }
		public string FriendlyName { get; }
		public string MonitorNameInOS { get; }
		public string ModelName { get; }
		public string Generation { get; }
		public string SerialNumber { get; }
		public string FirmwareVersion { get; }
		public bool IsAttached { get; }
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Rotation
	{
		private float _yaw;
		private float _pitch;
		private float _roll;

		public float Yaw => _yaw;

		public float Pitch => _pitch;

		public float Roll => _roll;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Position
	{
		private float _x;
		private float _y;
		private float _z;

		public float X => _x;

		public float Y => _y;

		public float Z => _z;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct HeadPose
	{
		private Rotation _rotation;
		private Position _position;
		private long _timeStampMicroSeconds;

		public Rotation Rotation => _rotation;

		public Position Position => _position;

		public long TimeStampMicroSeconds => _timeStampMicroSeconds;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct GazePoint
	{
		private long _timeStampMicroSeconds;
		private float _x;
		private float _y;

		public long TimeStampMicroSeconds => _timeStampMicroSeconds;

		public float X => _x;

		public float Y => _y;
	}

	public enum StatisticsLiteral
	{
		IsEnabled,
		IsDisabled,
		ChangedToEnabled,
		ChangedToDisabled,
		GameStarted,
		GameStopped,
		Separator
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Transformation
	{
		private Rotation _rotation;
		private Position _position;

		public Rotation Rotation => _rotation;

		public Position Position => _position;
	}

	public enum HeadViewType: int
	{
		None = 0,
		Direct = 1,
		Dynamic = 2
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SensitivityGradientSettings
	{
		private float _exponent;
		private float _inflectionPoint;
		private float _startPoint;
		private float _endPoint;

		public SensitivityGradientSettings
		(
			float exponent = 1.0f,
			float inflectionPoint = 0.5f,
			float startPoint = 0.0f,
			float endPoint = 1.0f
		)
		{
			_exponent = exponent;
			_inflectionPoint = inflectionPoint;
			_startPoint = startPoint;
			_endPoint = endPoint;
		}

		public float Exponent
		{
			get { return _exponent; }
			set { _exponent = value; }
		}

		public float InflectionPoint
		{
			get { return _inflectionPoint; }
			set { _inflectionPoint = value; }
		}

		public float StartPoint
		{
			get { return _startPoint; }
			set { _startPoint = value; }
		}

		public float EndPoint
		{
			get { return _endPoint; }
			set { _endPoint = value; }
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct HeadViewAutoCenterSettings
	{
		[MarshalAs(UnmanagedType.I1)] private bool _isEnabled;
		private float _normalizeFasterGazeDeadZoneNormalized;
		private float _extendedViewAngleFasterDeadZoneDegrees;
		private float _maxDistanceFromMasterCm;
		private float _maxAngularDistanceDegrees;
		private float _fasterNormalizationFactor;
		private float _positionCompensationSpeed;
		private float _rotationCompensationSpeed;

		public HeadViewAutoCenterSettings
		(
			bool isEnabled = true,
			float normalizeFasterGazeDeadZoneNormalized = 0.35f,
			float extendedViewAngleFasterDeadZoneDegrees = 10.0f,
			float maxDistanceFromMasterCm = 5.0f,
			float maxAngularDistanceDegrees = 15.0f,
			float fasterNormalizationFactor = 100.0f,
			float positionCompensationSpeed = 0.01f,
			float rotationCompensationSpeed = 0.01f
		)
		{
			_isEnabled = isEnabled;
			_normalizeFasterGazeDeadZoneNormalized = normalizeFasterGazeDeadZoneNormalized;
			_extendedViewAngleFasterDeadZoneDegrees = extendedViewAngleFasterDeadZoneDegrees;
			_maxDistanceFromMasterCm = maxDistanceFromMasterCm;
			_maxAngularDistanceDegrees = maxAngularDistanceDegrees;
			_fasterNormalizationFactor = fasterNormalizationFactor;
			_positionCompensationSpeed = positionCompensationSpeed;
			_rotationCompensationSpeed = rotationCompensationSpeed;
		}

		public bool IsEnabled
		{
			get { return _isEnabled; }
			set { _isEnabled = value; }
		}

		public float NormalizeFasterGazeDeadZoneNormalized
		{
			get { return _normalizeFasterGazeDeadZoneNormalized; }
			set { _normalizeFasterGazeDeadZoneNormalized = value; }
		}

		public float ExtendedViewAngleFasterDeadZoneDegrees
		{
			get { return _extendedViewAngleFasterDeadZoneDegrees; }
			set { _extendedViewAngleFasterDeadZoneDegrees = value; }
		}

		public float MaxDistanceFromMasterCm
		{
			get { return _maxDistanceFromMasterCm; }
			set { _maxDistanceFromMasterCm = value; }
		}

		public float MaxAngularDistanceDegrees
		{
			get { return _maxAngularDistanceDegrees; }
			set { _maxAngularDistanceDegrees = value; }
		}

		public float FasterNormalizationFactor
		{
			get { return _fasterNormalizationFactor; }
			set { _fasterNormalizationFactor = value; }
		}

		public float PositionCompensationSpeed
		{
			get { return _positionCompensationSpeed; }
			set { _positionCompensationSpeed = value; }
		}

		public float RotationCompensationSpeed
		{
			get { return _rotationCompensationSpeed; }
			set { _rotationCompensationSpeed = value; }
		}
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct ExtendedViewAdvancedSettings
	{
		private HeadViewType _headViewType;
		private float _aspectRatioCorrectionFactor;
		private float _headViewPitchCorrectionFactor;
		private SensitivityGradientSettings _gazeViewSensitivityGradientSettings;
		private float _gazeViewResponsiveness;
		private float _gazeViewMaxCameraAngleYaw;
		private float _gazeViewMaxCameraAnglePitchUp;
		private float _gazeViewMaxCameraAnglePitchDown;
		private SensitivityGradientSettings _headViewSensitivityGradientSettings;
		private float _headViewResponsiveness;
		private float _headViewMaxCameraAngleYaw;
		private float _headViewMaxCameraAnglePitchUp;
		private float _headViewMaxCameraAnglePitchDown;
		private HeadViewAutoCenterSettings _headViewAutoCenter;
		private float _headSensitivityYaw;
		private float _headSensitivityPitch;

		public static ExtendedViewAdvancedSettings Default()
        {
            return new ExtendedViewAdvancedSettings()
            {
                _headViewType = HeadViewType.Direct,
                _aspectRatioCorrectionFactor = 1.0f,
                _headViewPitchCorrectionFactor = 1.5f,
                _gazeViewSensitivityGradientSettings = new SensitivityGradientSettings(2.25f, 0.8f, 0.0f, 1.0f),
                _gazeViewResponsiveness = 0.5f,
                _gazeViewMaxCameraAngleYaw = 0.236f,
                _gazeViewMaxCameraAnglePitchUp = 0.183f,
                _gazeViewMaxCameraAnglePitchDown = 0.105f,
                _headViewSensitivityGradientSettings = new SensitivityGradientSettings(1.25f, 0.5f, 0.0f, 1.0f),
                _headViewResponsiveness = 1.0f,
                _headViewMaxCameraAngleYaw = 1.335f,
                _headViewMaxCameraAnglePitchUp = 1.038f,
                _headViewMaxCameraAnglePitchDown = 0.593f,
                _headViewAutoCenter = new HeadViewAutoCenterSettings(),
                _headSensitivityYaw = 1.0f,
                _headSensitivityPitch = 1.0f
            };
        }

		public HeadViewType HeadViewType
		{
			get { return _headViewType; }
			set { _headViewType = value; }
		}

		public float AspectRatioCorrectionFactor
		{
			get { return _aspectRatioCorrectionFactor; }
			set { _aspectRatioCorrectionFactor = value; }
		}

		public float HeadViewPitchCorrectionFactor
		{
			get { return _headViewPitchCorrectionFactor; }
			set { _headViewPitchCorrectionFactor = value; }
		}

		public SensitivityGradientSettings GazeViewSensitivityGradientSettings
		{
			get { return _gazeViewSensitivityGradientSettings; }
			set { _gazeViewSensitivityGradientSettings = value; }
		}

		public float GazeViewResponsiveness
		{
			get { return _gazeViewResponsiveness; }
			set { _gazeViewResponsiveness = value; }
		}

		public float GazeViewMaxCameraAngleYaw
		{
			get { return _gazeViewMaxCameraAngleYaw; }
			set { _gazeViewMaxCameraAngleYaw = value; }
		}

		public float GazeViewMaxCameraAnglePitchUp
		{
			get { return _gazeViewMaxCameraAnglePitchUp; }
			set { _gazeViewMaxCameraAnglePitchUp = value; }
		}

		public float GazeViewMaxCameraAnglePitchDown
		{
			get { return _gazeViewMaxCameraAnglePitchDown; }
			set { _gazeViewMaxCameraAnglePitchDown = value; }
		}

		public SensitivityGradientSettings HeadViewSensitivityGradientSettings
		{
			get { return _headViewSensitivityGradientSettings; }
			set { _headViewSensitivityGradientSettings = value; }
		}

		public float HeadViewResponsiveness
		{
			get { return _headViewResponsiveness; }
			set { _headViewResponsiveness = value; }
		}

		public float HeadViewMaxCameraAngleYaw
		{
			get { return _headViewMaxCameraAngleYaw; }
			set { _headViewMaxCameraAngleYaw = value; }
		}

		public float HeadViewMaxCameraAnglePitchUp
		{
			get { return _headViewMaxCameraAnglePitchUp; }
			set { _headViewMaxCameraAnglePitchUp = value; }
		}

		public float HeadViewMaxCameraAnglePitchDown
		{
			get { return _headViewMaxCameraAnglePitchDown; }
			set { _headViewMaxCameraAnglePitchDown = value; }
		}

		public HeadViewAutoCenterSettings HeadViewAutoCenter
		{
			get { return _headViewAutoCenter; }
			set { _headViewAutoCenter = value; }
		}

		public float HeadSensitivityYaw
		{
			get { return _headSensitivityYaw; }
			set { _headSensitivityYaw = value; }
		}

		public float HeadSensitivityPitch
		{
			get { return _headSensitivityPitch; }
			set { _headSensitivityPitch = value; }
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ExtendedViewSimpleSettings
	{
		private float _eyeHeadTrackingRatio;
		private float _cameraMaxAngleYaw;
		private float _cameraMaxAnglePitchUp;
		private float _cameraMaxAnglePitchDown;
		private float _gazeResponsiveness;
		private float _headSensitivityYaw;
		private float _headSensitivityPitch;

		public static ExtendedViewSimpleSettings Default()
        {
            return new ExtendedViewSimpleSettings
            {
                _eyeHeadTrackingRatio = 0.85f,
                _cameraMaxAngleYaw = 90.0f,
                _cameraMaxAnglePitchUp = 70.0f,
                _cameraMaxAnglePitchDown = 40.0f,
                _gazeResponsiveness = 0.5f,
                _headSensitivityYaw = 1.0f,
                _headSensitivityPitch = 1.0f,
            };
        }

		public float EyeHeadTrackingRatio
		{
			get { return _eyeHeadTrackingRatio; }
			set { _eyeHeadTrackingRatio = value; }
		}

		public float CameraMaxAngleYaw
		{
			get { return _cameraMaxAngleYaw; }
			set { _cameraMaxAngleYaw = value; }
		}

		public float CameraMaxAnglePitchUp
		{
			get { return _cameraMaxAnglePitchUp; }
			set { _cameraMaxAnglePitchUp = value; }
		}

		public float CameraMaxAnglePitchDown
		{
			get { return _cameraMaxAnglePitchDown; }
			set { _cameraMaxAnglePitchDown = value; }
		}

		public float GazeResponsiveness
		{
			get { return _gazeResponsiveness; }
			set { _gazeResponsiveness = value; }
		}

		public float HeadSensitivityYaw
		{
			get { return _headSensitivityYaw; }
			set { _headSensitivityYaw = value; }
		}

		public float HeadSensitivityPitch
		{
			get { return _headSensitivityPitch; }
			set { _headSensitivityPitch = value; }
		}
	}

	public static class TobiiGameIntegrationApi
	{
		private static bool Is64BitProcess => IntPtr.Size == 8;

		public static string LoadedDll => Is64BitProcess ? x64.TobiiGameIntegrationDll : x86.TobiiGameIntegrationDll;

		public static void PrelinkAll(){
			if (Is64BitProcess)
			{
				Marshal.PrelinkAll(typeof(x64));
			} 
			else
			{
				Marshal.PrelinkAll(typeof(x86));
			}
		}

		public static void SetApplicationName(string fullApplicationName)
		{
			if (Is64BitProcess)
			{
				x64.SetApplicationName(fullApplicationName);
			}
			else
			{
				x86.SetApplicationName(fullApplicationName);
			}
		}

		#region ITobiiGameIntegrationApi

		public static bool IsApiInitialized()
		{
			if(Is64BitProcess)
			{
				return x64.IsApiInitialized();
			}
			else
			{
				return x86.IsApiInitialized();
			}
		}

		public static void Update()
		{
			if(Is64BitProcess)
			{
				x64.Update();
			}
			else
			{
				x86.Update();
			}
		}

		public static void Shutdown()
		{
			if(Is64BitProcess)
			{
				x64.Shutdown();
			}
			else
			{
				x86.Shutdown();
			}
		}

		#endregion


		#region ITrackerController

		public static TrackerInfo GetTrackerInfo()
		{
			TrackerInfoInterop trackerInfoInterop;
			bool isTrackerInfoAvialable;

			if (Is64BitProcess)
			{
				isTrackerInfoAvialable = x64.GetTrackerInfo(out trackerInfoInterop);
			}
			else
			{
				isTrackerInfoAvialable = x86.GetTrackerInfo(out trackerInfoInterop);
			}
			
			return isTrackerInfoAvialable ? new TrackerInfo(trackerInfoInterop) : default(TrackerInfo);
		}

		public static TrackerInfo GetTrackerInfo(string trackerUrl)
		{
			TrackerInfoInterop trackerInfoInterop;
			bool isTrackerInfoAvialable;

			if (Is64BitProcess)
			{
				isTrackerInfoAvialable = x64.GetTrackerInfoByUrl(trackerUrl, out trackerInfoInterop);
			}
			else
			{
				isTrackerInfoAvialable = x86.GetTrackerInfoByUrl(trackerUrl, out trackerInfoInterop);
			}

			return isTrackerInfoAvialable ? new TrackerInfo(trackerInfoInterop) : default(TrackerInfo);
		}

		public static void UpdateTrackerInfos()
		{
			if (Is64BitProcess)
			{
				x64.UpdateTrackerInfos();
			}
			else
			{
				x86.UpdateTrackerInfos();
			}
		}

		public static List<TrackerInfo> GetTrackerInfos()
		{
			IntPtr trackerInfoInteropsPointer;
			bool hasAsyncUpdateFinished;
			int numberOfTrackerInfos;

			if (Is64BitProcess)
			{
				hasAsyncUpdateFinished = x64.GetTrackerInfos(out trackerInfoInteropsPointer, out numberOfTrackerInfos);
			}
			else
			{
				hasAsyncUpdateFinished = x86.GetTrackerInfos(out trackerInfoInteropsPointer, out numberOfTrackerInfos);
			}

			return hasAsyncUpdateFinished ? trackerInfoInteropsPointer.ToList<TrackerInfoInterop>(numberOfTrackerInfos).Select(TrackerInfo.Create).ToList() : default(List<TrackerInfo>);
		}
		
		public static bool TrackHMD()
		{
			if(Is64BitProcess)
			{
				return x64.TrackHMD();
			}
			else
			{
				return x86.TrackHMD();
			}
		}

		public static bool TrackRectangle(TobiiRectangle rectangle)
		{
			if(Is64BitProcess)
			{
				return x64.TrackRectangle(rectangle);
			}
			else
			{
				return x86.TrackRectangle(rectangle);
			}
		}

		public static bool TrackWindow(IntPtr windowHandle)
		{
			if(Is64BitProcess)
			{
				return x64.TrackWindow(windowHandle);
			}
			else
			{
				return x86.TrackWindow(windowHandle);
			}
		}

		public static bool TrackTracker(string trackerUrl)
		{
			if (Is64BitProcess)
			{
				return x64.TrackTracker(trackerUrl);
			}
			else
			{
				return x86.TrackTracker(trackerUrl);
			}
		}


		public static bool IsTrackerConnected()
		{
			if(Is64BitProcess)
			{
				return x64.IsConnected();
			}
			else
			{
				return x86.IsConnected();
			}
		}

		public static bool IsTrackerEnabled()
		{
			if (Is64BitProcess)
			{
				return x64.IsDeviceEnabled();
			}
			else
			{
				return x86.IsDeviceEnabled();
			}
		}

		public static void StopTracking()
		{
			if(Is64BitProcess)
			{
				x64.StopTracking();
			}
			else
			{
				x86.StopTracking();
			}
		}

		#endregion


		#region IStreamsProvider

		public static bool TryGetLatestHeadPose(out HeadPose headPose)
		{
			if(Is64BitProcess)
			{
				return x64.GetLatestHeadPose(out headPose);
			}
			else
			{
				return x86.GetLatestHeadPose(out headPose);
			}
		}

		public static bool TryGetLatestGazePoint(out GazePoint gazePoint)
		{
			if(Is64BitProcess)
			{
				return x64.GetLatestGazePoint(out gazePoint);
			}
			else
			{
				return x86.GetLatestGazePoint(out gazePoint);
			}
		}

		public static List<GazePoint> GetGazePoints()
		{
			IntPtr gazePoints;
			int numberOfAvailableGazePoints;

			if(Is64BitProcess)
			{
				numberOfAvailableGazePoints = x64.GetGazePoints(out gazePoints);
			}
			else
			{
				numberOfAvailableGazePoints = x86.GetGazePoints(out gazePoints);
			}

			return gazePoints.ToList<GazePoint>(numberOfAvailableGazePoints);
		}

		public static List<HeadPose> GetHeadPoses()
		{
			IntPtr headPoses;
			int numberOfAvailableHeadPoses;

			if(Is64BitProcess)
			{
				numberOfAvailableHeadPoses = x64.GetHeadPoses(out headPoses);
			}
			else
			{
				numberOfAvailableHeadPoses = x86.GetHeadPoses(out headPoses);
			}

			return headPoses.ToList<HeadPose>(numberOfAvailableHeadPoses);
		}

		public static bool IsPresent()
		{
			if(Is64BitProcess)
			{
				return x64.IsPresent();
			}
			else
			{
				return x86.IsPresent();
			}
		}

		public static void SetAutoUnsubscribe(StreamType capability, float timeout)
		{
			if(Is64BitProcess)
			{
				x64.SetAutoUnsubscribe(capability, timeout);
			}
			else
			{
				x86.SetAutoUnsubscribe(capability, timeout);
			}
		}

		public static void UnsetAutoUnsubscribe(StreamType capability)
		{
			if(Is64BitProcess)
			{
				x64.UnsetAutoUnsubscribe(capability);
			}
			else
			{
				x86.UnsetAutoUnsubscribe(capability);
			}
		}

		#endregion


		#region IStatisticsRecorder

		public static string GetStatisticsLiteral(StatisticsLiteral statisticsLiteral)
		{
			IntPtr statisticsLiteralPtr;

			if(Is64BitProcess)
			{
				statisticsLiteralPtr = x64.GetStatisticsLiteral(statisticsLiteral);
			}
			else
			{
				statisticsLiteralPtr = x86.GetStatisticsLiteral(statisticsLiteral);
			}

			return Marshal.PtrToStringAnsi(statisticsLiteralPtr);
		}

		#endregion


		#region IExtendedView

		public static Transformation GetExtendedViewTransformation()
		{
			Transformation extendedViewTransformation;

			if(Is64BitProcess)
			{
				x64.GetExtendedViewTransformation(out extendedViewTransformation);
			}
			else
			{
				x86.GetExtendedViewTransformation(out extendedViewTransformation);
			}

			return extendedViewTransformation;
		}

		public static bool UpdateExtendedViewAdvancedSettings(ExtendedViewAdvancedSettings settings)
		{
			if(Is64BitProcess)
			{
				return x64.UpdateExtendedViewAdvancedSettings(settings);
			}
			else
			{
				return x86.UpdateExtendedViewAdvancedSettings(settings);
			}
		}

		public static void ResetExtendedViewDefaultHeadPose()
		{
			if(Is64BitProcess)
			{
				x64.ResetExtendedViewDefaultHeadPose();
			}
			else
			{
				x86.ResetExtendedViewDefaultHeadPose();
			}
		}

		public static void SetExtendedViewPaused(bool paused)
		{
			if(Is64BitProcess)
			{
				x64.SetExtendedViewPaused(paused);
			}
			else
			{
				x86.SetExtendedViewPaused(paused);
			}
		}

		public static bool GetExtendedViewPaused()
		{
			if(Is64BitProcess)
			{
				return x64.GetExtendedViewPaused();
			}
			else
			{
				return x86.GetExtendedViewPaused();
			}
		}

		public static bool UpdateExtendedViewSimpleSettings(ExtendedViewSimpleSettings settings)
		{
			if(Is64BitProcess)
			{
				return x64.UpdateExtendedViewSimpleSettings(settings);
			}
			else
			{
				return x86.UpdateExtendedViewSimpleSettings(settings);
			}
		}

		public static ExtendedViewAdvancedSettings GetExtendedViewAdvancedSettings()
		{
			ExtendedViewAdvancedSettings settings;
			if(Is64BitProcess)
			{
				x64.GetExtendedViewAdvancedSettings(out settings);
			}
			else
			{
				x86.GetExtendedViewAdvancedSettings(out settings);
			}
			return settings;
		}

		public static ExtendedViewSimpleSettings GetExtendedViewSimpleSettings()
		{
			ExtendedViewSimpleSettings settings;
			if(Is64BitProcess)
			{
				x64.GetExtendedViewSimpleSettings(out settings);
			}
			else
			{
				x86.GetExtendedViewSimpleSettings(out settings);
			}
			return settings;
		}

		#endregion



		private static class x86
		{
#if DEBUG
			internal const string TobiiGameIntegrationDll = "tobii_gameintegration_x86_d.dll";
#else
			internal const string TobiiGameIntegrationDll = "tobii_gameintegration_x86.dll";
#endif
			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetApplicationName([MarshalAs(UnmanagedType.LPStr)]string fullApplicationName);

			#region ITobiiGameIntegrationApi

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsApiInitialized();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void Update();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void Shutdown();

			#endregion


			#region ITrackerController

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetTrackerInfo(out TrackerInfoInterop trackerInfoInterop);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetTrackerInfoByUrl([MarshalAs(UnmanagedType.LPStr)] string trackerUrl, out TrackerInfoInterop trackerInfoInterop);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void UpdateTrackerInfos();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetTrackerInfos(out IntPtr trackerInfosPointer, out int numberOfTrackerInfos);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackHMD();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackRectangle(TobiiRectangle rectangle);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackWindow(IntPtr windowHandle);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackTracker([MarshalAs(UnmanagedType.LPStr)]string trackerUrl);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void StopTracking();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsConnected();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsDeviceEnabled();

			#endregion


			#region IStreamsProvider

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetLatestGazePoint(out GazePoint gazePoint);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetLatestHeadPose(out HeadPose headPose);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int GetGazePoints(out IntPtr gazePoints);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int GetHeadPoses(out IntPtr headPoses);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsPresent();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetAutoUnsubscribe(StreamType capability, float timeout);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void UnsetAutoUnsubscribe(StreamType capability);

			// TODO: Implement
			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern bool GetLatestHMDGaze(out HMDGazePoint hmdGazePoint);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern int GetHMDGaze(out IntPtr hmdGazePoints);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void ConvertGazePoint(GazePoint fromGazePoint, GazePoint toGazePoint, UnitType fromUnit, UnitType toUnit);

			#endregion


			#region IStatisticsRecorder

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr GetStatisticsLiteral(StatisticsLiteral statisticsLiteral);

			#endregion


			#region IFeaturesEnabledSettings

			// TODO: Implement and test

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern bool AreSettingsInitialized();

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void InitializeSettings(Feature[] features, int numberOfFeatures);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern bool GetFeatureEnabled(Feature feature);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void SetFeatureEnabled(Feature feature, bool enabled, bool recordStatistics = true);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void RecordSettingsStateToStatistics();

			#endregion


			#region IExtendedView

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void GetExtendedViewTransformation(out Transformation transformation);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool UpdateExtendedViewAdvancedSettings(ExtendedViewAdvancedSettings settings);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void ResetExtendedViewDefaultHeadPose();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetExtendedViewPaused(bool paused);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetExtendedViewPaused();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool UpdateExtendedViewSimpleSettings(ExtendedViewSimpleSettings settings);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void GetExtendedViewAdvancedSettings(out ExtendedViewAdvancedSettings settings);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void GetExtendedViewSimpleSettings(out ExtendedViewSimpleSettings settings);

			#endregion


			#region IFilters

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern ResponsiveFilterSettings GetResponsiveFilterSettings();

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void SetResponsiveFilterSettings(ResponsiveFilterSettings responsiveFilterSettings);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern AimAtGazeFilterSettings GetAimAtGazeFilterSettings();

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static void SetAimAtGazeFilterSettings(AimAtGazeFilterSettings settings);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static void GetResponsiveFilterGazePoint(out GazePoint gazePoint);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static void GetAimAtGazeFilterGazePoint(out GazePoint gazePoint, out float gazePointStability);

			#endregion

		}

		private static class x64
		{
#if DEBUG
			internal const string TobiiGameIntegrationDll = "tobii_gameintegration_x64_d.dll";
#else
			internal const string TobiiGameIntegrationDll = "tobii_gameintegration_x64.dll";
#endif

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetApplicationName([MarshalAs(UnmanagedType.LPStr)]string fullApplicationName);

			#region ITobiiGameIntegrationApi

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsApiInitialized();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void Update();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void Shutdown();

			#endregion


			#region ITrackerController

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetTrackerInfo(out TrackerInfoInterop trackerInfoInterop);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetTrackerInfoByUrl([MarshalAs(UnmanagedType.LPStr)] string trackerUrl, out TrackerInfoInterop trackerInfoInterop);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void UpdateTrackerInfos();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetTrackerInfos(out IntPtr trackerInfosPointer, out int numberOfTrackerInfos);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackHMD();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackRectangle(TobiiRectangle rectangle);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackWindow(IntPtr windowHandle);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackTracker([MarshalAs(UnmanagedType.LPStr)]string trackerUrl);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void StopTracking();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsConnected();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsDeviceEnabled();

			#endregion


			#region IStreamsProvider

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetLatestGazePoint(out GazePoint gazePoint);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetLatestHeadPose(out HeadPose headPose);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int GetGazePoints(out IntPtr gazePoints);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int GetHeadPoses(out IntPtr headPoses);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsPresent();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetAutoUnsubscribe(StreamType capability, float timeout);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void UnsetAutoUnsubscribe(StreamType capability);

			// TODO: Implement
			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern bool GetLatestHMDGaze(out HMDGazePoint hmdGazePoint);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern int GetHMDGaze(out IntPtr hmdGazePoints);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void ConvertGazePoint(GazePoint fromGazePoint, GazePoint toGazePoint, UnitType fromUnit, UnitType toUnit);

			#endregion


			#region IStatisticsRecorder

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr GetStatisticsLiteral(StatisticsLiteral statisticsLiteral);

			#endregion


			#region IFeaturesEnabledSettings

			// TODO: Implement and test

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern bool AreSettingsInitialized();

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void InitializeSettings(Feature[] features, int numberOfFeatures);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern bool GetFeatureEnabled(Feature feature);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void SetFeatureEnabled(Feature feature, bool enabled, bool recordStatistics = true);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void RecordSettingsStateToStatistics();

			#endregion


			#region IExtendedView

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void GetExtendedViewTransformation(out Transformation transformation);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool UpdateExtendedViewAdvancedSettings(ExtendedViewAdvancedSettings settings);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void ResetExtendedViewDefaultHeadPose();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetExtendedViewPaused(bool paused);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetExtendedViewPaused();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool UpdateExtendedViewSimpleSettings(ExtendedViewSimpleSettings settings);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void GetExtendedViewAdvancedSettings(out ExtendedViewAdvancedSettings settings);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void GetExtendedViewSimpleSettings(out ExtendedViewSimpleSettings settings);

			#endregion


			#region IFilters

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern ResponsiveFilterSettings GetResponsiveFilterSettings();

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void SetResponsiveFilterSettings(ResponsiveFilterSettings responsiveFilterSettings);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern AimAtGazeFilterSettings GetAimAtGazeFilterSettings();

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static void SetAimAtGazeFilterSettings(AimAtGazeFilterSettings settings);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static void GetResponsiveFilterGazePoint(out GazePoint gazePoint);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static void GetAimAtGazeFilterGazePoint(out GazePoint gazePoint, out float gazePointStability);

			#endregion
		}
	}
}