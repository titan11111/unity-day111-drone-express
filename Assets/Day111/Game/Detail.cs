using UnityEngine;

namespace Day111
{
    // ビル外壁（レンガ／ガラスカーテンウォール／アールデコ）と煙突の作り込み。すべて手続き生成。
    public static class Detail
    {
        const float PX = Cfg.PX;
        static readonly Material[] facade = new Material[6];
        static Material brick;

        // ---------- 共有マテリアル ----------
        public static Material Facade(int style)
        {
            style = Mathf.Clamp(style, 0, 5);
            if (facade[style]) return facade[style];
            var m = new Material(Mats.Get("Bldg"));
            m.color = Color.white; m.mainTexture = FacadeTex(style);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", (style == 2 || style == 3) ? .6f : .1f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return facade[style] = m;
        }

        public static Material Brick()
        {
            if (brick) return brick;
            var m = new Material(Mats.Get("Brick")); m.color = Color.white; m.mainTexture = BrickTex();
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", .05f);
            return brick = m;
        }

        static float H(int a, int b) { return Mathf.Abs(Mathf.Sin(a * 12.9898f + b * 78.233f) * 43758.5453f) % 1f; }

        static Texture2D NewTex(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Bilinear; t.anisoLevel = 4; return t;
        }

        static Color32 C(float r, float g, float b) { return new Color32((byte)Mathf.Clamp(r, 0, 255), (byte)Mathf.Clamp(g, 0, 255), (byte)Mathf.Clamp(b, 0, 255), 255); }

        static Color Glass(int state, int v, int x, int y, int style)
        {
            float k = v / 64f; Color c;
            switch (state)
            {
                case 1: c = Color.Lerp(new Color(1f, .78f, .46f), new Color(1f, .86f, .6f), k); break;                  // 点灯
                case 2: c = Color.Lerp(new Color(.46f, .62f, .8f), new Color(.75f, .84f, .92f), k); break;                // 空を映す
                case 3: c = new Color(.78f, .72f, .62f); break;                                                           // カーテン
                default: c = Color.Lerp(new Color(.15f, .2f, .28f), new Color(.31f, .39f, .5f), k); break;               // 暗い
            }
            if (state != 1 && ((x + y) % 90) < 10) c += new Color(.09f, .09f, .09f);                                      // 斜めの映り込み
            if (style == 3) c *= new Color(.85f, 1f, .95f);
            return c;
        }

        static Texture2D FacadeTex(int style)
        {
            const int N = 256; var t = NewTex(N); var px = new Color32[N * N];
            var st = new int[4, 4];
            for (int a = 0; a < 4; a++) for (int b = 0; b < 4; b++) { float h = H(a * 7 + style, b * 3 + 1); st[a, b] = h < .42f ? 0 : h < .66f ? 1 : h < .88f ? 2 : 3; }
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                int cx = x / 64, cy = y / 64, u = x % 64, v = y % 64, s = st[cx, cy];
                Color32 col;
                if (style <= 1)
                {
                    Color baseC = style == 0 ? new Color(.69f, .52f, .38f) : new Color(.59f, .32f, .26f);
                    bool win = u >= 14 && u < 50 && v >= 10 && v < 50, frame = u >= 11 && u < 53 && v >= 7 && v < 53;
                    if (win)
                    {
                        bool mull = (u >= 31 && u < 33) || (v >= 29 && v < 31);
                        Color g = mull ? new Color(.88f, .87f, .84f) : Glass(s, v, x, y, style);
                        if (!mull && v > 46) g *= .8f;
                        col = g;
                    }
                    else if (frame) col = C(214, 204, 186);
                    else if (v >= 53 && v < 58 && u >= 9 && u < 55) col = C(190, 178, 160);
                    else if (v >= 4 && v < 8 && u >= 9 && u < 55) col = C(170, 160, 145);
                    else
                    {
                        int row = y / 6, by = y % 6, bx = (x + (row % 2) * 8) % 16;
                        if (by == 0 || bx == 0) col = baseC * .72f + new Color(.12f, .11f, .1f);
                        else { float k = .9f + H(row, (x + (row % 2) * 8) / 16) * .2f; col = baseC * k; }
                        if (v >= 58 || v < 4) col = Color.Lerp(col, new Color(.8f, .75f, .68f), .35f);
                    }
                }
                else if (style <= 3)
                {
                    bool spandrel = v < 14, mull = (x % 16) < 2 || v == 14 || v == 15;
                    if (spandrel) col = C(52 + 8 * (x % 32 < 16 ? 1 : 0), 62, 74);
                    else if (mull) col = C(30, 34, 40);
                    else col = Glass(s, v, x, y, style);
                }
                else
                {
                    Color stone = style == 4 ? new Color(.84f, .78f, .67f) : new Color(.6f, .6f, .62f);
                    bool pier = u < 6 || u >= 58;
                    bool slit = ((u >= 10 && u < 20) || (u >= 27 && u < 37) || (u >= 44 && u < 54)) && v >= 8 && v < 52;
                    bool slitFrame = ((u >= 9 && u < 21) || (u >= 26 && u < 38) || (u >= 43 && u < 55)) && v >= 7 && v < 53;
                    if (slit) col = Glass(s, v, x, y, style);
                    else if (slitFrame) col = stone * .82f;
                    else if (v >= 54) col = stone * (v == 58 || v == 59 ? .7f : .88f);
                    else if (pier) col = stone * 1.04f;
                    else col = stone * (.96f + H(x / 4, y / 4) * .06f);
                }
                px[y * N + x] = col;
            }
            t.SetPixels32(px); t.Apply(true); return t;
        }

