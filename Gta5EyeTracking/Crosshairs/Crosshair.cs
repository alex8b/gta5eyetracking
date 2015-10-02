using System.Drawing;
using GTA;
using GTA.Math;

namespace Gta5EyeTracking.Crosshairs
{
	public abstract class Crosshair
	{
		protected UIContainer _uiContainer;
		
		public void Move(Vector2 crosshairPosition)
		{
			_uiContainer.Position = new Point((int)crosshairPosition.X - _uiContainer.Size.Width / 2, (int)crosshairPosition.Y - _uiContainer.Size.Height / 2);
		}

		public virtual void Render()
		{
			_uiContainer.Draw();
		}
	}
}