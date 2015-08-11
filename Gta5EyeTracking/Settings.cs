namespace Gta5EyeTracking
{
	public enum FeeelookDevice : int
	{
		Gamepad = 0,
		Mouse = 1
	}

	public class Settings
	{
		public FeeelookDevice FreelookDevice { get; set; }
		public bool ThirdPersonFreelookEnabled { get; set; }
		public double ThirdPersonSensitivity { get; set; }

		public double ThirdPersonYOffset { get; set; }
		public double ThirdPersonDeadZoneHeight { get; set; }
		public double ThirdPersonDeadZoneWidth { get; set; }
		public double ThirdPersonMinPitchDeg { get; set; }
		public double ThirdPersonMaxPitchDeg { get; set; }

		public double ThirdPersonYOffsetDriving { get; set; }

		public double ThirdPersonDeadZoneHeightDriving { get; set; }
		public double ThirdPersonDeadZoneWidthDriving { get; set; }
		public double ThirdPersonMinPitchDrivingDeg { get; set; }
		public double ThirdPersonMaxPitchDrivingDeg { get; set; }

		public double ThirdPersonYOffsetPlane { get; set; }
		public double ThirdPersonDeadZoneHeightPlane { get; set; }
		public double ThirdPersonDeadZoneWidthPlane { get; set; }
		public double ThirdPersonMinPitchPlaneDeg { get; set; }
		public double ThirdPersonMaxPitchPlaneDeg { get; set; }

		public double ThirdPersonYOffsetHeli { get; set; }
		public double ThirdPersonDeadZoneHeightHeli { get; set; }
		public double ThirdPersonDeadZoneWidthHeli { get; set; }
		public double ThirdPersonMinPitchHeliDeg { get; set; }
		public double ThirdPersonMaxPitchHeliDeg { get; set; }

		public double AimingSensitivity { get; set; }
		public bool FirstPersonFreelookEnabled { get; set; }
		public double FirstPersonSensitivity { get; set; }

		public double FirstPersonDeadZoneHeight { get; set; }
		public double FirstPersonDeadZoneWidth { get; set; }
		public double FirstPersonMinPitchDeg { get; set; }
		public double FirstPersonMaxPitchDeg { get; set; }
		
		public bool AimWithGazeEnabled { get; set; }
		public bool SnapAtPedestriansEnabled { get; set; }
		public double GazeFiltering { get; set; }
		public bool IncinerateAtGazeEnabled { get; set; }
		public bool TaseAtGazeEnabled { get; set; }
		public bool MissilesAtGazeEnabled { get; set; }
		public bool PedestrianInteractionEnabled { get; set; }
		public bool DontFallFromBikesEnabled { get; set; }

		public Settings()
		{
			FreelookDevice = FeeelookDevice.Gamepad;

			ThirdPersonFreelookEnabled = true;
			ThirdPersonSensitivity = 0.3;

			ThirdPersonYOffset = 0.3; 
			ThirdPersonDeadZoneWidth = 0.1;
			ThirdPersonDeadZoneHeight = 0.1;
			ThirdPersonMinPitchDeg = -20;
			ThirdPersonMaxPitchDeg = 33;

			ThirdPersonYOffsetDriving = 0.3;
			ThirdPersonDeadZoneWidthDriving = 0.1;
			ThirdPersonDeadZoneHeightDriving = 0.1;
			ThirdPersonMinPitchDrivingDeg = -20;
			ThirdPersonMaxPitchDrivingDeg = 0;

			ThirdPersonYOffsetPlane = 0.0;
			ThirdPersonDeadZoneWidthPlane = 0.3;
			ThirdPersonDeadZoneHeightPlane = 0.1;
			ThirdPersonMinPitchPlaneDeg = -60;
			ThirdPersonMaxPitchPlaneDeg = 0;

			ThirdPersonYOffsetHeli = 0.0;
			ThirdPersonDeadZoneWidthHeli = 0.3;
			ThirdPersonDeadZoneHeightHeli = 0.1;
			ThirdPersonMinPitchHeliDeg = -60;
			ThirdPersonMaxPitchHeliDeg = 0;

			FirstPersonFreelookEnabled = true;
			FirstPersonSensitivity = 0.5;

			FirstPersonDeadZoneWidth = 0.2;
			FirstPersonDeadZoneHeight = 0.3;
			FirstPersonMinPitchDeg = -45;
			FirstPersonMaxPitchDeg = 33;
			
			AimingSensitivity = 0.4;
			GazeFiltering = 0.5;
			AimWithGazeEnabled = true;
			SnapAtPedestriansEnabled = false;
			IncinerateAtGazeEnabled = true;
			TaseAtGazeEnabled = true;
			MissilesAtGazeEnabled = true;
			PedestrianInteractionEnabled = true;
			DontFallFromBikesEnabled = true;
		}
	}
}