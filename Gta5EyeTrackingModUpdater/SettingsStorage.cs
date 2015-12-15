using System;
using System.IO;

namespace Gta5EyeTrackingModUpdater
{
	public class SettingsStorage
	{
		private const string SettingsFileName = "updatersettings.xml";

		public Settings LoadSettings()
		{
			var result = new Settings();
			try
			{
				var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Util.SettingsPath);
				var filePath = Path.Combine(folderPath, SettingsFileName);
				System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(Settings));
				var file = new StreamReader(filePath);
				var settings = (Settings)reader.Deserialize(file);
				result = settings;
				file.Close();
			}
			catch (Exception e)
			{
				Util.Log(e.Message);
				//Failed
			}
			return result;
		}

		public void SaveSettings(Settings settings)
		{
			try
			{
				var writer = new System.Xml.Serialization.XmlSerializer(typeof (Settings));
				var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Util.SettingsPath);
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}

				var filePath = Path.Combine(folderPath, SettingsFileName);
				var wfile = new StreamWriter(filePath);
				writer.Serialize(wfile, settings);
				wfile.Close();
			}
			catch (Exception e)
			{
				Util.Log(e.Message);
				//Failed
			}
		}
	}
}