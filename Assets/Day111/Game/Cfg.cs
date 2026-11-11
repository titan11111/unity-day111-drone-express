using UnityEngine;

namespace Day111
{
    // HTML版のルール定数（ゲームルールの基準値）。見た目は3D用に再設計。
    public static class Cfg
    {
        public const float PX = 0.02f;            // 1px = 0.02 unit
        public const float LW = 960f, H = 540f, GROUND = 470f;
        public const float SCROLL = 135f;
        public const float GOAL_CAM = 13240f;
        public const float PAD_X = 13840f;
        public const float CUST_X = 13960f;
        public const float HUR_WARN = 10900f, HUR_START = 11300f, HUR_END = 12600f;
        public const float ROPE = 58f, GRAV = 1500f;
        public const float OPEN_END = 5.6f;
        public const string BUILD_ID = "d111-0001";

        public static Vector3 P(float x, float y, float z = 0f) { return new Vector3(x * PX, (GROUND - y) * PX, z); }
        public static float Clamp(float v, float a, float b) { return v < a ? a : v > b ? b : v; }
        public static float Ease(float x) { x = Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
    }
}
