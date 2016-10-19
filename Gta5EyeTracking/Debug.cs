using System;
using System.IO;

namespace Gta5EyeTracking
{
	public static class Debug
	{
		public static void Log(string message)
		{
			var now = DateTime.Now;
			var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingsStorage.SettingsPath);
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

		
			var logpath = Path.Combine(folderPath, "log.txt");

			try
			{
				var fs = new FileStream(logpath, FileMode.Append, FileAccess.Write, FileShare.Read);
				var sw = new StreamWriter(fs);

				try
				{
					sw.Write("[" + now.ToString("dd.MM.yyyy HH:mm:ss") + "] ");

					sw.Write(message);

					sw.WriteLine();
				}
				finally
				{
					sw.Close();
					fs.Close();
				}
			}
			catch
			{
				return;
			}
		}
	}
}