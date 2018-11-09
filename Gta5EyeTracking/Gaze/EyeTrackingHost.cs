using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Gta5EyeTracking;
using SharpDX;
using Tobii.GameIntegration.Net;
using Debug = Gta5EyeTracking.Debug;

public class HeadPosition
{
    public float X;
    public float Y;
    public float Z;
}

public class HeadRotation
{
    public float Yaw;
    public float Pitch;
    public float Roll;
}

public class HeadPose
{
    public HeadRotation Rotation;
    public HeadPosition Position;
    public long TimeStampMicroSeconds;
}


public static class TobiiAPI
{
	public static Vector2 GetGazePoint()
	{
		return new Vector2((EyeTrackingHost.GetInstance().GazeX + 1) * 0.5f,
			(EyeTrackingHost.GetInstance().GazeY + 1) * 0.5f);
	}

	public static float AspectRatio
	{
		get { return EyeTrackingHost.GetInstance().AspectRatio; }
	}

	public static bool IsHeadTrackingAvailable
	{
		get { return EyeTrackingHost.GetInstance().IsHeadTrackingAvailable; }
	}

	public static HeadPose GetHeadPose()
	{
		var headPose = new HeadPose();
		headPose.Rotation = new HeadRotation();
		headPose.Rotation.Yaw = EyeTrackingHost.GetInstance().Yaw;
		headPose.Rotation.Pitch = EyeTrackingHost.GetInstance().Pitch;
		headPose.Rotation.Roll = EyeTrackingHost.GetInstance().Roll;

		headPose.Position = new HeadPosition();
		headPose.Position.X = EyeTrackingHost.GetInstance().X;
		headPose.Position.Y = EyeTrackingHost.GetInstance().Y;
		headPose.Position.Z = EyeTrackingHost.GetInstance().Z;

		headPose.TimeStampMicroSeconds = EyeTrackingHost.GetInstance().TimeStampMicroSeconds;
		return headPose;
	}
}

public class EyeTrackingHost : IDisposable
{
	private static EyeTrackingHost _instance;

	public static EyeTrackingHost GetInstance()
	{
		return _instance;
	}
	public long TimeStampMicroSeconds { get; private set; }

	public float Yaw { get; private set; }
	public float Pitch { get; private set; }
	public float Roll { get; private set; }
	public float X { get; private set; }
	public float Y { get; private set; }
	public float Z { get; private set; }
	public float GazeY { get; private set; }
	public float GazeX { get; private set; }

	public float AspectRatio { get; private set; }
	public bool IsHeadTracking { get; private set; }

    private int _frame;

	public EyeTrackingHost()
	{
		_instance = this;

		Debug.Log("TobiiGameIntegrationApi.TrackWindow() before");
		TobiiGameIntegrationApi.TrackWindow(Process.GetCurrentProcess().MainWindowHandle);
		Debug.Log("TobiiGameIntegrationApi.TrackWindow() after");

		AspectRatio = 16f / 9f;
	}

	public void Dispose()
	{
		Debug.Log("TobiiGameIntegrationApi.StopTracking() before");
        TobiiGameIntegrationApi.StopTracking();
		Debug.Log("TobiiGameIntegrationApi.StopTracking() after");
	}

	public bool IsHeadTrackingAvailable
	{
		get
		{
			return TobiiGameIntegrationApi.GetTrackerInfo()?.Capabilities.HasFlag(CapabilityFlags.Head) ?? false;
		}
	}

	// Replace with API call when available.	
	private float GetDisplayAspectratio()
	{
		var displayAspectRatios = new List<float> { (4.0f / 3.0f), (16.0f / 10.0f), (16.0f / 9.0f), (21.0f / 9.0f) };

		var rect = new RECT();
		User32.GetClientRect(Process.GetCurrentProcess().MainWindowHandle, ref rect);
		var displayAreaAspectRatio = (float)(rect.right - rect.left) / (rect.bottom - rect.top);

		var bestDisplayAspectRatioMatch = displayAspectRatios.First();

		float smallestDelta = float.MaxValue;
		foreach (var displayAspectRatio in displayAspectRatios)
		{
			var delta = Math.Abs(displayAspectRatio - displayAreaAspectRatio);
			if (delta < smallestDelta)
			{
				smallestDelta = delta;
				bestDisplayAspectRatioMatch = displayAspectRatio;
			}
		}

		return bestDisplayAspectRatioMatch;
	}

	public void Update()
    {
        TobiiGameIntegrationApi.Update();
		{
			var gazePoints = TobiiGameIntegrationApi.GetGazePoints();
			if (gazePoints.Count > 0)
			{
				GazeX = (Math.Min(Math.Max(gazePoints.Last().X, -1.0f), 1.0f));
				GazeY = -(Math.Min(Math.Max(gazePoints.Last().Y, -1.0f), 1.0f));
			}

			var headPoses = TobiiGameIntegrationApi.GetHeadPoses();
			if (headPoses.Count > 0)
			{
				Yaw = -headPoses.Last().Rotation.Yaw;
				Pitch = headPoses.Last().Rotation.Pitch;
				Roll = headPoses.Last().Rotation.Roll;

				X = headPoses.Last().Position.X;
				Y = headPoses.Last().Position.Y;
				Z = headPoses.Last().Position.Z;

				TimeStampMicroSeconds = headPoses.Last().TimeStampMicroSeconds;
			}
		}

		_frame++;
	    if (_frame > 60)
	    {
	        _frame = 0;
            AspectRatio = GetDisplayAspectratio();
        }
    }
}
