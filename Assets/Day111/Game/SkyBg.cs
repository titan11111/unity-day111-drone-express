using UnityEngine;

namespace Day111
{
    // HTML版 drawSky に合わせた背景：グラデ空・太陽・雲（視差）・鳥。カメラの子として最奥に置く。
    public class SkyBg
    {
        const float DIST = 200f, SW = 960f, SH = 540f, GROUND = 470f;
        Transform root; Camera cam;
        SpriteRenderer skyD, skyS, glow, sunDisc, sunCore;
        SpriteRenderer[] lowD = new SpriteRenderer[7], lowS = new SpriteRenderer[7], hiD = new SpriteRenderer[4], hiS = new SpriteRenderer[4];
        SpriteRenderer[] birds = new SpriteRenderer[9];
        Sprite[] cloudD = new Sprite[3], cloudS = new Sprite[3], birdUp, birdDn;
        ParticleSystem rain;

        static float R(float i, float k) { return Mathf.Abs(Mathf.Sin(i * 12.9898f + k * 78.233f) * 43758.5453f) % 1f; }
        static Color H(string h) { Color c; ColorUtility.TryParseHtmlString(h, out c); return c; }

        public SkyBg(Camera c)
        {
            cam = c;
            root = new GameObject("SkyBg").transform; root.SetParent(cam.transform, false);
            skyD = Spr(Grad(new[] { 0f, .45f, .78f, 1f }, new[] { H("#3f679c"), H("#9fb3c8"), H("#efc79a"), H("#f7d2a0") }), -100);
            skyS = Spr(Grad(new[] { 0f, .45f, .78f, 1f }, new[] { H("#1d222c"), H("#3a4052"), H("#5a5660"), H("#665f58") }), -99);
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = H("#9fb3c8");
            glow = Spr(GlowTex(), -98); sunDisc = Spr(DiscTex(new Color(1f, .98f, .9f)), -97); sunCore = Spr(DiscTex(Color.white), -96);
            for (int k = 0; k < 3; k++) { cloudD[k] = SpriteOf(CloudTex(k, false)); cloudS[k] = SpriteOf(CloudTex(k, true)); }
            for (int i = 0; i < 4; i++) { hiD[i] = Spr(cloudD[i % 3], -95); hiS[i] = Spr(cloudS[i % 3], -95); }
            for (int i = 0; i < 7; i++) { lowD[i] = Spr(cloudD[i % 3], -94); lowS[i] = Spr(cloudS[i % 3], -94); }
            birdUp = new[] { SpriteOf(BirdTex(true)) }; birdDn = new[] { SpriteOf(BirdTex(false)) };
            for (int i = 0; i < 9; i++) birds[i] = Spr(birdUp[0], -93);
            rain = BuildRain();
        }

        // ハリケーン時の雨（カメラ正面のローカル空間で常時降らせ、emissionでstormに応じて強弱）
        ParticleSystem BuildRain()
        {
            var go = new GameObject("Rain"); go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0, 6, 16); go.transform.localRotation = Quaternion.identity;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.6f; main.startSpeed = 0f; main.startSize = 1f;
            main.startColor = new Color(.82f, .88f, .95f, .55f);
            main.maxParticles = 1200; main.gravityModifier = 0f; main.playOnAwake = true;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(34f, 1f, 18f);
            var vel = ps.velocityOverLifetime; vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-5f, -3f);
            vel.y = new ParticleSystem.MinMaxCurve(-42f, -34f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var em = ps.emission; em.rateOverTime = 0f;
            var sizeByLife = ps.sizeOverLifetime; // 使わない（棒状はStretched Billboardで表現）
            sizeByLife.enabled = false;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.12f; r.lengthScale = 2.2f;
            var sh = Shader.Find("Legacy Shaders/Particles/Alpha Blended") ?? Shader.Find("Particles/Alpha Blended Premultiply") ?? Shader.Find("Sprites/Default");
            var mat = new Material(sh); mat.mainTexture = RainTex();
            r.sharedMaterial = mat;
            ps.Play();
            return ps;
        }

