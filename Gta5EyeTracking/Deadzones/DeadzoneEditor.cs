using System;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;

namespace Gta5EyeTracking.Deadzones
{
    public class DeadzoneEditor
    {
        private readonly Settings _settings;
        private readonly SettingsMenu _settingsMenu;

        private bool _isDrawingDeadzone = false;
        private Vector2? _firstPoint;
        private Vector2? _secondPoint;

        public DeadzoneEditor(Settings settings, 
            SettingsMenu settingsMenu)
        {
            _settings = settings;
            _settingsMenu = settingsMenu;

            _settingsMenu.DeadzoneMenu.OnItemSelect += (m, item, indx) =>
            {
                if (indx == 0)
                {
                    _isDrawingDeadzone = true;
                }
                else
                {
                    _settings.Deadzones.RemoveAt(indx - 1);
                    _settingsMenu.DeadzoneMenu.RemoveItemAt(indx);
                    _settingsMenu.DeadzoneMenu.RefreshIndex();
                }
            };
        }

        private void DrawDeadzones()
        {
            if (!_settingsMenu.DeadzoneMenu.Visible) return;
            foreach (Deadzone z in _settings.Deadzones)
            {
                var res = UIMenu.GetScreenResolutionMantainRatio();
                Point pos = new Point(Convert.ToInt32(((z.Position.X + 1) / 2) * res.Width), Convert.ToInt32(((z.Position.Y + 1) / 2) * res.Height));
                Vector2 endPoint = new Vector2(z.Position.X + z.Size.Width, z.Position.Y + z.Size.Height);
                Size size = new Size(Convert.ToInt32(((endPoint.X + 1) / 2) * res.Width - pos.X),
                    Convert.ToInt32(((endPoint.Y + 1) / 2) * res.Height - pos.Y));
                new UIResRectangle(pos, size, z.Color).Draw();
            }
        }

        private void DeadzoneCreator()
        {
            if(!_isDrawingDeadzone) return;
            var mouseX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)GTA.Control.CursorX);
            var mouseY = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)GTA.Control.CursorY);
            if (_firstPoint == null && Game.IsControlJustPressed(0, GTA.Control.Attack))
                _firstPoint = new Vector2((mouseX*2) - 1, (mouseY*2) - 1);
            if (_secondPoint == null && Game.IsControlJustReleased(0, GTA.Control.Attack))
                _secondPoint = new Vector2((mouseX*2) - 1, (mouseY*2) - 1);
            if(!_firstPoint.HasValue) return;
            var res = UIMenu.GetScreenResolutionMantainRatio();
            Point pos = new Point(Convert.ToInt32(((_firstPoint.Value.X + 1)/2)*res.Width), Convert.ToInt32(((_firstPoint.Value.Y + 1) / 2) * res.Height));
            Size size;
            size = new Size(Convert.ToInt32(mouseX * res.Width - ((_firstPoint.Value.X + 1) / 2) * res.Width),
                Convert.ToInt32(mouseY * res.Height - ((_firstPoint.Value.Y + 1) / 2) * res.Height));
            new UIResRectangle(pos, size, Color.FromArgb(150, 200, 200, 20)).Draw();
        }

        private void DeadzoneMonitor()
        {
            if(!_isDrawingDeadzone) return;
            if (_firstPoint.HasValue && _secondPoint.HasValue)
            {
                _settings.Deadzones.Add(new Deadzone(_firstPoint.Value.X, _firstPoint.Value.Y, _secondPoint.Value.X - _firstPoint.Value.X, _secondPoint.Value.Y - _firstPoint.Value.Y));
                _settingsMenu.DeadzoneMenu.AddItem(new UIMenuItem("Deadzone #" + _settings.Deadzones.Count, "Select to remove."));
                _settingsMenu.DeadzoneMenu.RefreshIndex();
                _firstPoint = null;
                _secondPoint = null;
                _isDrawingDeadzone = false;
            }
        }

        public void Process()
        {
            DrawDeadzones();
            DeadzoneCreator();
            DeadzoneMonitor();
        }
    }
}