﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Xml;

namespace Gta5EyeTrackingModUpdater
{
	public class Updater
	{
		public event EventHandler<EventArgs> ScriptHookVInstalled = delegate { };
		public event EventHandler<EventArgs> ScriptHookVRemoved = delegate { };
		public event EventHandler<EventArgs> ModInstalled = delegate { };

		private readonly UpdaterNotifyIcon _updaterNotifyIcon;
		private readonly Settings _settings;
		private readonly object _lock = new object();
		private bool _enabled = true;
		

		public Updater(UpdaterNotifyIcon updaterNotifyIcon, Settings settings)
		{
			_updaterNotifyIcon = updaterNotifyIcon;
			_settings = settings;
		}

		public void CheckForUpdates()
		{
			if (!_enabled) return;
			if (!Monitor.TryEnter(_lock)) return;
			try
			{
				Util.Log("Checking for updates");
				SelfUpdate();
				UpdateModBundle();
				UpdateScriptHookV();
			}
			catch
			{
				Util.Log("Failed to update");
			}
			Monitor.Exit(_lock);
		}

		public Version GetGtaVersion()
		{
			var installedGtaVersion = new Version(0, 0);
			var gtaExeFilePath = Path.Combine(_settings.GtaPath, "GTA5.exe");
			Util.TryGetFileVersion(gtaExeFilePath, ref installedGtaVersion);
			return installedGtaVersion;
		}

		public void UpdateScriptHookV()
		{
			var installedGtaVersion = GetGtaVersion();
			
			if (installedGtaVersion == new Version(0,0))
			{
				Util.Log("Failed to get GTA V version");
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
				Util.Log("Script Hook V is temporarly disabled");
				return;
			}

			var installedScriptHookVVersion = GetInstalledScriptHookVVersion();

			if (IsVersionLower(installedScriptHookVVersion,availableScriptHookVVersion))
			{
				DownloadScriptHookV(scriptHookVDownloadUrlAddress);
				if (!InstallScriptHookV())
				{
					Util.Log("Failed to update Script Hook V");
				}
			}
			else if (!IsScriptHookVInstalled())
			{
				if (!InstallScriptHookV())
				{
					Util.Log("Failed to update Script Hook V");
				}
			}
		}

		public string GetInstalledScriptHookVVersion()
		{
			var scriptHookVDllPath = Path.Combine(_settings.GtaPath, "ScriptHookV.dll");
			return GetScriptHookVVersion(scriptHookVDllPath);
		}

		private string GetScriptHookVVersion(string scriptHookVDllPath)
		{
			var installedScriptHookVVersion = "";
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
			return installedScriptHookVVersion;
		}


		private bool IsScriptHookVInstalled()
		{
			var scriptHookVDllPath = Path.Combine(_settings.GtaPath, "ScriptHookV.dll");
			var dinput8DllPath = Path.Combine(_settings.GtaPath, "dinput8.dll");

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
			var versionPattern = @"<tr>\s*<th>Version</th>\s*<td>\s*v?([^\s]*)\s*</td>\s*</tr>";
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
				var scriptHookVDllPathDest = Path.Combine(_settings.GtaPath, "ScriptHookV.dll");
				var dinput8DllPath = Path.Combine(extractPath, "bin", "dinput8.dll");
				var dinput8DllPathDest = Path.Combine(_settings.GtaPath, "dinput8.dll");
				var nativeTrainerAsiPath = Path.Combine(extractPath, "bin", "NativeTrainer.asi");
				var nativeTrainerAsiPathDest = Path.Combine(_settings.GtaPath, "NativeTrainer.asi");


				if (File.Exists(scriptHookVDllPathDest))
				{
					if (File.Exists(scriptHookVDllPathDest + ".bak"))
					{
						File.Delete(scriptHookVDllPathDest + ".bak");
					}
					File.Move(scriptHookVDllPathDest, scriptHookVDllPathDest + ".bak");
				}
				if (File.Exists(dinput8DllPathDest))
				{
					if (File.Exists(dinput8DllPathDest + ".bak"))
					{
						File.Delete(dinput8DllPathDest + ".bak");
					}
					File.Move(dinput8DllPathDest, dinput8DllPathDest + ".bak");
				}
				if (File.Exists(nativeTrainerAsiPathDest))
				{
					if (File.Exists(nativeTrainerAsiPathDest + ".bak"))
					{
						File.Delete(nativeTrainerAsiPathDest + ".bak");
					}
					File.Move(nativeTrainerAsiPathDest, nativeTrainerAsiPathDest + ".bak");
				}

				File.Copy(scriptHookVDllPath, Path.Combine(_settings.GtaPath, Path.GetFileName(scriptHookVDllPath)), true);
				File.Copy(dinput8DllPath, Path.Combine(_settings.GtaPath, Path.GetFileName(dinput8DllPath)), true);
				File.Copy(nativeTrainerAsiPath, Path.Combine(_settings.GtaPath, Path.GetFileName(nativeTrainerAsiPath)), true);
			}
			catch(Exception e)
			{
				return false;
				//Failed to install
			}

