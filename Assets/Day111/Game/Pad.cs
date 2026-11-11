using UnityEngine;
#if D111_INPUTSYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Day111
{
    // ゲームパッド入力（Joy-Con / Proコン / MFi / Xbox / PS）。接続済みの全デバイスを合算して使う。
    // iOS(GameController) / macOS / WebGL(ブラウザのGamepad API) 対応。
    public static class Pad
    {
        const float DEAD = .2f;

        static Vector2 Dead(Vector2 v) { return v.magnitude < DEAD ? Vector2.zero : (v.magnitude > 1f ? v.normalized : v); }

#if D111_INPUTSYSTEM
        public static bool Connected { get { return Gamepad.all.Count > 0 || Joystick.all.Count > 0; } }

        // 画面座標系（右+, 下+）。スティック or 十字キー
        public static Vector2 Move()
        {
            Vector2 sum = Vector2.zero;
            var all = Gamepad.all;
            for (int i = 0; i < all.Count; i++)
            {
                var g = all[i]; Vector2 v = Dead(g.leftStick.ReadValue()); var dp = g.dpad.ReadValue();
                if (dp.sqrMagnitude > .1f) v = dp;
                sum += v;
            }
            var js = Joystick.all;
            for (int i = 0; i < js.Count; i++) sum += Dead(js[i].stick.ReadValue());
            if (sum.magnitude > 1f) sum.Normalize();
            return new Vector2(sum.x, -sum.y);
        }

        public static bool Fire()
        {
            var all = Gamepad.all;
            for (int i = 0; i < all.Count; i++)
            {
                var g = all[i];
                if (g.buttonSouth.isPressed || g.buttonEast.isPressed || g.buttonWest.isPressed || g.buttonNorth.isPressed ||
                    g.rightShoulder.isPressed || g.leftShoulder.isPressed || g.rightTrigger.isPressed || g.leftTrigger.isPressed) return true;
            }
            var js = Joystick.all;
            for (int i = 0; i < js.Count; i++) if (AnyButton(js[i], false)) return true;
            return false;
        }

        // スタート・リトライ用：いずれかのボタンを押した瞬間
        public static bool AnyDown()
        {
            var all = Gamepad.all;
            for (int i = 0; i < all.Count; i++)
            {
                var g = all[i];
                if (g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame ||
                    g.startButton.wasPressedThisFrame || g.rightShoulder.wasPressedThisFrame || g.leftShoulder.wasPressedThisFrame ||
                    g.rightTrigger.wasPressedThisFrame || g.leftTrigger.wasPressedThisFrame) return true;
            }
            var js = Joystick.all;
            for (int i = 0; i < js.Count; i++) if (AnyButton(js[i], true)) return true;
            return false;
        }

        // ポーズ：＋（Start）/ −（Select）
        public static bool PauseDown()
        {
            var all = Gamepad.all;
            for (int i = 0; i < all.Count; i++) if (all[i].startButton.wasPressedThisFrame || all[i].selectButton.wasPressedThisFrame) return true;
            return false;
        }

        static bool AnyButton(InputDevice d, bool down)
        {
            var ch = d.children;
            for (int i = 0; i < ch.Count; i++)
            {
                var b = ch[i] as ButtonControl;
                if (b != null && (down ? b.wasPressedThisFrame : b.isPressed)) return true;
            }
            return false;
        }
#else
        // Input System未導入時の代替（旧Input Manager）
        public static bool Connected { get { var n = Input.GetJoystickNames(); return n != null && n.Length > 0; } }
        public static Vector2 Move()
        {
            float x = 0, y = 0; try { x = Input.GetAxisRaw("Horizontal"); y = -Input.GetAxisRaw("Vertical"); } catch { }
            return Dead(new Vector2(x, y));
        }
        public static bool Fire() { for (int i = 0; i < 20; i++) if (Input.GetKey(KeyCode.JoystickButton0 + i)) return true; return false; }
        public static bool AnyDown() { for (int i = 0; i < 20; i++) if (Input.GetKeyDown(KeyCode.JoystickButton0 + i)) return true; return false; }
        public static bool PauseDown() { return Input.GetKeyDown(KeyCode.JoystickButton7) || Input.GetKeyDown(KeyCode.JoystickButton6); }
#endif
    }
}
