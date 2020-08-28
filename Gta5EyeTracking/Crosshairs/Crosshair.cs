using System.Drawing;
using GTA;
using GTA.Math;

namespace Gta5EyeTracking.Crosshairs
{
    public abstract class Crosshair
    {
        protected GTA.UI.ContainerElement UiContainer;

        public void Move(Vector2 crosshairPosition)
        {
            UiContainer.Position = new PointF((int)crosshairPosition.X - UiContainer.Size.Width / 2, (int)crosshairPosition.Y - UiContainer.Size.Height / 2);
        }

        public virtual void Render()
        {
            UiContainer.Draw();
        }
    }
}