			ScriptHookVInstalled(this, new EventArgs());
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
			var dinput8DllPath = Path.Combine(_settings.GtaPath, "dinput8.dll");
			var scriptHookVDllPath = Path.Combine(_settings.GtaPath, "ScriptHookV.dll");
			if (File.Exists(scriptHookVDllPath))
			{
				try
				{
					if (File.Exists(scriptHookVDllPath + ".bak"))
					{
						File.Delete(scriptHookVDllPath + ".bak");
					}
					File.Move(scriptHookVDllPath, scriptHookVDllPath + ".bak");
					File.Delete(scriptHookVDllPath);

					if (File.Exists(dinput8DllPath + ".bak"))
					{
						File.Delete(dinput8DllPath + ".bak");
					}
					File.Move(dinput8DllPath, dinput8DllPath + ".bak");
					File.Delete(dinput8DllPath);
				}
				catch
				{
					Util.Log("Failed to remove Script Hook V");
				}
			}
			ScriptHookVRemoved(this, new EventArgs());
		}
		public Version GetModVersion()
		{
			var installedModVersion = new Version(0, 0);
			var modDllFilePath = Path.Combine(_settings.GtaPath, "scripts", "Gta5EyeTracking.dll");
			Util.TryGetFileVersion(modDllFilePath, ref installedModVersion);
			return installedModVersion;
		}

		private void UpdateModBundle()
		{
			var installedModVersion = GetModVersion();

			Version availableModVersion;
			string modBundleDownloadUrl;
			bool blockScriptHookV;
			Version availableUpdaterVersion;
			string updaterDownloadUrl;
			if (!TryParseModInfoWebPage(out availableModVersion, out modBundleDownloadUrl, out blockScriptHookV, out availableUpdaterVersion, out updaterDownloadUrl))
			{
				//Failed to read or parse web page
				return;
			}

			if (installedModVersion >= availableModVersion)
			{
				//Mod is up to date
				return;
			}

			DownloadModBundle(modBundleDownloadUrl);

			if (!InstallModBundle())
			{
				Util.Log("Failed to update Gta V Eye Tracking Mod");
			}
		}

		private bool IsVersionLower(string installedScriptHookVVersion, string scriptHookVVersion)
		{
			return true;
			//todo: parse version + abcde
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
				var scriptHookFiles = new List<string>
				{
					"ScriptHookV.dll",
					"NativeTrainer.asi",
					"dinput8.dll"
				};

				var scriptHookVVersion = GetScriptHookVVersion(Path.Combine(extractPath, "ScriptHookV.dll"));

				var scriptHookVDllPath = Path.Combine(_settings.GtaPath, "bin", "ScriptHookV.dll");
				var dinput8DllPath = Path.Combine(_settings.GtaPath, "bin", "dinput8.dll");
				var nativeTrainerAsiPath = Path.Combine(_settings.GtaPath, "bin", "NativeTrainer.asi");

				var skipScriptHook = File.Exists(scriptHookVDllPath)
					&& File.Exists(dinput8DllPath)
					&& File.Exists(nativeTrainerAsiPath)
					&& IsVersionLower(scriptHookVVersion, GetInstalledScriptHookVVersion());

				Util.DirectoryCopy(Path.Combine(extractPath), _settings.GtaPath, true, true, true,
					skipScriptHook ? scriptHookFiles : new List<string>());
			}
			catch
			{
				return false;
				//Failed to install
			}

			ModInstalled(this, new EventArgs());
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
				Util.Log("Failed to auto-update");
			}

		}

		private void DownloadModUpdater(string downloadUrlAddress)
		{
			var wc = new WebClient();
			var localFilePath = Path.Combine(Util.GetDownloadsPath(), "gta5eyetrackingmodupdater_bundle.exe");
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

				var installerPath = Path.Combine(Util.GetDownloadsPath(), "gta5eyetrackingmodupdater_bundle.exe");
				Process.Start(installerPath, "/quiet /install");
			}
			catch
			{
				return false;
			}
			return true;
		}

		private void ShowNotification(string text)
		{
			_updaterNotifyIcon.ShowNotification(text);
		}

		public void Close()
		{
			_enabled = false;
			Monitor.Enter(_lock);
			Monitor.Exit(_lock);
		}
	}
}
