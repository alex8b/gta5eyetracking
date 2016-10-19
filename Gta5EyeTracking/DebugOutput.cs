using System.Drawing;
using GTA;

namespace Gta5EyeTracking
{
	public class DebugOutput
	{
		public UIText DebugText1;
		public UIText DebugText2;
		public UIText DebugText3;
		public UIText DebugText4;
		public UIText DebugText5;

	    private UIContainer _uiContainer;

        public DebugOutput()
		{
			CreateDebugWindow();
		}

		public bool Visible { get; set; }					

		private void CreateDebugWindow()
		{
			_uiContainer = new UIContainer(new Point(0, 0), new Size(1280, 720), Color.FromArgb(25, 237, 239, 241));
			_uiContainer.Items.Add(new UIRectangle(new Point(0, 0), new Size(400, 30), Color.FromArgb(255, 26, 188, 156)));
			_uiContainer.Items.Add(new UIText("Tobii Eye tracking", new Point(200, 4), 0.5f, Color.WhiteSmoke, 0, true));
			_uiContainer.Items.Add(new UIRectangle(new Point(0, 30), new Size(400, 150), Color.FromArgb(135, 26, 187, 155)));

			DebugText1 = new UIText("Debug", new Point(200, 34), 0.4f, Color.Black, 0, true);
			_uiContainer.Items.Add(DebugText1);
			DebugText2 = new UIText("Debug", new Point(200, 64), 0.4f, Color.Black, 0, true);
			_uiContainer.Items.Add(DebugText2);
			DebugText3 = new UIText("Debug", new Point(200, 94), 0.4f, Color.Black, 0, true);
			_uiContainer.Items.Add(DebugText3);
			DebugText4 = new UIText("Debug", new Point(200, 124), 0.4f, Color.Black, 0, true);
			_uiContainer.Items.Add(DebugText4);
			DebugText5 = new UIText("Debug", new Point(200, 154), 0.4f, Color.Black, 0, true);
			_uiContainer.Items.Add(DebugText5);
		}

		public void Process()
		{
			if (!Visible) return;
			_uiContainer.Draw();
		}


	}
}