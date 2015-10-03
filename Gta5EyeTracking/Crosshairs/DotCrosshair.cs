using System.Drawing;
using GTA;

namespace Gta5EyeTracking.Crosshairs
{
	public class DotCrosshair: Crosshair
	{
		public DotCrosshair()
		{
			CreateUiContainer(Color.FromArgb(220, 0, 0, 0), Color.FromArgb(220, 255, 255, 255));
		}
		public DotCrosshair(Color outerColor, Color innerColor)
		{
			CreateUiContainer(outerColor, innerColor);
		}

		private void CreateUiContainer(Color outerColor, Color innerColor)
		{
			_uiContainer = new UIContainer(new Point(0, 0), new Size(4, 4), Color.FromArgb(0, 0, 0, 0));
			var crosshair1 = new UIRectangle(new Point(0, 0), new Size(4, 4), outerColor);
			_uiContainer.Items.Add(crosshair1);
			var crosshair2 = new UIRectangle(new Point(1, 1), new Size(2, 2), innerColor);
			_uiContainer.Items.Add(crosshair2);
		}
	}
}