using System.Collections.Generic;
using UnityEngine;

namespace Day111
{
    // 障害物の3D見た目（プリミティブ構成）。原点は論理位置のアンカー、子はpx単位(y上向き)
    public static class ObFactory
    {
        const float G = Cfg.GROUND;
        const float PX = Cfg.PX;
        const float FRONT = 24f;                 // 固体の前面を、機体の面より少し奥へ
        static Texture2D winTex;
        static readonly Dictionary<string, Material> bmats = new Dictionary<string, Material>();

        static Material M(string n) { return Mats.Get(n); }

        static Material BuildingMat(float w, float h, bool fac)
        {
            string k = (int)w + "x" + (int)h + (fac ? "f" : "");
            Material m;
            if (bmats.TryGetValue(k, out m) && m) return m;
            if (winTex == null) winTex = MakeWindowTex();
            m = new Material(M(fac ? "Factory" : "Bldg"));
            m.mainTexture = winTex;
            m.mainTextureScale = new Vector2(Mathf.Max(1, Mathf.Round(w / 50f)), Mathf.Max(1, Mathf.Round(h / 50f)));
            bmats[k] = m;
            return m;
        }

        static Texture2D MakeWindowTex()
        {
            const int N = 64;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, true);
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    bool win = x > 12 && x < 52 && y > 14 && y < 50;
                    bool lit = ((x * 7 + y * 3) % 5) == 0;
                    Color32 c = new Color32(205, 198, 188, 255);
                    if (win)
                    {
                        bool onLit = (x < 32 && y > 32);
                        c = onLit ? new Color32(255, 214, 140, 255) : new Color32(70, 90, 120, 255);
                    }
                    px[y * N + x] = c;
                }
            t.SetPixels32(px); t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Bilinear; t.Apply(true);
            return t;
        }

        public static GameObject Build(Ob o)
        {
            var root = new GameObject(o.type + "_" + o.i);
            var r = root.transform;
            switch (o.type)
            {
                case "house": House(o, r); break;
                case "building": Building(o, r); break;
                case "wire": Wire(o, r); break;
                case "tree": Tree(o, r); break;
                case "sign": Sign(o, r); break;
                case "car": Car(o, r); break;
                case "viaduct": Viaduct(o, r); break;
                case "balloon": Balloon(o, r); break;
                case "trash": Trash(o, r); break;
                case "crow": Bird(o, r, 1f, "Crow"); break;
                case "pigeon": Bird(o, r, .7f, "Pigeon"); break;
                case "monkey": Monkey(o, r); break;
                case "cell": Cell(o, r); break;
                case "odrone": MiniDrone(o, r); break;
                case "crane": Crane(o, r); break;
                case "vent": Vent(o, r); break;
                case "steam": Steam(o, r); break;
                case "sprinkler": Sprinkler(o, r); break;
                case "debris": Debris(o, r); break;
            }
            return root;
        }

        static void House(Ob o, Transform r)
        {
            float bh = o.h * .62f, rh = o.h - bh; int c = o.col % 3; float d = 110, z = FRONT + d / 2;
            Prims.Box(r, M("Wall" + c), o.w / 2, bh / 2, o.w, bh, d, z);
            Prims.Roof(r, M("Roof" + c), o.w / 2, bh, o.w * 1.1f, rh, d + 20, z);
            Prims.Box(r, M("Door"), o.w * .5f, 20, 22, 40, 3, FRONT - 1.5f, false);
            Prims.Box(r, M("Glass"), o.w * .2f, bh * .6f, 24, 24, 3, FRONT - 1.5f, false);
            Prims.Box(r, M("Glass"), o.w * .8f, bh * .6f, 24, 24, 3, FRONT - 1.5f, false);
            if (o.ch == 1 || o.i < 0 || o.w > 260)
            {
                float cxh = o.w * .5f + o.w * .17f, roofY = bh + rh * (1f - (.17f * o.w) / (o.w * .55f));
                Detail.Chimney(r, cxh, roofY - 10f, o.h + 30f, z, o.i >= 0);
            }
            Prims.Box(r, M("Concrete"), o.w / 2, bh + 2, o.w + 6, 5, d + 8, z, false);   // 軒のライン
        }

        static void Building(Ob o, Transform r)
        {
            if (!o.fac) { Detail.Building(o, r, FRONT, o.i >= 0); return; }
            float d = 120, z = FRONT + d / 2;
            Prims.Box(r, BuildingMat(o.w, o.h, o.fac), o.w / 2, o.h / 2, o.w, o.h, d, z);
            Prims.Box(r, M("MetalDark"), o.w / 2, o.h + 3, o.w + 8, 7, d + 8, z);
            if (o.ex != null)
                foreach (var e in o.ex)
                {
                    Prims.Box(r, M("Metal"), e[0] + e[1] / 2, o.h + 7 + e[2] / 2, e[1], e[2], 50, FRONT + 50);
                    Prims.Cyl(r, M("MetalDark"), e[0] + e[1] / 2, o.h + 7 + e[2] + 2, e[1] * .6f, 4, FRONT + 50);
                }
            if (o.fac) { Prims.Cyl(r, M("Brick"), o.w * .85f, o.h + 40, 26, 80, FRONT + 90); }
        }

        static void Wire(Ob o, Transform r)
        {
            float top = 0;
            foreach (var y in o.ys) { float up = G - y; top = Mathf.Max(top, up); Prims.Rod(r, M("Wire"), new Vector2(0, up), new Vector2(o.w, up), 4f, 0f); }
            foreach (var px in new[] { 0f, o.w })
            {
                Prims.Cyl(r, M("Pole"), px, Mathf.Max(top + 36, 60) / 2f, 10, Mathf.Max(top + 36, 60), 4f);
                Prims.Box(r, M("Pole"), px, top + 24, 56, 6, 10, 4f);
            }
        }

        static void Tree(Ob o, Transform r)
        {
            Prims.Cyl(r, M("Trunk"), 0, 125, 22, 250, FRONT + 10);
            Prims.Ball(r, M("Leaf"), 0, G - 245, 160, 110, 120, FRONT + 20);
            Prims.Ball(r, M("Leaf2"), -55, G - 240, 100, 80, 100, FRONT + 10);
            Prims.Ball(r, M("Leaf2"), 55, G - 250, 95, 80, 100, FRONT + 30);
            Prims.Box(r, M("Trunk"), -127.5f, G - 264, 125, 12, 14, FRONT + 6);
            Prims.Ball(r, M("Leaf"), -180, G - 262, 58, 46, 50, FRONT + 6);
        }

        static void Sign(Ob o, Transform r)
        {
            Prims.Box(r, M("Sign"), (o.w + 8) / 2, G - o.y - o.h / 2, o.w - 8, o.h, 14, FRONT + 20);
            Prims.Box(r, M("MetalDark"), 4, G - o.y - 6, 12, 12, 12, FRONT + 20);
            Prims.Box(r, M("MetalDark"), (o.w + 8) / 2, G - o.y + 3, o.w - 8, 5, 18, FRONT + 20);
        }

        static void Car(Ob o, Transform r)
        {
            Color bc = o.k == 1 ? new Color(.85f, .75f, .25f) : new Color(.75f, .2f, .17f);
            if (o.col != 0) bc = ColorFromInt(o.col);
            var bm = Mats.Tint("CarBody", bc);
            Prims.Box(r, bm, o.w / 2, o.h * .34f, o.w, o.h * .5f, 44, FRONT + 20);
            Prims.Box(r, M("Glass"), o.w * (o.k == 1 ? .35f : .5f), o.h * .78f, o.w * (o.k == 1 ? .55f : .5f), o.h * .42f, 38, FRONT + 20);
            var w1 = Prims.Wheel(r, M("Tire"), o.w * .22f, 9, 18, 46, FRONT + 20);
            var w2 = Prims.Wheel(r, M("Tire"), o.w * .78f, 9, 18, 46, FRONT + 20);
            Prims.Box(r, M("Led"), 2, o.h * .35f, 4, 8, 20, FRONT + 20, false);
            o.list = new[] { w1, w2 };
        }

        public static Color ColorFromInt(int i)
        {
            Color[] pal = { new Color(.72f, .2f, .16f), new Color(.18f, .43f, .69f), new Color(.85f, .7f, .23f), new Color(.25f, .54f, .35f), new Color(.33f, .35f, .4f), new Color(.48f, .25f, .54f) };
            return pal[Mathf.Abs(i) % pal.Length];
        }

        static void Viaduct(Ob o, Transform r)
        {
            Prims.Box(r, M("Concrete"), o.w / 2, G - 225 + 37.5f, o.w, 75, 110, FRONT + 55);
            Prims.Box(r, M("MetalDark"), o.w / 2, G - 150 + 3, o.w, 6, 118, FRONT + 55);
            for (float x = 60; x < o.w; x += 220) Prims.Box(r, M("Concrete"), x, (G - 225) / 2f, 42, G - 225, 70, FRONT + 55);
        }

        static void Balloon(Ob o, Transform r)
        {
            Prims.Ball(r, M("Balloon" + (o.c % 3)), 0, 0, 30, 38, 30, 0f, false);
            Prims.Box(r, M("Rope"), 0, -22, 6, 6, 6, 0f, false);
            Prims.Rod(r, M("Rope"), new Vector2(0, -22), new Vector2(0, -64), 1.5f, 0f);
        }

        static void Trash(Ob o, Transform r)
        {
            o.a = Prims.Box(r, M("Trash"), 0, 0, 24, 20, 20, 0f);
            Prims.Ball(r, M("Trash"), 4, 8, 16, 12, 14, 0f);
        }

        static void Bird(Ob o, Transform r, float s, string mat)
        {
            var body = new GameObject("body").transform; body.SetParent(r, false); body.localScale = Vector3.one * s;
            o.a = body;
            Prims.Ball(body, M(mat), 0, 0, 32, 18, 18, 0f);
            Prims.Ball(body, M(mat), 16, 7, 13, 13, 13, 0f);
            Prims.Box(body, M("Beak"), 25, 5, 10, 4, 4, 0f, false);
            Prims.Box(body, M(mat), -19, 1, 12, 5, 8, 0f);
            Prims.Ball(body, M("Led"), 19, 10, 3, 3, 3, -6f, false);
            var pv = new GameObject("wing").transform; pv.SetParent(body, false); pv.localPosition = new Vector3(-2 * PX, 6 * PX, 0);
            Prims.Box(pv, M(mat), -6, 9, 20, 18, 3, 0f);
            o.b = pv;
        }

        static void Monkey(Ob o, Transform r)
        {
            var b = new GameObject("body").transform; b.SetParent(r, false); o.a = b;
            Prims.Ball(b, M("Fur"), 0, 15, 22, 28, 18, 0f);
            Prims.Ball(b, M("Fur"), 0, 36, 20, 18, 18, 0f);
            Prims.Ball(b, M("Skin1"), 4, 35, 11, 9, 4, -8f, false);
            Prims.Rod(b, M("Fur"), new Vector2(-9, 8), new Vector2(-22, 22), 5f, 0f);
            Prims.Rod(b, M("Fur"), new Vector2(9, 10), new Vector2(18, 4), 5f, 0f);
            Prims.Ball(b, M("Led"), 6, 38, 3, 3, 3, -9f, false);
        }

        static void Cell(Ob o, Transform r)
        {
            var b = new GameObject("spin").transform; b.SetParent(r, false); o.a = b;
            Prims.Cyl(b, M("Cell"), 0, 0, 20, 26, 0f, false);
            Prims.Cyl(b, M("Metal"), 0, 16, 8, 6, 0f, false);
            Prims.Box(b, M("Led"), 0, 0, 12, 3, 22, 0f, false);
            Prims.Box(b, M("Led"), 0, 0, 3, 12, 22, 0f, false);
        }

        public static void MiniDroneParts(Transform r, out Transform[] rotors, bool red)
        {
            Prims.Box(r, M("DroneDark"), 0, 0, 42, 11, 24, 0f);
            Prims.Box(r, M("DroneBody"), 0, 6, 26, 6, 18, 0f);
            Prims.Ball(r, M(red ? "Led" : "Led2"), 0, -5, 6, 6, 6, -10f, false);
            rotors = new Transform[2];
            for (int n = 0; n < 2; n++)
            {
                float x = n == 0 ? -24 : 24;
                Prims.Rod(r, M("DroneDark"), new Vector2(x * .3f, 3), new Vector2(x, 9), 3f, 0f);
                rotors[n] = Prims.Cyl(r, M("Rotor"), x, 10, 26, 1.5f, 0f, false);
            }
        }

        static void MiniDrone(Ob o, Transform r)
        {
            Transform[] ro; MiniDroneParts(r, out ro, true); o.list = ro;
        }

        static void Crane(Ob o, Transform r)
        {
            Prims.Box(r, M("Crane"), 0, G - 79, 380, 14, 26, FRONT + 20);
            Prims.Box(r, M("Crane"), 150, (G - 72) / 2f, 22, G - 72, 26, FRONT + 20);
            Prims.Box(r, M("MetalDark"), 150, G - 99, 40, 26, 30, FRONT + 20);
            for (float x = -170; x < 170; x += 40) Prims.Box(r, M("MetalDark"), x, G - 92, 5, 14, 6, FRONT + 20, false);
            o.a = Prims.Rod(r, M("Rope"), Vector2.zero, Vector2.up, 3f, FRONT + 20);
            o.b = Prims.Box(r, M("Warn"), 0, 0, 52, 34, 28, FRONT + 20);
        }

        static void Vent(Ob o, Transform r)
        {
            Prims.Box(r, M("Metal"), 0, 8, 70, 14, 34, FRONT + 40);
            for (int n = -2; n <= 2; n++) Prims.Box(r, M("MetalDark"), n * 12, 12, 3, 8, 38, FRONT + 40, false);
            o.a = Prims.Cyl(r, Mats.Tint("FxPlume", new Color(.8f, .93f, 1f, .22f)), 0, 0, 72, 10, 0f, false);
        }

        static void Steam(Ob o, Transform r)
        {
            Prims.Box(r, M("MetalDark"), 0, 17, 48, 34, 40, FRONT + 30);
            Prims.Cyl(r, M("Metal"), 0, 38, 56, 8, FRONT + 30);
            o.a = Prims.Cyl(r, Mats.Tint("FxPlume", new Color(.95f, .95f, .95f, .45f)), 0, 0, 58, 10, 0f, false);
        }

        static void Sprinkler(Ob o, Transform r)
        {
            Prims.Box(r, M("MetalDark"), 0, 6, 26, 12, 22, FRONT + 20);
            Prims.Ball(r, M("Metal"), 0, 14, 18, 14, 18, FRONT + 20);
            o.list = new Transform[7];
            for (int n = 0; n < 7; n++) o.list[n] = Prims.Ball(r, Mats.Tint("FxPlume", new Color(.6f, .85f, 1f, .75f)), 0, 0, 20, 20, 20, 0f, false);
        }

        static void Debris(Ob o, Transform r)
        {
            o.a = Prims.Box(r, M("Wood"), 0, 0, 34, 20, 20, 0f);
            Prims.Box(r, M("MetalDark"), 0, 0, 36, 4, 22, 0f, false);
        }

        // ---- 毎フレームの見た目更新 ----
        public static void Sync(Ob o, float T, float camX)
        {
            if (o.go == null) return;
            var t = o.go.transform;
            switch (o.type)
            {
                case "house": case "building": case "wire": case "tree": case "sign": case "viaduct":
                    t.position = Cfg.P(o.x, G, 0); break;
                case "car":
                    t.position = Cfg.P(o.x, G, 0);
                    if (o.list != null) foreach (var w in o.list) w.Rotate(0, 0, 0); break;
                case "balloon": t.position = Cfg.P(o.px, o.py, 0); t.rotation = Quaternion.Euler(0, 0, Mathf.Sin(T * 1.7f + o.ph) * 6f); break;
                case "trash": t.position = Cfg.P(o.px, o.py, 0); t.rotation = Quaternion.Euler(0, 0, -o.rot * Mathf.Rad2Deg); break;
                case "crow": case "pigeon":
                    {
                        t.position = Cfg.P(o.px, o.py, 0);
                        float dirX = (o.mode == "chase") ? Mathf.Cos(o.hd) : (o.mode == "flee" || o.mode == "scatter" ? 1f : -1f);
                        if (o.type == "pigeon" && (o.mode == "perch" || o.mode == "warn" || o.mode == "fly")) dirX = -1f;
                        float flap = (o.mode == "perch") ? 0f : Mathf.Sin(T * 22f + o.ph) * 55f;
                        if (o.a) o.a.localScale = new Vector3((dirX < 0 ? -1f : 1f) * Mathf.Abs(o.a.localScale.y), Mathf.Abs(o.a.localScale.y), Mathf.Abs(o.a.localScale.y));
                        if (o.b) o.b.localRotation = Quaternion.Euler(0, 0, flap);
                        float pitch = (o.mode == "chase") ? -Mathf.Sin(o.hd) * 25f * (dirX < 0 ? 1 : -1) : 0f;
                        t.rotation = Quaternion.Euler(0, 0, pitch);
                        break;
                    }
                case "monkey":
                    t.position = Cfg.P(o.x, o.y, 0);
                    if (o.a) o.a.localRotation = Quaternion.Euler(0, 0, -o.rot * Mathf.Rad2Deg);
                    t.localScale = new Vector3(o.face >= 0 ? 1f : -1f, 1, 1);
                    break;
                case "cell":
                    t.position = Cfg.P(o.x, o.y + Mathf.Sin(T * 3f + o.ph) * 4f, 0);
                    if (o.a) o.a.localRotation = Quaternion.Euler(0, T * 140f, 0); break;
                case "odrone":
                    t.position = Cfg.P(o.px, o.py, 0);
                    if (o.list != null) foreach (var rr in o.list) rr.localRotation = Quaternion.Euler(0, T * 2000f, 0);
                    break;
                case "crane":
                    t.position = Cfg.P(o.x, G, 0);
                    if (o.a) Prims.SetRod(o.a, new Vector2(o.tr, G - 86), new Vector2(o.tr, G - (o.lb - 34)), 3f);
                    if (o.b) o.b.localPosition = new Vector3(o.tr * PX, (G - (o.lb - 17)) * PX, o.b.localPosition.z);
                    break;
                case "vent":
                    {
                        t.position = Cfg.P(o.x, o.y, 0);
                        bool burst = o.phs == "burst", warn = o.phs == "warn";
                        if (o.a)
                        {
                            float hgt = burst ? (o.y - 40) : (warn ? 40 + Mathf.Sin(T * 30) * 8 : 0);
                            o.a.gameObject.SetActive(hgt > 1);
                            o.a.localScale = new Vector3(72 * PX, hgt * PX * .5f, 72 * PX);
                            o.a.localPosition = new Vector3(0, hgt * PX * .5f + 12 * PX, 0);
                        }
                        break;
                    }
                case "steam":
                    {
                        t.position = Cfg.P(o.x, G, 0);
                        bool burst = o.phs == "burst", warn = o.phs == "warn";
                        if (o.a)
                        {
                            float hgt = burst ? (G - 34 - 170) : (warn ? 30 + Mathf.Sin(T * 28) * 8 : 0);
                            o.a.gameObject.SetActive(hgt > 1);
                            o.a.localScale = new Vector3(58 * PX, hgt * PX * .5f, 58 * PX);
                            o.a.localPosition = new Vector3(0, (44 + hgt * .5f) * PX, 0);
                        }
                        break;
                    }
                case "sprinkler":
                    {
                        t.position = Cfg.P(o.x, G, 0);
                        bool burst = o.phs == "burst", warn = o.phs == "warn";
                        for (int n = 0; n < 7; n++)
                        {
                            float u = n / 6f;
                            float reach = burst ? 1f : (warn ? .25f : 0f);
                            var b = o.list[n];
                            b.gameObject.SetActive(reach > 0f && u <= reach);
                            b.localPosition = new Vector3((-210 * u) * PX, (8 + 130 * 4 * u * (1 - u) + 14) * PX, 0);
                        }
                        break;
                    }
                case "debris":
                    t.position = Cfg.P(o.x, o.y, 0);
                    if (o.a) o.a.localRotation = Quaternion.Euler(0, 0, -o.rot * Mathf.Rad2Deg);
                    break;
            }
        }
    }
}