        static Texture2D BrickTex()
        {
            const int N = 64; var t = NewTex(N); var px = new Color32[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                int row = y / 8, by = y % 8, off = (row % 2) * 8, bx = (x + off) % 16;
                Color32 c;
                if (by == 0 || bx == 0) c = C(150, 138, 125);
                else { float k = .85f + H(row, (x + off) / 16) * .3f; c = C(158 * k, 70 * k, 52 * k); }
                px[y * N + x] = c;
            }
            t.SetPixels32(px); t.Apply(true); return t;
        }

        // ---------- 外壁つき箱（UVを建物ごとに設定。マテリアルは共有できる）----------
        public static Transform FacadeBox(Transform p, Material m, float cx, float cy, float w, float h, float d, float z, float tx, float ty, float dtx)
        {
            var go = new GameObject("Facade"); go.transform.SetParent(p, false);
            go.transform.localPosition = new Vector3(cx * PX, cy * PX, z * PX);
            float hw = w * PX / 2, hh = h * PX / 2, hd = d * PX / 2;
            var v = new Vector3[12]; var uv = new Vector2[12]; var n = new Vector3[12];
            v[0] = new Vector3(-hw, -hh, -hd); v[1] = new Vector3(hw, -hh, -hd); v[2] = new Vector3(hw, hh, -hd); v[3] = new Vector3(-hw, hh, -hd);
            uv[0] = new Vector2(0, 0); uv[1] = new Vector2(tx, 0); uv[2] = new Vector2(tx, ty); uv[3] = new Vector2(0, ty);
            v[4] = new Vector3(hw, -hh, -hd); v[5] = new Vector3(hw, -hh, hd); v[6] = new Vector3(hw, hh, hd); v[7] = new Vector3(hw, hh, -hd);
            v[8] = new Vector3(-hw, -hh, hd); v[9] = new Vector3(-hw, -hh, -hd); v[10] = new Vector3(-hw, hh, -hd); v[11] = new Vector3(-hw, hh, hd);
            for (int i = 4; i < 12; i += 4) { uv[i] = new Vector2(0, 0); uv[i + 1] = new Vector2(dtx, 0); uv[i + 2] = new Vector2(dtx, ty); uv[i + 3] = new Vector2(0, ty); }
            for (int i = 0; i < 4; i++) { n[i] = new Vector3(0, 0, -1); n[4 + i] = new Vector3(1, 0, 0); n[8 + i] = new Vector3(-1, 0, 0); }
            var tri = new int[18];
            for (int f = 0; f < 3; f++) { int b = f * 4, o = f * 6; tri[o] = b; tri[o + 1] = b + 2; tri[o + 2] = b + 1; tri[o + 3] = b; tri[o + 4] = b + 3; tri[o + 5] = b + 2; }
            var mesh = new Mesh(); mesh.vertices = v; mesh.uv = uv; mesh.normals = n; mesh.triangles = tri; mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true;
            return go.transform;
        }

