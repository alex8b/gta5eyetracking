using System;
using System.Diagnostics;
using System.Drawing;
using GTA;
using GTA.UI;

namespace Gta5EyeTracking.Crosshairs
{
    public class MissileLockCrosshair : Crosshair
    {
        private int _colorDelta;
        private readonly Stopwatch _animateStopwatch;
        private readonly TimeSpan _animateFrameTime;

        public MissileLockCrosshair()
        {
            _animateStopwatch = new Stopwatch();
            _animateStopwatch.Restart();
            _animateFrameTime = TimeSpan.FromSeconds(0.02);
            CreateUiContainer();
        }

        private void CreateUiContainer()
        {
            UiContainer = new ContainerElement(new Point(0, 0), new Size(40, 40), Color.FromArgb(0, 0, 0, 0));

            var color = Color.FromArgb(220, 255, 50, 50);
            UiContainer.Items.Add(new ContainerElement(new Point(0, 0), new Size(5, 2), color));
            UiContainer.Items.Add(new ContainerElement(new Point(0, 38), new Size(5, 2), color));
            UiContainer.Items.Add(new ContainerElement(new Point(0, 2), new Size(2, 3), color));
            UiContainer.Items.Add(new ContainerElement(new Point(38, 2), new Size(2, 3), color));
            UiContainer.Items.Add(new ContainerElement(new Point(35, 0), new Size(5, 2), color));
            UiContainer.Items.Add(new ContainerElement(new Point(35, 38), new Size(5, 2), color));
            UiContainer.Items.Add(new ContainerElement(new Point(0, 35), new Size(2, 3), color));
            UiContainer.Items.Add(new ContainerElement(new Point(38, 35), new Size(2, 3), color));
        }

        private void Animate()
        {
            var color1 = Color.FromArgb(220, 255, 50, 50);
            var color2 = Color.FromArgb(220, 255, 205, 0);
            var delta = 0.0;

            if (_animateStopwatch.Elapsed > _animateFrameTime)
            {
                _colorDelta++;
                _animateStopwatch.Restart();
            }

            if (_colorDelta > 200)
            {
                _colorDelta = 0;
                delta = _colorDelta * 0.01;
            }
            else if (_colorDelta > 100)
            {
                delta = (200 - _colorDelta) * 0.01;
            }
            else
            {
                delta = _colorDelta * 0.01;
            }

            var a = color1.A + (color2.A - color1.A) * delta;
            var r = color1.R + (color2.R - color1.R) * delta;
            var g = color1.G + (color2.G - color1.G) * delta;
            var b = color1.B + (color2.B - color1.B) * delta;
            var color = Color.FromArgb((int)a, (int)r, (int)g, (int)b);
            foreach (var el in UiContainer.Items)
            {
                el.Color = color;
            }
        }

        public override void Render()
        {
            UiContainer.Draw();
            Animate();
        }
    }
}