namespace Gta5EyeTracking.HidEmulation
{
	public interface IHidEmulation
	{
		double DeltaX { set; }
		double DeltaY { set; }
		bool Enabled { get; set; }
	}
}