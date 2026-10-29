using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BlindGunfight
{
    /// <summary>
    /// 자동 테스트(AutoCapture)가 키보드/마우스 대신 넣는 입력.
    /// "Pressed" 값은 넣은 쪽이 한 프레임 뒤에 직접 꺼야 한다.
    /// </summary>
    public sealed class SimulatedInput
    {
        public Vector2 Move;
        public Vector2 MouseScreenPosition;
        public bool Run;
        public bool Crouch;
        public bool Fire;
        public bool FirePressed;
        public bool Aim;
        public bool ReloadPressed;
        public bool MeleePressed;
        public bool SpotPressed;
    }

    /// <summary>
    /// 키보드/마우스 입력을 한곳에서 읽는다.
    /// 새 Input System 과 예전 Input Manager 둘 다 지원한다.
    /// Simulated 가 있으면 실제 입력 대신 그 값을 쓴다.
    /// </summary>
    public static class GameInput
    {
        public static SimulatedInput Simulated { get; set; }

        public static Vector2 Move
        {
            get
            {
                if (Simulated != null) return Vector2.ClampMagnitude(Simulated.Move, 1f);
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                float x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
                float y = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
                return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
#else
                return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#endif
            }
        }

        public static Vector2 MouseScreenPosition
        {
            get
            {
                if (Simulated != null) return Simulated.MouseScreenPosition;
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        public static bool Run => Simulated != null ? Simulated.Run : RawRun;
        public static bool Crouch => Simulated != null ? Simulated.Crouch : RawCrouch;
        public static bool ReloadPressed => Simulated != null ? Simulated.ReloadPressed : RawReloadPressed;
        public static bool MeleePressed => Simulated != null ? Simulated.MeleePressed : RawMeleePressed;
        public static bool SpotPressed => Simulated != null ? Simulated.SpotPressed : RawSpotPressed;
        public static bool Slot1Pressed => Simulated == null && RawSlot1Pressed;
        public static bool Slot2Pressed => Simulated == null && RawSlot2Pressed;
        public static bool TabLeftPressed => Simulated == null && RawTabLeftPressed;
        public static bool TabRightPressed => Simulated == null && RawTabRightPressed;
        public static bool BackPressed => Simulated == null && RawBackPressed;
        public static bool Fire => Simulated != null ? Simulated.Fire : RawFire;
        public static bool FirePressed => Simulated != null ? Simulated.FirePressed : RawFirePressed;
        public static bool Aim => Simulated != null ? Simulated.Aim : RawAim;

#if ENABLE_INPUT_SYSTEM
        private static bool RawRun => Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        private static bool RawCrouch => Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed);
        private static bool RawReloadPressed => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        private static bool RawMeleePressed => Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
        private static bool RawSpotPressed => Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
        private static bool RawSlot1Pressed => Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame;
        private static bool RawSlot2Pressed => Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame;
        private static bool RawTabLeftPressed => Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
        private static bool RawTabRightPressed => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        private static bool RawBackPressed => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        private static bool RawFire => Mouse.current != null && Mouse.current.leftButton.isPressed;
        private static bool RawFirePressed => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        private static bool RawAim => Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
        private static bool RawRun => Input.GetKey(KeyCode.LeftShift);
        private static bool RawCrouch => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
        private static bool RawReloadPressed => Input.GetKeyDown(KeyCode.R);
        private static bool RawMeleePressed => Input.GetKeyDown(KeyCode.F);
        private static bool RawSpotPressed => Input.GetKeyDown(KeyCode.Q);
        private static bool RawSlot1Pressed => Input.GetKeyDown(KeyCode.Alpha1);
        private static bool RawSlot2Pressed => Input.GetKeyDown(KeyCode.Alpha2);
        private static bool RawTabLeftPressed => Input.GetKeyDown(KeyCode.Q);
        private static bool RawTabRightPressed => Input.GetKeyDown(KeyCode.E);
        private static bool RawBackPressed => Input.GetKeyDown(KeyCode.Escape);
        private static bool RawFire => Input.GetMouseButton(0);
        private static bool RawFirePressed => Input.GetMouseButtonDown(0);
        private static bool RawAim => Input.GetMouseButton(1);
#endif
    }
}
