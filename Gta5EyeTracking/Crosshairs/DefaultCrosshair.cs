using System.Drawing;

namespace Gta5EyeTracking.Crosshairs
{
	public class DefaultCrosshair: Crosshair
	{
		public DefaultCrosshair()
		{
			CreateUiContainer(Color.FromArgb(180, 0, 0, 0), Color.FromArgb(180, 255, 255, 255));
		}
		public DefaultCrosshair(Color outerColor, Color innerColor)
		{
			CreateUiContainer(outerColor, innerColor);
		}

		private void CreateUiContainer(Color outerColor, Color innerColor)
		{
			UiContainer = new UIContainer(new Point(0, 0), new Size(16, 16), Color.FromArgb(0, 0, 0, 0));

			UiContainer.Items.Add(new UIRectangle(new Point(6, 0), new Size(4, 6), outerColor));
            UiContainer.Items.Add(new UIRectangle(new Point(7, 1), new Size(2, 4), innerColor));
			UiContainer.Items.Add(new UIRectangle(new Point(6, 10), new Size(4, 6), outerColor));
            UiContainer.Items.Add(new UIRectangle(new Point(7, 11), new Size(2, 4), innerColor));
            UiContainer.Items.Add(new UIRectangle(new Point(0, 6), new Size(6, 4), outerColor));
            UiContainer.Items.Add(new UIRectangle(new Point(1, 7), new Size(4, 2), innerColor));
            UiContainer.Items.Add(new UIRectangle(new Point(10, 6), new Size(6, 4), outerColor));
            UiContainer.Items.Add(new UIRectangle(new Point(11, 7), new Size(4, 2), innerColor));
		}
	}
}