using System.Drawing;
using GTA;

namespace Gta5EyeTracking.Crosshairs
{
	public class DotCrosshair: Crosshair
	{
		public DotCrosshair()
		{
			CreateUiContainer();
		}

		private void CreateUiContainer()
		{
			_uiContainer = new UIContainer(new Point(0, 0), new Size(4, 4), Color.FromArgb(0, 0, 0, 0));
			var crosshair1 = new UIRectangle(new Point(0, 0), new Size(4, 4), Color.FromArgb(220, 0, 0, 0));
			_uiContainer.Items.Add(crosshair1);
			var crosshair2 = new UIRectangle(new Point(1, 1), new Size(2, 2), Color.FromArgb(220, 255, 255, 255));
			_uiContainer.Items.Add(crosshair2);
		}
	}
}