using System;

public interface ITobiiTracker : IDisposable
{
	float GazeY { get; }
	float GazeX { get; }
	float AspectRatio { get; }
}