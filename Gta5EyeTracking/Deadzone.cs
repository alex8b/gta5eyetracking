using System.Drawing;
using GTA.Math;

namespace Gta5EyeTracking
{
    public class Deadzone
    {
        public PointF Position { get; set; }
        public SizeF Size { get; set; }

        public Color Color => Color.FromArgb(100, 200, 200, 50);

        public Deadzone(float posX, float posY, float width, float height)
            : this(new PointF(posX, posY), new SizeF(width, height))
        {
        }

        public Deadzone(PointF pos, SizeF size)
        {
            Position = pos;
            Size = size;
        }

        public bool Contains(Vector2 screenCoord)
        {
            return ((screenCoord.X >= Position.X && screenCoord.Y >= Position.Y) && 
                (screenCoord.X < Position.X + Size.Width && screenCoord.Y < Position.Y + Size.Height));
        }
    }
}