namespace Gta5EyeTrackingModUpdater
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public class Settings
	{
		public string GtaPath { get; set; }
		public bool Autostart { get; set; }
		public bool Autoupdate { get; set; }

		public Settings()
		{
			GtaPath = "";
			Autostart = true;
			Autoupdate = true;
		}

	}
}