        public static int StyleOf(float w, float h)
        {
            int hs = Mathf.Abs((int)(w * 13 + h * 7));
            if (h > 520) return 4 + hs % 2;
            return hs % 4;
        }

        static void Tiles(float w, float h, out float tx, out float ty) { tx = Mathf.Max(1, Mathf.Round(w / 192f)); ty = Mathf.Max(1, Mathf.Round(h / 224f)); }

        // ---------- ビル ----------
        public static void Building(Ob o, Transform r, float FRONT, bool detail)
        {
            float d = 120, z = FRONT + d / 2, w = o.w, h = o.h;
            int style = StyleOf(w, h), hs = Mathf.Abs((int)(w * 3 + h));
            var stone = Mats.Get("Concrete"); var metal = Mats.Get("Metal"); var dark = Mats.Get("MetalDark");
            float tx, ty; Tiles(w, h, out tx, out ty);
            FacadeBox(r, Facade(style), w / 2, h / 2, w, h, d, z, tx, ty, 1);
            Prims.Box(r, stone, w / 2, 12, w + 8, 24, d + 8, z);                        // 基壇
            Prims.Box(r, stone, w / 2, h + 5, w + 12, 10, d + 12, z);                   // コーニス
            Prims.Box(r, dark, w / 2, h + 13, w + 2, 6, d + 2, z, false);               // パラペット
            float top = h + 16;
            if (detail)
            {
                Prims.Box(r, Mats.Get("Glass"), w / 2, 22, w * .84f, 26, 2, FRONT - 1.5f, false);       // 1階店舗ガラス
                Prims.Box(r, stone, w / 2, 40, w * .9f, 4, 6, FRONT - 3f, false);                       // 庇
                Prims.Box(r, stone, 5, h / 2, 10, h, 12, FRONT - 3f);                                   // 角のピラスター
                Prims.Box(r, stone, w - 5, h / 2, 10, h, 12, FRONT - 3f);
                for (int k = 1; k < 4; k++) Prims.Box(r, stone, w / 2, h * k / 4f, w + 4, 3, d + 4, z, false);   // 帯状のせり出し
            }
            // セットバック（高いビルは段々に）
            if (h > 400)
            {
                float w2 = w * .68f, h2 = Mathf.Min(h * .22f, 150f), d2 = d * .72f; float tx2, ty2; Tiles(w2, h2, out tx2, out ty2);
                FacadeBox(r, Facade(style), w / 2, top + h2 / 2, w2, h2, d2, z, tx2, ty2, 1);
                Prims.Box(r, stone, w / 2, top + h2 + 4, w2 + 10, 8, d2 + 10, z);
                top += h2 + 8;
                if (h > 520)
                {
                    float w3 = w2 * .55f, h3 = 70; float tx3, ty3; Tiles(w3, h3, out tx3, out ty3);
                    FacadeBox(r, Facade(style), w / 2, top + h3 / 2, w3, h3, d2 * .6f, z, tx3, ty3, 1);
                    Prims.Box(r, stone, w / 2, top + h3 + 3, w3 + 8, 6, d2 * .6f + 8, z);
                    top += h3 + 6;
                    Prims.Cyl(r, dark, w / 2, top + 35, 5, 70, z, false);
                    Prims.Ball(r, Mats.Get("Led2"), w / 2, top + 72, 9, 9, 9, z, false);
                    top += 76;
                }
            }
            else if (detail || hs % 2 == 0)
            {
                // 屋上設備：給水タンク／室外機／階段室／アンテナ
                float bx = w * (.2f + (hs % 5) * .1f);
                if (hs % 3 == 0)
                {
                    foreach (float lx in new[] { -10f, 10f }) Prims.Box(r, dark, bx + lx, top + 14, 3, 28, 3, z - 6f, false);
                    Prims.Cyl(r, metal, bx, top + 40, 30, 26, z - 6f);
                    Prims.Roof(r, dark, bx, top + 53, 34, 14, 34, z - 6f);
                }
                else
                {
                    Prims.Box(r, stone, bx, top + 14, 36, 28, 30, z - 6f);
                    Prims.Box(r, dark, bx, top + 29, 40, 3, 34, z - 6f, false);
                }
                Prims.Box(r, metal, w * .72f, top + 8, 28, 16, 22, z, false);
                Prims.Box(r, metal, w * .72f + 32, top + 6, 22, 12, 20, z, false);
                Prims.Cyl(r, dark, w * .85f, top + 24, 2.5f, 48, z, false);
                Prims.Box(r, dark, w * .85f, top + 38, 16, 2, 2, z, false);
                Prims.Box(r, dark, w * .85f, top + 28, 12, 2, 2, z, false);
            }
            if (o.ex != null)
                foreach (var e in o.ex)
                {
                    Prims.Box(r, metal, e[0] + e[1] / 2, h + 16 + e[2] / 2, e[1], e[2], 50, FRONT + 50);
                    Prims.Cyl(r, dark, e[0] + e[1] / 2, h + 16 + e[2] + 2, e[1] * .6f, 4, FRONT + 50);
                }
        }


