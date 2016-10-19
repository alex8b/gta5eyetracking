using Gta5EyeTracking.HidEmulation;
using GTA;
using NativeUI;
using SharpDX.XInput;

namespace Gta5EyeTracking
{
    public class GameState
    {
        public bool IsPaused { get; private set; }
        public bool IsInVehicle { get; private set; }
        public bool IsInAircraft { get; private set; }
        public bool IsInRadialMenu { get; private set; }
        public bool IsMenuOpen { get; private set; }

        public bool IsMeleeWeapon { get; private set; }
        public bool IsThrowableWeapon { get; private set; }
        public bool IsSniperWeaponAndZoomed { get; private set; }

        public bool IsInCharacterSelectionMenu { get; private set; }
        public bool IsAimingWithMouse { get; private set; }
        public bool IsShootingWithMouse { get; private set; }
        public bool IsAimingWithGamepad { get; private set; }
        public bool IsShootingWithGamepad { get; private set; }

        private readonly ControllerEmulation _controllerEmulation;
        private readonly MenuPool _menuPool;

        public GameState(ControllerEmulation controllerEmulation, MenuPool menuPool)
        {
            _controllerEmulation = controllerEmulation;
            _menuPool = menuPool;
        }

        public void Update()
        {
            IsPaused = Game.IsPaused;
            IsInVehicle = Game.Player.Character.IsInVehicle();
            IsInAircraft = Game.Player.Character.IsInFlyingVehicle;

            IsMenuOpen = _menuPool.IsAnyMenuOpen();

            var controllerState = _controllerEmulation.ControllerState;

            IsInRadialMenu = !IsInVehicle && controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder);
            IsShootingWithMouse = !IsInVehicle && User32.IsKeyPressed(VirtualKeyStates.VK_LBUTTON);
            IsShootingWithGamepad = !IsInVehicle && !IsInRadialMenu && (controllerState.Gamepad.RightTrigger > 0);

            IsAimingWithMouse = !IsInVehicle && User32.IsKeyPressed(VirtualKeyStates.VK_RBUTTON);
            IsAimingWithGamepad = !IsInVehicle && !IsInRadialMenu && (controllerState.Gamepad.LeftTrigger > 50);

            IsMeleeWeapon = ScriptHookExtensions.IsMelee(Game.Player.Character.Weapons.Current.Hash);
            IsThrowableWeapon = ScriptHookExtensions.IsThrowable(Game.Player.Character.Weapons.Current.Hash);
            IsSniperWeaponAndZoomed = ScriptHookExtensions.IsSniper(Game.Player.Character.Weapons.Current.Hash) && (GameplayCamera.IsFirstPersonAimCamActive);

            IsInCharacterSelectionMenu = !IsMenuOpen
                                             && (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadDown)
                                                 || User32.IsKeyPressed(VirtualKeyStates.VK_LMENU));
        }
    }
}
