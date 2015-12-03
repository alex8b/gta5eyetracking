using System;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;

namespace Gta5EyeTrackingModUpdater
{
	public class ApplicationUninstallRegistryKey
	{
		private const string WindowsUninstallRegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
		private const string InstallLocationValueName = @"InstallLocation";
		private const string DisplayNameValueName = @"DisplayName";

		private readonly string _uninstallRegistryKeyDisplayName;

		public ApplicationUninstallRegistryKey(string uninstallRegistryKeyDisplayName)
		{
			_uninstallRegistryKeyDisplayName = uninstallRegistryKeyDisplayName;

			using (var uninstallKey = UninstallRegistryKey)
			{
				if (uninstallKey != null)
				{
					IsPresent = true;
					InstallLocation = uninstallKey.GetValueNames().Contains(InstallLocationValueName) ? Convert.ToString(uninstallKey.GetValue(InstallLocationValueName), CultureInfo.InvariantCulture) : null;
				}
			}
		}

		public bool IsPresent { get; private set; }

		public string InstallLocation { get; private set; }

		private RegistryKey UninstallRegistryKey
		{
			get
			{
				var windowsUninstallRegistryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(WindowsUninstallRegistryKeyPath);

				if (windowsUninstallRegistryKey != null)
				{
					foreach (var subKeyName in windowsUninstallRegistryKey.GetSubKeyNames())
					{
						using (var uninstallKey = windowsUninstallRegistryKey.OpenSubKey(subKeyName))
						{
							if (uninstallKey != null)
							{
								var displayName = Convert.ToString(uninstallKey.GetValue(DisplayNameValueName), CultureInfo.InvariantCulture);
								if (displayName.Equals(_uninstallRegistryKeyDisplayName, StringComparison.OrdinalIgnoreCase))
								{
									return windowsUninstallRegistryKey.OpenSubKey(subKeyName);
								}
							}
						}
					}
				}

				return null;
			}
		}
	}
}