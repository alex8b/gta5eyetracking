using System.Drawing;
using GTA;
using GTA.Math;
using Tobii.EyeX.Client;

namespace Gta5EyeTracking
{
	public class GazeVisualization: DisposableBase
	{
		private Vector2 _lastNormalizedCenterDelta;

		private readonly UIContainer _uiContainerGaze;

		public bool Visible { get; set; }

		public GazeVisualization()
		{
			_uiContainerGaze = new UIContainer(new Point(0, 0), new Size(4, 4), Color.FromArgb(0, 0, 0, 0));
			var crosshair1 = new UIRectangle(new Point(0, 0), new Size(4, 4), Color.FromArgb(220, 255, 0, 0));
			_uiContainerGaze.Items.Add(crosshair1);
			var crosshair2 = new UIRectangle(new Point(1, 1), new Size(2, 2), Color.FromArgb(220, 0, 255, 255));
			_uiContainerGaze.Items.Add(crosshair2);
		}

		public void MovePoint(Vector2 point)
		{
			_lastNormalizedCenterDelta = point;
		}

		public void Process()
		{
			if (!Visible) return;

			var uiWidth = UI.WIDTH;
			var uiHeight = UI.HEIGHT;

			var gazePosition = new Vector2(uiWidth * 0.5f + _lastNormalizedCenterDelta.X * uiWidth * 0.5f - 2, uiHeight * 0.5f + _lastNormalizedCenterDelta.Y * uiHeight * 0.5f - 2);
			_uiContainerGaze.Position = new Point((int)gazePosition.X, (int)gazePosition.Y);

			_uiContainerGaze.Draw();				
		}
	}
}
