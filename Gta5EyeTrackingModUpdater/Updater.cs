using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Gta5EyeTrackingModUpdater
{
	public class Updater
	{
		private readonly UpdaterNotifyIcon _updaterNotifyIcon;
		public const string SettingsPath = "Gta5EyeTracking";

		public Updater(UpdaterNotifyIcon updaterNotifyIcon)
		{
			_updaterNotifyIcon = updaterNotifyIcon;

			CheckForUpdates();
		}

		public void CheckForUpdates()
		{
			ShowNotification("Checking for updates");
			SelfUpdate();
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

			if (("v" + installedScriptHookVVersion).ToUpperInvariant() != availableScriptHookVVersion.ToUpperInvariant())
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
				if (Directory.Exists(extractPath))
				{
					Directory.Delete(extractPath,true);
				}
				zipFile.ExtractToDirectory(extractPath);

				var scriptHookVDllPath = Path.Combine(extractPath, "bin", "ScriptHookV.dll");
				var dinput8DllPath = Path.Combine(extractPath, "bin", "dinput8.dll");
				var nativeTrainerAsiPath = Path.Combine(extractPath, "bin", "NativeTrainer.asi");

				var gtaPath = GetGtaInstallPath();
				File.Copy(scriptHookVDllPath, Path.Combine(gtaPath, Path.GetFileName(scriptHookVDllPath)), true);
				File.Copy(dinput8DllPath, Path.Combine(gtaPath, Path.GetFileName(dinput8DllPath)), true);
				File.Copy(nativeTrainerAsiPath, Path.Combine(gtaPath, Path.GetFileName(nativeTrainerAsiPath)), true);
			}
			catch(Exception e)
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
			string modBundleDownloadUrl;
			bool blockScriptHookV;
			Version availableUpdaterVersion;
			string updaterDownloadUrl;
			if (!TryParseModInfoWebPage(out availableModVersion, out modBundleDownloadUrl, out blockScriptHookV, out availableUpdaterVersion, out updaterDownloadUrl))
			{
				//Failed to read or parse web page
			}

			if (installedModVersion >= availableModVersion)
			{
				//Mod is up to date
				return;
			}

			DownloadModBundle(modBundleDownloadUrl);
			if (!InstallModBundle())
			{
				ShowNotification("Failed to update Gta V Eye Tracking Mod");
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
				if (Directory.Exists(extractPath))
				{
					Directory.Delete(extractPath, true);
				}
				zipFile.ExtractToDirectory(extractPath);
				Util.DirectoryCopy(extractPath, GetGtaInstallPath(), true, true);
				//todo: skip script hook v files
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


		private bool TryParseModInfoWebPage(out Version modVersion, out string modBundleDownloadUrl, out bool block, out Version updaterVersion, out string updaterDownloadUrl)
		{
			modVersion = new Version(0, 0);
			modBundleDownloadUrl = null;
			block = true;
			updaterVersion = new Version(0, 0);
			updaterDownloadUrl = null;

			var xmlText = Util.ReadWebPageContent("https://raw.githubusercontent.com/alex8b/gta5eyetracking/updater/update.xml");

			var xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(xmlText);


			var modVersionNode = xmlDoc.SelectSingleNode("Update/ModVersion");

			if (!((modVersionNode != null) && (Version.TryParse(modVersionNode.InnerText, out modVersion))))
			{
				return false;
			}

			var modBundleDownloadUrlNode = xmlDoc.SelectSingleNode("Update/ModBundleDownloadUrl");
			if (modBundleDownloadUrlNode != null)
			{
				modBundleDownloadUrl = modBundleDownloadUrlNode.InnerText;
			}
			else
			{
				return false;
			}

			var blockNode = xmlDoc.SelectSingleNode("Update/Block");
			if (!((blockNode != null) && (bool.TryParse(blockNode.InnerText, out block))))
			{
				return false;
			}

			var updaterVersionNode = xmlDoc.SelectSingleNode("Update/UpdaterVersion");

			if (!((updaterVersionNode != null) && (Version.TryParse(updaterVersionNode.InnerText, out updaterVersion))))
			{
				return false;
			}

			var updaterDownloadUrlNode = xmlDoc.SelectSingleNode("Update/UpdaterDownloadUrl");
			if (updaterDownloadUrlNode != null)
			{
				updaterDownloadUrl = updaterDownloadUrlNode.InnerText;
			}
			else
			{
				return false;
			}

			return true;
		}

		public void SelfUpdate()
		{
			var installedUpdaterVersion = Assembly.GetExecutingAssembly().GetName().Version;
            Version availableModVersion;
			string modBundleDownloadUrl;
			bool blockScriptHookV;
			Version availableUpdaterVersion;
			string updaterDownloadUrl;
			if (!TryParseModInfoWebPage(out availableModVersion, out modBundleDownloadUrl, out blockScriptHookV, out availableUpdaterVersion, out updaterDownloadUrl))
			{
				// Failed to read or parse web page
				return;
			}

			if (installedUpdaterVersion >= availableUpdaterVersion)
			{
				//Updater is up to date
				return;
			}

			DownloadModUpdater(updaterDownloadUrl);
			if (!InstallModUpdater())
			{
				ShowNotification("Failed to auto-update");
			}

		}

		private void DownloadModUpdater(string downloadUrlAddress)
		{
			var wc = new WebClient();
			var localFilePath = Path.Combine(Util.GetDownloadsPath(), "gta5eyetrackingmodupdater_installer.exe");
			wc.DownloadFile(downloadUrlAddress, localFilePath);
		}

		private bool InstallModUpdater()
		{
			try
			{
				var exePath = Assembly.GetEntryAssembly().Location;
				var bakPath = exePath + ".bak";
				if (File.Exists(bakPath))
				{
					File.Delete(bakPath);
				}
				File.Move(exePath, bakPath);
				File.Copy(bakPath, exePath, true);

				var installerPath = Path.Combine(Util.GetDownloadsPath(), "gta5eyetrackingmodupdater_installer.exe");
				Process.Start(installerPath, "/VERYSILENT");
			}
			catch
			{
				return false;
			}
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
	}
}
