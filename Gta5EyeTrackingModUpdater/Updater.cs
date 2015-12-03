using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.IO.Compression;

namespace Gta5EyeTrackingModUpdater
{
	public class Updater
	{
		private readonly UpdaterNotifyIcon _updaterNotifyIcon;
		public const string SettingsPath = "Gta5EyeTracking";

		public Updater(UpdaterNotifyIcon updaterNotifyIcon)
		{
			_updaterNotifyIcon = updaterNotifyIcon;

			ShowNotification("Start");
			UpdateModBundle();
			UpdateScriptHookV();
		}

		public void UpdateScriptHookV()
		{
			var gtaExeFilePath = Path.Combine(GetGtaInstallPath(), "GTA5.exe");

			var installedGtaVersion = new Version(0, 0);
			if (!Util.TryGetFileVersion(gtaExeFilePath, ref installedGtaVersion))
			{
				//Failed to get gta version
				return;
			}

			var supportedGtaVersion = new Version(0, 0);
			var availableScriptHookVVersion = "";
			var scriptHookVDownloadUrlAddress = "";
			if (
				!TryParseScriptHookVWebPage(out supportedGtaVersion, out availableScriptHookVVersion,
					out scriptHookVDownloadUrlAddress))
			{
				//Failed to get script hook version
				return;
			}

			if (installedGtaVersion > supportedGtaVersion)
			{
				RemoveScriptHookV();
				ShowNotification("Script Hook V is temporarly disabled");
				return;
			}

			var installedScriptHookVVersion = "";
			var scriptHookVDllPath = Path.Combine(GetGtaInstallPath(), "ScriptHookV.dll");
			if (File.Exists(scriptHookVDllPath))
			{
				try
				{
					var fileVersionInfo = FileVersionInfo.GetVersionInfo(scriptHookVDllPath);
					installedScriptHookVVersion = fileVersionInfo.ProductVersion;
				}
				catch
				{
					//Can't get script hook v version... continue
				}
			}

			if (installedScriptHookVVersion != availableScriptHookVVersion)
			{
				DownloadScriptHookV(scriptHookVDownloadUrlAddress);
				if (!InstallScriptHookV())
				{
					ShowNotification("Failed to update Script Hook V");
				}
			}
			else if (!IsScriptHookVInstalled())
			{
				if (!InstallScriptHookV())
				{
					ShowNotification("Failed to update Script Hook V");
				}
			}
		}

		private bool IsScriptHookVInstalled()
		{
			var gtaPath = GetGtaInstallPath();
			var scriptHookVDllPath = Path.Combine(gtaPath, "ScriptHookV.dll");
			var dinput8DllPath = Path.Combine(gtaPath, "dinput8.dll");

			return (File.Exists(scriptHookVDllPath) && File.Exists(dinput8DllPath));
		}



		private bool TryParseScriptHookVWebPage(out Version supportedGtaVersion, out string availableScriptHookVVersion,
			out string downloadUrlAddress)
		{
			availableScriptHookVVersion = null;
			supportedGtaVersion = new Version();
			downloadUrlAddress = null;

			var urlAddress = "http://www.dev-c.com/gtav/scripthookv/";
			var webPageText = Util.ReadWebPageContent(urlAddress);

			if (!ParseVersionInfo(ref availableScriptHookVVersion, webPageText)) return false;

			if (!ParseSupportedGtaVersionsInfo(ref supportedGtaVersion, webPageText)) return false;

			if (!ParseDownloadUrlAddressInfo(ref downloadUrlAddress, webPageText, urlAddress)) return false;

			return true;
		}

