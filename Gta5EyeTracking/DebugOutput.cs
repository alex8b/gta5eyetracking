using System.Drawing;
using GTA.UI;

namespace Gta5EyeTracking
{
    public class DebugOutput
    {
        public TextElement DebugText1;
        public TextElement DebugText2;
        public TextElement DebugText3;
        public TextElement DebugText4;
        public static TextElement DebugText5;

        private ContainerElement _uiContainer;

        public DebugOutput()
        {
            CreateDebugWindow();
        }

        public bool Visible { get; set; }

        private void CreateDebugWindow()
        {
            _uiContainer = new ContainerElement(new Point(0, 0), new Size(1280, 720), Color.FromArgb(0, 0, 0, 0));
            _uiContainer.Items.Add(new ContainerElement(new Point(0, 0), new Size(400, 30), Color.FromArgb(255, 26, 188, 156)));
            _uiContainer.Items.Add(new TextElement("Tobii Eye Tracking", new Point(200, 4), 0.5f, Color.WhiteSmoke, 0));
            _uiContainer.Items.Add(new ContainerElement(new Point(0, 30), new Size(400, 150), Color.FromArgb(135, 26, 187, 155)));

            DebugText1 = new TextElement("Debug", new Point(200, 34), 0.4f, Color.Black, 0);
            _uiContainer.Items.Add(DebugText1);
            DebugText2 = new TextElement("Debug", new Point(200, 64), 0.4f, Color.Black, 0);
            _uiContainer.Items.Add(DebugText2);
            DebugText3 = new TextElement("Debug", new Point(200, 94), 0.4f, Color.Black, 0);
            _uiContainer.Items.Add(DebugText3);
            DebugText4 = new TextElement("Debug", new Point(200, 124), 0.4f, Color.Black, 0);
            _uiContainer.Items.Add(DebugText4);
            DebugText5 = new TextElement("Debug", new Point(200, 154), 0.4f, Color.Black, 0);
            _uiContainer.Items.Add(DebugText5);
        }

        public void Process()
        {
            if (!Visible) return;
            _uiContainer.Draw();
        }


    }
}