        // ---------- アメリカ郊外の家（お客さんの家）----------
        // 原点：左端・正面(z=0)・地面(y=0)。奥行きは+z、玄関ポーチは手前(-z)へ張り出す。単位px。
        static Material siding;
        static Texture2D SidingTex()
        {
            const int N = 64; var t = NewTex(N); var px = new Color32[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                int by = y % 8; float k = by == 0 ? .74f : by == 1 ? 1.04f : 1f - by * .006f;
                k *= .98f + H(x / 16, y / 8) * .04f;
                px[y * N + x] = C(255 * k, 255 * k, 255 * k);
            }
            t.SetPixels32(px); t.Apply(true); return t;
        }

        static Material Siding(Color tint)
        {
            var m = new Material(Mats.Get("Bldg")); m.mainTexture = SidingTex(); m.color = tint;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", .12f);
            return m;
        }

        public static void AmericanHouse(Transform r)
        {
            var trim = Mats.Tint("Concrete", new Color(.96f, .96f, .93f));
            var roof = Mats.Tint("Roof0", new Color(.24f, .25f, .29f));
            var shut = Mats.Tint("Wall0", new Color(.16f, .3f, .24f));
            var doorM = Mats.Tint("Door", new Color(.56f, .12f, .1f));
            var glass = Mats.Get("Glass"); var dark = Mats.Get("MetalDark"); var conc = Mats.Get("Concrete"); var leaf = Mats.Get("Leaf");
            var sid = Siding(new Color(.62f, .73f, .84f));
            if (siding == null) siding = sid;
            float MW = 230, MH = 170, MD = 150, GW = 112, GH = 96, GD = 140;

            // 本体（2階建て）とガレージ
            FacadeBox(r, sid, MW / 2, MH / 2, MW, MH, MD, MD / 2, MW / 64f, MH / 64f, MD / 64f);
            FacadeBox(r, sid, MW + GW / 2, GH / 2, GW, GH, GD, GD / 2, GW / 64f, GH / 64f, GD / 64f);
            Prims.Box(r, conc, (MW + GW) / 2, 3, MW + GW + 6, 6, MD + 6, MD / 2, false);          // 基礎

            // 屋根（棟がx方向の切妻）
            var rt = Prims.Roof(r, roof, MW / 2, MH, MD + 40, 64, MW + 24, MD / 2); rt.localRotation = Quaternion.Euler(0, 90, 0);
            var rg = Prims.Roof(r, roof, MW + GW / 2, GH, GD + 34, 36, GW + 20, GD / 2); rg.localRotation = Quaternion.Euler(0, 90, 0);
            Prims.Box(r, trim, MW / 2, MH - 1, MW + 24, 5, 5, -18f, false);                       // 軒先の破風
            Prims.Box(r, trim, MW + GW / 2, GH - 1, GW + 20, 4, 5, -14f, false);

            // 窓（白枠・雨戸・窓台）
            float[] ux = { 45, 115, 185 }, lx = { 45, 185 };
            foreach (float x in ux) Win(r, x, 130, 34, 46, trim, glass, shut);
            foreach (float x in lx) Win(r, x, 52, 34, 46, trim, glass, shut);
            // 玄関ドア
            Prims.Box(r, trim, 115, 38, 40, 72, 3, -1f, false);
            Prims.Box(r, doorM, 115, 36, 30, 66, 4, -2.5f, false);
            Prims.Box(r, glass, 115, 64, 24, 9, 3, -4f, false);
            Prims.Ball(r, Mats.Get("Led"), 124, 34, 3.5f, 3.5f, 3.5f, -5f, false);
            // ポーチ（柱・屋根・階段）
            Prims.Box(r, conc, 115, 7, 124, 14, 56, -28f);
            Prims.Box(r, conc, 115, 3, 60, 6, 22, -66f, false);
            Prims.Box(r, conc, 115, 10, 70, 6, 12, -58f, false);
            foreach (float x in new[] { 62f, 168f }) Prims.Cyl(r, trim, x, 58, 8, 88, -52f);
            Prims.Box(r, trim, 115, 104, 134, 6, 62, -28f);
            var pr = Prims.Roof(r, roof, 115, 107, 130, 38, 70, -28f);
            Prims.Box(r, glass, 115, 150, 28, 14, 3, -1.5f, false);                               // 屋根裏の小窓の影
            // ガレージドア
            float gx = MW + GW / 2;
            Prims.Box(r, trim, gx, 38, 92, 72, 3, -1f, false);
            Prims.Box(r, Mats.Tint("Concrete", new Color(.86f, .86f, .84f)), gx, 36, 84, 66, 3, -2.5f, false);
            for (int k = 0; k < 4; k++) Prims.Box(r, dark, gx, 17 + k * 16, 84, 1.4f, 2, -4f, false);
            for (int k = 0; k < 4; k++) Prims.Box(r, glass, gx - 30 + k * 20, 66, 14, 7, 2, -4f, false);
            // 煙突（左の妻壁に沿って屋根の上まで）
            FacadeBox(r, Brick(), 6, 100, 26, 200, 40, MD / 2, 1f, 8f, 1f);
            Chimney(r, 6, 200, 268, MD / 2, true);
            // 前庭：芝の小道・ドライブウェイ・生け垣・フェンス・ポスト・木
            Prims.Box(r, conc, 115, 1, 26, 2, 90, -45f, false);
            Prims.Box(r, conc, gx, 1, 92, 2, 100, -50f, false);
            foreach (float x in new[] { 28f, 62f, 168f, 202f }) Prims.Ball(r, leaf, x, 11, 30, 24, 28, -20f, false);
            for (float x = -10; x < 230; x += 9f)
            {
                if (x > 92 && x < 138) continue;
                Prims.Box(r, trim, x, 14, 4, 26, 2, -72f, false);
            }
            Prims.Box(r, trim, 40, 18, 100, 2.5f, 2, -73f, false); Prims.Box(r, trim, 40, 8, 100, 2.5f, 2, -73f, false);
            Prims.Box(r, trim, 190, 18, 90, 2.5f, 2, -73f, false); Prims.Box(r, trim, 190, 8, 90, 2.5f, 2, -73f, false);
            Prims.Box(r, Mats.Get("Pole"), 245, 18, 3, 36, 3, -80f, false);                       // ポストと郵便受け
            Prims.Box(r, Mats.Tint("Concrete", new Color(.2f, .3f, .55f)), 245, 40, 16, 11, 24, -80f, false);
            Prims.Cyl(r, Mats.Get("Trunk"), -48, 56, 11, 112, 40f);
            Prims.Ball(r, leaf, -48, 130, 78, 66, 74, 40f);
        }