        static Texture2D RainTex()
        {
            int w = 8, h = 64; var t = Mk(w, h);
            for (int y = 0; y < h; y++)
            {
                float u = y / (float)(h - 1), edge = Mathf.Min(u, 1f - u) * 2f;
                float a = Mathf.Clamp01(edge) * .9f;
                for (int x = 0; x < w; x++) t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.Apply(); return t;
        }

        Sprite SpriteOf(Texture2D t) { return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), 100f); }
        SpriteRenderer Spr(Texture2D t, int order) { return Spr(SpriteOf(t), order); }
        SpriteRenderer Spr(Sprite s, int order)
        {
            var go = new GameObject("s"); go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingOrder = order; return sr;
        }

        static Texture2D Mk(int w, int h) { var t = new Texture2D(w, h, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; return t; }

        static Texture2D Grad(float[] pos, Color[] col)
        {
            int h = 256; var t = Mk(4, h);
            for (int y = 0; y < h; y++)
            {
                float u = 1f - (y + .5f) / h;               // 上=0
                float p = Mathf.Clamp01(u * SH / GROUND);   // HTMLは地平線(470px)までで1
                Color c = col[col.Length - 1];
                for (int k = 0; k < pos.Length - 1; k++) if (p <= pos[k + 1]) { c = Color.Lerp(col[k], col[k + 1], Mathf.InverseLerp(pos[k], pos[k + 1], p)); break; }
                for (int x = 0; x < 4; x++) t.SetPixel(x, y, c);
            }
            t.Apply(); return t;
        }

        static Texture2D GlowTex()
        {
            int n = 256; var t = Mk(n, n);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - n / 2f) * (x - n / 2f) + (y - n / 2f) * (y - n / 2f)) / (n / 2f);
                Color c;
                if (d < .12f) c = Color.Lerp(new Color(1f, .96f, .82f, .95f), new Color(1f, .84f, .59f, .55f), d / .12f);
                else c = Color.Lerp(new Color(1f, .84f, .59f, .55f), new Color(1f, .745f, .47f, 0f), Mathf.Clamp01((d - .12f) / .88f));
                t.SetPixel(x, y, c);
            }
            t.Apply(); return t;
        }

        static Texture2D DiscTex(Color col)
        {
            int n = 64; var t = Mk(n, n);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - n / 2f + .5f) * (x - n / 2f + .5f) + (y - n / 2f + .5f) * (y - n / 2f + .5f)) / (n / 2f);
                t.SetPixel(x, y, new Color(col.r, col.g, col.b, Mathf.Clamp01((1f - d) * 8f)));
            }
            t.Apply(); return t;
        }

        // 立体的な積雲：ふくらみの合成＋上は明るく下は影
        static Texture2D CloudTex(int seed, bool storm)
        {
            int w = 256, h = 96; var t = Mk(w, h);
            var rnd = new System.Random(seed * 17 + 5);
            int nb = 9; var bx = new float[nb]; var by = new float[nb]; var br = new float[nb];
            for (int i = 0; i < nb; i++)
            {
                float u = (i + .5f) / nb;
                bx[i] = w * (.14f + .72f * u); br[i] = h * (.22f + .2f * (float)rnd.NextDouble()) * (1f - Mathf.Abs(u - .5f) * .9f) * 1.25f;
                by[i] = h * .36f + br[i] * .45f * (float)rnd.NextDouble();
            }
            Color top = storm ? new Color(.46f, .48f, .54f) : new Color(1f, .97f, .93f);
            Color bot = storm ? new Color(.19f, .21f, .26f) : new Color(.72f, .76f, .88f);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float dens = 0;
                for (int i = 0; i < nb; i++) { float dx = x - bx[i], dy = (y - by[i]) * 1.1f; float q = 1f - (dx * dx + dy * dy) / (br[i] * br[i]); if (q > dens) dens = q; }
                if (y < h * .24f) dens *= Mathf.Clamp01(y / (h * .24f));    // 下を平らに
                float a = Mathf.SmoothStep(0, 1, Mathf.Clamp01(dens * 2.2f));
                float shade = Mathf.Clamp01(1f - (float)y / (h * .85f));
                Color c = Color.Lerp(bot, top, Mathf.Clamp01(shade * 1.1f));
                t.SetPixel(x, y, new Color(c.r, c.g, c.b, a * (storm ? .95f : .92f)));
            }
            t.Apply(); return t;
        }

        static Texture2D BirdTex(bool up)
        {
            int w = 32, h = 16; var t = Mk(w, h);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) t.SetPixel(x, y, new Color(0, 0, 0, 0));
            for (int x = 2; x < 30; x++)
            {
                float u = Mathf.Abs(x - 15.5f) / 13.5f;
                int iy = Mathf.RoundToInt(up ? 5f + u * 8f : 9f - u * 6f);
                for (int k = 0; k < 2; k++) t.SetPixel(x, Mathf.Clamp(iy + k, 0, h - 1), new Color(.12f, .13f, .18f, 1f));
            }
            t.Apply(); return t;
        }

        // 画面px(左上原点)→カメラ子のローカル座標
        Vector3 L(float px, float py, float vw, float vh, int z) { return new Vector3((px / SW - .5f) * vw, (.5f - py / SH) * vh, DIST - z * .01f); }

        public void Tick(float camX, float t, float storm)
        {
            float vh = 2f * DIST * Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad), vw = vh * cam.aspect, u = vh / SH;
            float a = 1f - storm;
            // 空
            Fill(skyD, vw * 1.05f, vh * 1.05f); Fill(skyS, vw * 1.05f, vh * 1.05f);
            skyD.color = Color.white; skyS.color = new Color(1, 1, 1, storm);
            // 太陽
            float sx = SW * .8f, sy = GROUND - 92f;
            Place(glow, L(sx, sy, vw, vh, 1), 660f * u); glow.color = new Color(1, 1, 1, a);
            Place(sunDisc, L(sx, sy, vw, vh, 2), 44f * u); sunDisc.color = new Color(1, 1, 1, a);
            Place(sunCore, L(sx, sy, vw, vh, 3), 26f * u); sunCore.color = new Color(1, 1, 1, a * .9f);
            glow.enabled = sunDisc.enabled = sunCore.enabled = a > .02f;
            // 雲
            float off = camX * .035f + t * 4f;
            for (int i = 0; i < 4; i++)
            {
                float x = ((i * 380 - off * .5f) % 1700 + 1700) % 1700 - 300, y = 24 + R(i, 9) * 46;
                var p = L(x + 310, y + 33, vw, vh, 4);
                Cloud(hiD[i], hiS[i], p, 620f * u, 66f * u, .34f, storm);
            }
            for (int i = 0; i < 7; i++)
            {
                float x = ((i * 210 - off) % 1500 + 1500) % 1500 - 220, y = 40 + R(i, 3) * 120, s = .8f + R(i, 5) * .9f;
                var p = L(x, y, vw, vh, 5);
                Cloud(lowD[i], lowS[i], p, 460f * s * .8f * u, 170f * s * .8f * u, .96f, storm);
            }
            // 鳥
            Color bc = Color.Lerp(Color.white, new Color(.4f, .4f, .45f), storm);
            for (int k = 0; k < 9; k++)
            {
                float bx = ((k * 53 + t * 22 + camX * .02f) % (SW + 160)) - 80, by = 70 + Mathf.Sin(k * 1.9f) * 38 + Mathf.Sin(t * 2.2f + k) * 4;
                bool up = Mathf.Sin(t * 9 + k * 2) > 0;
                birds[k].sprite = up ? birdUp[0] : birdDn[0];
                Place(birds[k], L(bx, by, vw, vh, 6), 24f * u * 1.0f); birds[k].color = bc;
            }
            // 雨：接近(storm~.45)から薄く、ハリケーン本体(storm→1)でザーザー降りに
            if (rain != null)
            {
                var em = rain.emission;
                float r01 = Mathf.Clamp01((storm - .12f) / .88f);
                em.rateOverTime = r01 * 700f;
            }
        }

        void Fill(SpriteRenderer sr, float w, float h)
        {
            sr.transform.localPosition = new Vector3(0, 0, DIST);
            var sz = sr.sprite.bounds.size; sr.transform.localScale = new Vector3(w / sz.x, h / sz.y, 1f);
        }

        void Place(SpriteRenderer sr, Vector3 p, float worldW)
        {
            sr.transform.localPosition = p;
            var sz = sr.sprite.bounds.size; float s = worldW / sz.x; sr.transform.localScale = new Vector3(s, s, 1f);
        }

        void Cloud(SpriteRenderer d, SpriteRenderer s, Vector3 p, float w, float h, float alpha, float storm)
        {
            var sz = d.sprite.bounds.size;
            var sc = new Vector3(w / sz.x, h / sz.y, 1f);
            d.transform.localPosition = p; d.transform.localScale = sc; s.transform.localPosition = p; s.transform.localScale = sc;
            d.color = new Color(1, 1, 1, alpha * (1f - storm)); s.color = new Color(1, 1, 1, alpha * storm * (alpha > .5f ? 1f : .6f));
        }
    }
}
