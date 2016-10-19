using System;

namespace Gta5EyeTracking
{
	public class Settings
	{
		public bool ExtendedViewEnabled { get; set; }
		public float ExtendedViewSensitivity { get; set; }

		public bool AimAtGazeEnabled { get; set; }
		public bool FireAtGazeEnabled { get; set; }
		public bool SnapAtTargetsEnabled { get; set; }
		public float GazeFiltering { get; set; }
		public bool IncinerateAtGazeEnabled { get; set; }
		public bool TaseAtGazeEnabled { get; set; }
		public bool MissilesAtGazeEnabled { get; set; }
        public bool AlwaysShowCrosshairEnabled { get; set; }
        public bool DontFallFromBikesEnabled { get; set; }
		public bool SendUsageStatistics { get; set; }
		public bool UserAgreementAccepted { get; set; }
		public string UserGuid { get; set; }

		public Settings()
		{
			ExtendedViewEnabled = true;
			ExtendedViewSensitivity = 0.5f;

			GazeFiltering = 0.5f;

            AimAtGazeEnabled = true;

            FireAtGazeEnabled = true;
			SnapAtTargetsEnabled = false;

            IncinerateAtGazeEnabled = false;
			TaseAtGazeEnabled = false;
			MissilesAtGazeEnabled = true;

		    AlwaysShowCrosshairEnabled = false;

            DontFallFromBikesEnabled = true;

			SendUsageStatistics = false;
			UserAgreementAccepted = false;

			UserGuid = Guid.NewGuid().ToString();
        }
	}
}