        static void Win(Transform r, float x, float y, float w, float h, Material trim, Material glass, Material shut)
        {
            Prims.Box(r, trim, x, y, w + 8, h + 8, 3, -1f, false);
            Prims.Box(r, glass, x, y, w, h, 3, -2.5f, false);
            Prims.Box(r, trim, x, y, w, 2, 3, -3.5f, false); Prims.Box(r, trim, x, y, 2, h, 3, -3.5f, false);
            Prims.Box(r, trim, x, y - h / 2 - 5, w + 14, 4, 9, -4f, false);
            Prims.Box(r, shut, x - w / 2 - 10, y, 10, h + 6, 3, -2.5f, false);
            Prims.Box(r, shut, x + w / 2 + 10, y, 10, h + 6, 3, -2.5f, false);
        }

        // ---------- 煙突 ----------
        // cx:中心x  baseY:屋根面に埋める高さ  topY:煙突の天端  z:奥行中心
        public static void Chimney(Transform r, float cx, float baseY, float topY, float z, bool smoke)
        {
            var br = Brick(); var stone = Mats.Get("Concrete"); var dark = Mats.Get("MetalDark");
            float hgt = topY - baseY, wd = 22;
            Prims.Box(r, dark, cx, baseY + 6, wd + 6, 9, wd + 6, z, false);                        // 雨仕舞い
            FacadeBox(r, br, cx, baseY + hgt / 2, wd, hgt, wd, z, 1f, Mathf.Max(1f, Mathf.Round(hgt / 26f)), 1f);
            Prims.Box(r, br, cx, topY - 7, wd + 4, 5, wd + 4, z);                                  // 持ち送り1
            Prims.Box(r, br, cx, topY - 2, wd + 8, 5, wd + 8, z);                                  // 持ち送り2
            Prims.Box(r, stone, cx, topY + 3.5f, wd + 12, 5, wd + 12, z);                          // 笠木
            Prims.Cyl(r, Mats.Get("Roof0"), cx - 5, topY + 11, 8, 11, z, false);                   // 煙突の土器ポット
            Prims.Cyl(r, Mats.Get("Roof0"), cx + 5, topY + 11, 8, 11, z, false);
            Prims.Cyl(r, dark, cx - 5, topY + 16.8f, 6, .5f, z, false);
            Prims.Cyl(r, dark, cx + 5, topY + 16.8f, 6, .5f, z, false);
            if (!smoke) return;
            var go = new GameObject("Smoke"); go.transform.SetParent(r, false);
            go.transform.localPosition = new Vector3(cx * PX, (topY + 18) * PX, (z - 6) * PX);
            go.AddComponent<ChimneySmoke>();
        }
    }

    // 煙突のけむり（半透明の丸い煙がゆらゆら昇って消える）
    public class ChimneySmoke : MonoBehaviour
    {
        const int N = 6; SpriteRenderer[] p = new SpriteRenderer[N]; float seed;
        void Start()
        {
            Hud.MakeSprites(); seed = Random.value * 10f;
            for (int i = 0; i < N; i++)
            {
                var g = new GameObject("p"); g.transform.SetParent(transform, false);
                p[i] = g.AddComponent<SpriteRenderer>(); p[i].sprite = Hud.Circle; p[i].sortingOrder = 5;
            }
        }
        void Update()
        {
            for (int i = 0; i < N; i++)
            {
                if (!p[i]) continue;
                float u = (Time.time * .3f + seed + i / (float)N) % 1f;
                float px = 14f + u * 70f, size = 10f + u * 46f;
                p[i].transform.localPosition = new Vector3((Mathf.Sin(u * 5f + i) * 8f + u * 24f) * Cfg.PX, u * 88f * Cfg.PX, -.1f);
                float s = size * Cfg.PX / (p[i].sprite.rect.width / p[i].sprite.pixelsPerUnit);
                p[i].transform.localScale = new Vector3(s, s, 1f);
                float a = Mathf.Sin(u * Mathf.PI) * .42f;
                p[i].color = new Color(.86f, .86f, .88f, a);
            }
        }
    }

    // 足が地面にめり込まないように、アニメ中の足の最下点を地面の高さに揃える
    public class FootGround : MonoBehaviour
    {
        Animator an; Vector3 basePos; float refOff; int frames; Transform[] bones;
        public void Init(Animator a, Vector3 pos)
        {
            an = a; basePos = pos; frames = 0; refOff = float.NaN;
            var l = new System.Collections.Generic.List<Transform>();
            foreach (var b in new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightToes })
            { var t = a ? a.GetBoneTransform(b) : null; if (t) l.Add(t); }
            bones = l.ToArray();
        }
        float Lowest() { float m = float.MaxValue; foreach (var t in bones) m = Mathf.Min(m, t.position.y); return m; }
        void LateUpdate()
        {
            if (an == null || bones == null || bones.Length == 0) return;
            transform.position = basePos;
            float low = Lowest();
            if (float.IsNaN(refOff)) { if (++frames >= 4) refOff = low - basePos.y; return; }
            float need = basePos.y + refOff;
            if (low < need) transform.position = basePos + Vector3.up * (need - low);
        }
    }
}
