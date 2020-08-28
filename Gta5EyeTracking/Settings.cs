using System;

namespace Gta5EyeTracking
{
    public class Settings
    {
        public float Responsiveness { get; set; }

        public bool ExtendedViewEnabled { get; set; }
        public float ExtendedViewSensitivity { get; set; }

        public bool AimAtGazeEnabled { get; set; }
        public bool FireAtGazeEnabled { get; set; }
        public bool SnapAtTargetsEnabled { get; set; }
        public bool IncinerateAtGazeEnabled { get; set; }
        public bool TaseAtGazeEnabled { get; set; }
        public bool MissilesAtGazeEnabled { get; set; }
        public bool AlwaysShowCrosshairEnabled { get; set; }
        public bool DontFallFromBikesEnabled { get; set; }
        public bool SendUsageStatistics { get; set; }
        public bool UserAgreementAccepted { get; set; }
        public string UserGuid { get; set; }
        public bool FirstPersonModeEnabled { get; set; }

        public Settings()
        {
            Responsiveness = 0.5f;

            ExtendedViewEnabled = true;
            ExtendedViewSensitivity = 0.5f;

            AimAtGazeEnabled = true;

            FireAtGazeEnabled = false;
            SnapAtTargetsEnabled = false;

            IncinerateAtGazeEnabled = false;
            TaseAtGazeEnabled = false;
            MissilesAtGazeEnabled = true;

            AlwaysShowCrosshairEnabled = false;

            DontFallFromBikesEnabled = true;

            SendUsageStatistics = false;
            UserAgreementAccepted = false;

            UserGuid = Guid.NewGuid().ToString();

            FirstPersonModeEnabled = false;
        }
    }
}
