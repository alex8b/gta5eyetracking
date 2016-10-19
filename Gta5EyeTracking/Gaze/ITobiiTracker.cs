using System;

public interface ITobiiTracker : IDisposable
{
	float Yaw { get; }
	float Pitch { get; }
	float Roll { get; }
	float X { get; }
	float Y { get; }
	float Z { get; }
	float GazeY { get; }
	float GazeX { get; }
	float AspectRatio { get; }
	bool IsHeadTracking { get; }
}