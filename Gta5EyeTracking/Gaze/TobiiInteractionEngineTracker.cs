using System;
using EyeXFramework;
using Tobii.EyeX.Framework;

public class TobiiInteractionEngineTracker : ITobiiTracker, IDisposable
{
    public float GazeY { get; private set; }
    public float GazeX { get; private set; }
    public float AspectRatio { get; private set; }

    private EyeXHost _host;
    private GazePointDataStream _lightlyFilteredGazePointDataProvider;

    public TobiiInteractionEngineTracker()
	{
        _host = new EyeXHost();
        _host.Start();
        _lightlyFilteredGazePointDataProvider = _host.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
        _lightlyFilteredGazePointDataProvider.Next += NewGazePoint;

        AspectRatio = 16f / 9f;
	}

    public void Dispose()
    {
        if (_lightlyFilteredGazePointDataProvider != null)
        {
            _lightlyFilteredGazePointDataProvider.Next -= NewGazePoint;
            _lightlyFilteredGazePointDataProvider.Dispose();
            _lightlyFilteredGazePointDataProvider = null;
        }

        if (_host != null)
        {
            _host.Dispose();
            _host = null;
        }
    }

    private void NewGazePoint(object sender, GazePointEventArgs gazePointEventArgs)
    {
        const double screenExtensionFactor = 0;
        var screenExtensionX = _host.ScreenBounds.Value.Width * screenExtensionFactor;
        var screenExtensionY = _host.ScreenBounds.Value.Height * screenExtensionFactor;

        var gazePointX = gazePointEventArgs.X + screenExtensionX / 2;
        var gazePointY = gazePointEventArgs.Y + screenExtensionY / 2;

        var screenWidth = _host.ScreenBounds.Value.Width + screenExtensionX;
        var screenHeight = _host.ScreenBounds.Value.Height + screenExtensionY;

        if (screenHeight > 0)
        {
            AspectRatio = (float) (screenWidth / screenHeight);
        }

        var normalizedGazePointX = (float)Math.Min(Math.Max((gazePointX / screenWidth), 0.0), 1.0);
        var normalizedGazePointY = (float)Math.Min(Math.Max((gazePointY / screenHeight), 0.0), 1.0);

        var normalizedCenterDeltaX = (normalizedGazePointX - 0.5f) * 2.0f;
        var normalizedCenterDeltaY = (normalizedGazePointY - 0.5f) * 2.0f;
        if (float.IsNaN(normalizedCenterDeltaX) || float.IsNaN(normalizedCenterDeltaY)) return;

        GazeX = normalizedCenterDeltaX;
        GazeY = normalizedCenterDeltaY;
    }
}
