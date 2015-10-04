using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;

namespace Gta5EyeTracking
{
	public class Tutorial
	{
		//private UIContainer _uiContainer;

		public Tutorial()
		{
			//_uiContainer = new UIContainer(new Point(0, 0), new Size(1280, 720), Color.FromArgb(0,0,0,0));

			//Welcome to the tutorial

			//First you need to calibrate the eye tracker.

			//=== Freelook ===

			//# Teleport to the desert
			//Look to the left side of the screen. The camera will turn towards the direction you are looking at.
			//# Wait until he looks for 2 seconds
			//Look at the right side of the screen.
			//# Wait until he looks for 2 seconds
			//Try to walk around without using the right joystick
			//# Check the path waypoints

			//=== Aiming ===
			//Time for shooting!
			//# Give weapons
			//Try to hit those cars with homing missiles. Press B to shoot a missile where you look
			//# Wait until targets are destroyed
			//Press LB to select your favorite weapon. Now you can do it with your eyes!
			//# Wait for weapon
			//# Spawn pedestrians
			//Kill them all!
			//# Wait until targets are destroyed
			//# Spawn pedestrians
			//Now we enabled "Snap At Pedestrians". The crosshair will snap at targets. Kill them all once again!
			//# Wait until targets are destroyed
			//# Spawn pedestrians
			//Press A to burn the targets.
			//# Wait until targets are destroyed
			//# Spawn pedestrians
			//Press RB to tase the targets.
			//# Wait until targets are destroyed

			//=== Driving ===
			//# Spawn a motorbike
			//# Put player on the motorbike
			//# Teleport to the highway
			//Destroy all the cars on your way. Use homing missiles (B) and incinerate (A)
			//# Wait until targets are destroyed

			//=== Helicopter ===
			//# Teleport to the airport
			//# Spawn a helicopter
			//# Put player in the helicopter
			//Use RT to take off, LT to go down, LB and RB to turn around
			//Fly to the city and destroy some cars with homing missiles!
			//# Wait until targets are destroyed
			//Continue playing?
			//# Wait 3 min
			//# Ask again. Restart tutorial.
		}
	}
}