		private static bool ParseDownloadUrlAddressInfo(ref string downloadUrlAddress, string webPageText, string urlAddress)
		{
			var downloadUrlAddressPattern = @"<tr>\s*<th>Download</th>\s*<td>\s*<a\shref=""([^""]+)""";
			var downloadUrlAddresRegex = new Regex(downloadUrlAddressPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
			var downloadUrlAddresMatch = downloadUrlAddresRegex.Match(webPageText);
			if (downloadUrlAddresMatch.Success)
			{
				var tempDownloadUrlAddress = downloadUrlAddresMatch.Groups[1].Captures[0].Value;
				Uri uri;
				if (Uri.TryCreate(new Uri(urlAddress), tempDownloadUrlAddress, out uri))
				{
					downloadUrlAddress = uri.ToString();
				}
			}
			else
			{
				return false;
			}
			return true;
		}

		private static bool ParseSupportedGtaVersionsInfo(ref Version supportedGtaVersion, string webPageText)
		{
			var gtaVersionsPattern = @"<tr>\s*<th>Supported\spatches</th>\s*<td>\s*([^\s]*)\s*-\s*([^\s]*)\s*</td>\s*</tr>";
			var gtaVersionsRegex = new Regex(gtaVersionsPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
			var gtaVersionsMatch = gtaVersionsRegex.Match(webPageText);
			if (gtaVersionsMatch.Success)
			{
				var fromVersion = gtaVersionsMatch.Groups[1].Captures[0].Value;
				var toVersion = gtaVersionsMatch.Groups[2].Captures[0].Value;

				Version tempSupportedGtaVersion;
				if (Version.TryParse(toVersion, out tempSupportedGtaVersion))
				{
					supportedGtaVersion = tempSupportedGtaVersion;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
			return true;
		}

		private static bool ParseVersionInfo(ref string availableScriptHookVVersion, string webPageText)
		{
			var versionPattern = @"<tr>\s*<th>Version</th>\s*<td>\s*([^\s]*)\s*</td>\s*</tr>";
			var versionRegex = new Regex(versionPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
			var versionMatch = versionRegex.Match(webPageText);
			if (versionMatch.Success)
			{
				availableScriptHookVVersion = versionMatch.Groups[1].Captures[0].Value;
			}
			else
			{
				return false;
			}
			return true;
		}

		private bool InstallScriptHookV()
		{
			var localFilePath = Path.Combine(Util.GetDownloadsPath(), "scripthookv.zip");
			if (!File.Exists(localFilePath)) return false;

			try
			{
				var zipFile = ZipFile.Open(localFilePath, ZipArchiveMode.Read);

				var extractPath = Path.Combine(Util.GetDownloadsPath(), "scripthookv");
				zipFile.ExtractToDirectory(extractPath);

				var scriptHookVDllPath = Path.Combine(extractPath, "bin", "ScriptHookV.dll");
				var dinput8DllPath = Path.Combine(extractPath, "bin", "dinput8.dll");
				var nativeTrainerAsiPath = Path.Combine(extractPath, "bin", "NativeTrainer.asi");

				var gtaPath = GetGtaInstallPath();
				File.Copy(scriptHookVDllPath, gtaPath);
				File.Copy(dinput8DllPath, gtaPath);
				File.Copy(nativeTrainerAsiPath, gtaPath);
			}
			catch
			{
				return false;
				//Failed to install
			}
			return true;
		}

		private void DownloadScriptHookV(string downloadUrlAddress)
		{
			var wc = new WebClient();
			wc.Headers.Add("Referer", "http://www.dev-c.com/gtav/scripthookv/");

			var localFilePath = Path.Combine(Util.GetDownloadsPath(), "scripthookv.zip");
			wc.DownloadFile(downloadUrlAddress, localFilePath);
		}

		private void RemoveScriptHookV()
		{
			var scriptHookVDllPath = Path.Combine(GetGtaInstallPath(), "ScriptHookV.dll");
			if (File.Exists(scriptHookVDllPath))
			{
				try
				{
					File.Delete(scriptHookVDllPath);
				}
				catch
				{
					ShowNotification("Failed to remove Script Hook V");
				}
			}
		}

		private void UpdateModBundle()
		{
			var modDllFilePath = Path.Combine(GetGtaInstallPath(), "scripts", "Gta5EyeTracking.dll");
			var installedModVersion = new Version(0, 0);
			if (!Util.TryGetFileVersion(modDllFilePath, ref installedModVersion))
			{
				//Failed to get mod version
			}

			Version availableModVersion;
			string downloadUrlAddress;
			bool modActive;
			if (!TryParseModInfoWebPage(out downloadUrlAddress, out availableModVersion, out modActive))
			{
				// Failed to read web page
			}

			if (installedModVersion >= availableModVersion)
			{
				//Mod is up to date
				return;
			}

			DownloadModBundle(downloadUrlAddress);
			if (!InstallModBundle())
			{
				ShowNotification("Failed to update Gta V Eye tracking Mod");
			}
		}

		private bool InstallModBundle()
		{
			var localFilePath = Path.Combine(Util.GetDownloadsPath(), "gta5eyetracking_bundle.zip");
			if (!File.Exists(localFilePath)) return false;

			try
			{
				var zipFile = ZipFile.Open(localFilePath, ZipArchiveMode.Read);

				var extractPath = Path.Combine(Util.GetDownloadsPath(), "gta5eyetracking_bundle");
				zipFile.ExtractToDirectory(extractPath);
				Util.DirectoryCopy(extractPath, GetGtaInstallPath(), true);
				//todo: skip script hook files
			}
			catch
			{
				return false;
				//Failed to install
			}
			return true;
		}

		private void DownloadModBundle(string downloadUrlAddress)
		{
			var wc = new WebClient();
			var localFilePath = Path.Combine(Util.GetDownloadsPath(), "gta5eyetracking_bundle.zip");
			wc.DownloadFile(downloadUrlAddress, localFilePath);
		}


		private bool TryParseModInfoWebPage(out string downloadUrlAddress, out Version version, out bool active)
		{
			downloadUrlAddress = null;
			version = new Version(0, 0);
			active = true;

			return true;
		}







		private string GetGtaInstallPath()
		{
			var folderPath = Util.GetGtaInstallPathFromRegistry();
			if (folderPath != null)
			{
				var gtaExePath = Path.Combine(folderPath, "GTA5.exe");
				if (File.Exists(gtaExePath)) return folderPath;
			}

			return @"d:\SteamLibrary\steamapps\common\Grand Theft Auto V\";
			//todo: browse button
		}

		private void ShowNotification(string text)
		{
			_updaterNotifyIcon.ShowNotification(text);
		}




		public void SelfUpdate()
		{
			//if there's a file in a "update folder"
			//if file version is higher than the assembly version
			//exit app start self update in silent mode

			//installer should start the app when finished
		}
	}
}
