using UnityEngine;

namespace Day111
{
    // 外部ファイル無しの手続き生成サウンド（WebGLはタップ後に鳴る）
    // ・BGM：通常（疾走感のあるAmマイナー）／嵐（重い4つ打ち）を同期ループしてクロスフェード
    // ・ドローン音：4ローターのうなり＋ブレード羽ばたき＋サーボのホイン音
    public class Snd : MonoBehaviour
    {
        static Snd I;
        AudioSource sfx, rotor, servo, wind, musA, musB;
        AudioClip cHit, cPulse, cPop, cTick, cWin, cLose, cWarn, cGust, cLand, cSpin, cLoopRotor, cLoopServo, cLoopWind, cMusA, cMusB;
        public static bool On = true;
        const int SR = 22050;
        float musVol, musTarget;

        public static void Boot(GameObject host)
        {
            if (I != null) return;
            I = host.AddComponent<Snd>();
            I.Init();
        }

        void Init()
        {
            sfx = Src(false); rotor = Src(true); servo = Src(true); wind = Src(true); musA = Src(true); musB = Src(true);
            cHit = Tone(220, 60, .22, .9, 0, .5f);
            cPulse = Tone(520, 180, .18, .6, 1, .15f);
            cPop = Tone(700, 1100, .09, .6, 0, .1f);
            cTick = Tone(1200, 1500, .05, .4, 1, 0);
            cWin = Chord(new[] { 523f, 659f, 784f, 1046f }, .16f);
            cLose = Tone(330, 70, .8, .8, 2, .2f);
            cWarn = Tone(880, 880, .12, .45, 1, 0);
            cGust = Tone(150, 60, .7, .55, 0, 1f);
            cLand = Tone(140, 70, .15, .7, 0, .3f);
            cSpin = SpinUp();
            cLoopRotor = RotorLoop();
            cLoopServo = ServoLoop();
            cLoopWind = WindLoop();
            rotor.clip = cLoopRotor; servo.clip = cLoopServo; wind.clip = cLoopWind;
            BuildMusic();
            musA.clip = cMusA; musB.clip = cMusB;
            musA.volume = musB.volume = 0f;
        }

        AudioSource Src(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>(); s.playOnAwake = false; s.loop = loop; s.volume = 0f; s.spatialBlend = 0f; return s;
        }

        // ---------------- 効果音 ----------------
        // wave: 0=sine 1=triangle 2=saw ; noise: ノイズ混合率
        static AudioClip Tone(float f0, float f1, double dur, double vol, int wave, float noise)
        {
            int n = (int)(SR * dur);
            var d = new float[n];
            double ph = 0;
            var rnd = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                double u = (double)i / n;
                double f = f0 + (f1 - f0) * u;
                ph += f / SR;
                double s;
                double frac = ph - System.Math.Floor(ph);
                if (wave == 0) s = System.Math.Sin(ph * 6.2831853);
                else if (wave == 1) s = 4 * System.Math.Abs(frac - .5) - 1;
                else s = 2 * frac - 1;
                s = s * (1 - noise) + ((rnd.NextDouble() * 2 - 1) * noise);
                double env = System.Math.Min(1, i / 120.0) * (1 - u) * (1 - u);
                d[i] = (float)(s * env * vol);
            }
            var c = AudioClip.Create("t", n, 1, SR, false); c.SetData(d, 0); return c;
        }

        static AudioClip Chord(float[] fr, float step)
        {
            int n = (int)(SR * (step * fr.Length + .5f));
            var d = new float[n];
            for (int k = 0; k < fr.Length; k++)
            {
                int st = (int)(SR * step * k);
                for (int i = st; i < n; i++)
                {
                    double t = (double)(i - st) / SR;
                    d[i] += (float)(System.Math.Sin(6.2831853 * fr[k] * t) * System.Math.Exp(-t * 4) * .28);
                }
            }
            var c = AudioClip.Create("c", n, 1, SR, false); c.SetData(d, 0); return c;
        }

        // 起動音：ローターが回り始めて、サーボがキュイーンと上がる
        static AudioClip SpinUp()
        {
            double dur = 1.4; int n = (int)(SR * dur); var d = new float[n];
            double p1 = 0, p2 = 0, p3 = 0; var rnd = new System.Random(3); float lp = 0;
            for (int i = 0; i < n; i++)
            {
                double u = (double)i / n, e = u * u * (3 - 2 * u);
                p1 += (50 + 150 * e) / SR; p2 += (500 + 2700 * e * e) / SR; p3 += (51 + 152 * e) / SR;
                float nz = (float)(rnd.NextDouble() * 2 - 1); lp += (nz - lp) * .25f;
                double saw = 2 * (p1 - System.Math.Floor(p1)) - 1, saw2 = 2 * (p3 - System.Math.Floor(p3)) - 1;
                double s = (saw + saw2) * .25 * (.3 + e) + System.Math.Sin(p2 * 6.2831853) * .18 * e + lp * .25 * e;
                double env = System.Math.Min(1, i / 400.0) * (u > .85 ? (1 - u) / .15 : 1);
                d[i] = (float)(s * env * .8);
            }
            var c = AudioClip.Create("spin", n, 1, SR, false); c.SetData(d, 0); return c;
        }

        // 4ローターの重なり：整数サイクル/秒なので1秒ループが継ぎ目なし
        static AudioClip RotorLoop()
        {
            int n = SR; var d = new float[n]; var rnd = new System.Random(11);
            float[] baseF = { 96, 100, 104, 108 };
            float lp = 0, lp2 = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SR; double s = 0;
                foreach (var f in baseF) for (int k = 1; k <= 7; k++) s += System.Math.Sin(6.2831853 * f * k * t + f) * (1.0 / (k * 1.3));
                s *= .11;
                double flutter = .62 + .38 * System.Math.Sin(6.2831853 * 24 * t);       // ブレードの羽ばたき
                float nz = (float)(rnd.NextDouble() * 2 - 1); lp += (nz - lp) * .35f; lp2 += (lp - lp2) * .35f;
                d[i] = (float)(s * flutter) + lp2 * .5f;
            }
            Seam(d, 1500);
            var c = AudioClip.Create("rotor", n, 1, SR, false); c.SetData(d, 0); return c;
        }

        // サーボ/モーターのホイン音
        static AudioClip ServoLoop()
        {
            int n = SR; var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SR;
                double fm = System.Math.Sin(6.2831853 * 3 * t) * .6;
                double s = System.Math.Sin(6.2831853 * 1600 * t + fm * 4) * .5 + System.Math.Sin(6.2831853 * 2400 * t + fm * 3) * .3 + System.Math.Sin(6.2831853 * 3200 * t) * .12;
                d[i] = (float)(s * (.8 + .2 * System.Math.Sin(6.2831853 * 6 * t)) * .5);
            }
            var c = AudioClip.Create("servo", n, 1, SR, false); c.SetData(d, 0); return c;
        }

        static AudioClip WindLoop()
        {
            int n = SR * 2; var d = new float[n]; var rnd = new System.Random(21); float lp = 0, lp2 = 0;
            for (int i = 0; i < n; i++)
            {
                float nz = (float)(rnd.NextDouble() * 2 - 1); lp += (nz - lp) * .08f; lp2 += (lp - lp2) * .15f;
                double t = (double)i / SR;
                d[i] = lp2 * 3.2f * (float)(.7 + .3 * System.Math.Sin(6.2831853 * .5 * t));
            }
            Seam(d, 3000);
            var c = AudioClip.Create("wind", n, 1, SR, false); c.SetData(d, 0); return c;
        }

        static void Seam(float[] d, int len)
        {
            int n = d.Length;
            for (int i = 0; i < len; i++) { float a = i / (float)len; d[n - len + i] = d[n - len + i] * (1 - a) + d[i] * a * 0 + d[n - len + i] * 0; }
            // 末尾を先頭へなめらかに寄せる
            for (int i = 0; i < len; i++) { float a = i / (float)len; d[n - len + i] = Mathf.Lerp(d[n - len + i], d[0], a * a); }
        }

        // ---------------- BGM ----------------
        const float BPM = 132f;
        static float Hz(int midi) { return 440f * Mathf.Pow(2f, (midi - 69) / 12f); }

        void BuildMusic()
        {
            float beat = 60f / BPM; int bars = 8; int per = Mathf.RoundToInt(beat * 4 * SR); int n = per * bars;
            int tail = per / 2;
            var a = new float[n + tail]; var b = new float[n + tail];

            // 通常：Am F C G | Am F G Am
            int[] rootA = { 45, 41, 48, 43, 45, 41, 43, 45 };
            int[][] triA = { new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 60, 64 }, new[] { 55, 59, 62 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 }, new[] { 57, 60, 64 } };
            int[] lead = { 76, 72, 74, 76, 77, 76, 72, -1, 79, 76, 72, 76, 74, 71, 74, 79, 76, 72, 74, 76, 81, 79, 77, 76, 74, 76, 79, -1, 76, -1, 69, -1 };
            var rnd = new System.Random(5);
            for (int bar = 0; bar < bars; bar++)
            {
                int o = bar * per;
                for (int bt = 0; bt < 4; bt++)
                {
                    int bo = o + Mathf.RoundToInt(bt * beat * SR);
                    if (bt == 0 || bt == 2) Kick(a, bo, 1f);
                    if (bt == 1 || bt == 3) Snare(a, bo, .8f, rnd);
                    Hat(a, bo, .35f, rnd); Hat(a, bo + Mathf.RoundToInt(beat * SR * .5f), .5f, rnd);
                    // ベース：8分
                    for (int h = 0; h < 2; h++)
                    {
                        int so = bo + Mathf.RoundToInt(h * beat * SR * .5f);
                        int note = rootA[bar] + ((bt == 3 && h == 1) ? 12 : 0);
                        Bass(a, so, Hz(note), beat * .5f, .5f);
                    }
                    // アルペジオ：16分
                    for (int s = 0; s < 4; s++)
                    {
                        int so = bo + Mathf.RoundToInt(s * beat * SR * .25f);
                        int idx = (s * 2 + bt) % 3;
                        int note = triA[bar][idx] + 12 + ((s % 2 == 1) ? 12 : 0);
                        Pluck(a, so, Hz(note), .22f + (s == 0 ? .08f : 0f), beat * .6f);
                    }
                    // リード
                    int ln = lead[bar * 4 + bt];
                    if (ln > 0) Lead(a, bo, Hz(ln), beat * .92f, .16f);
                }
                foreach (var m in triA[bar]) Pad(a, o, Hz(m), per, .045f);
            }

            // 嵐：Am Am F E | Am Am Dm E ／ 4つ打ち＋歪んだベース＋不穏なスタブ
            int[] rootB = { 33, 33, 29, 28, 33, 33, 26, 28 };
            int[][] triB = { new[] { 57, 60, 64 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 52, 56, 59 }, new[] { 57, 60, 64 }, new[] { 57, 60, 64 }, new[] { 50, 53, 57 }, new[] { 52, 56, 59 } };
            for (int bar = 0; bar < bars; bar++)
            {
                int o = bar * per;
                for (int bt = 0; bt < 4; bt++)
                {
                    int bo = o + Mathf.RoundToInt(bt * beat * SR);
                    Kick(b, bo, 1.1f);
                    if (bt == 1 || bt == 3) Snare(b, bo, 1f, rnd);
                    Hat(b, bo + Mathf.RoundToInt(beat * SR * .5f), .6f, rnd);
                    for (int h = 0; h < 2; h++)
                    {
                        int so = bo + Mathf.RoundToInt(h * beat * SR * .5f);
                        Bass(b, so, Hz(rootB[bar] + (h == 1 && bt % 2 == 1 ? 12 : 0)), beat * .5f, .75f);
                    }
                    if (bt == 0 || (bt == 2 && bar % 2 == 1))
                        foreach (var m in triB[bar]) Stab(b, bo, Hz(m), beat * .8f, .07f);
                    int idx = bt % 3; Pluck(b, bo + Mathf.RoundToInt(beat * SR * .75f), Hz(triB[bar][idx] + 12), .18f, beat * .5f);
                }
                foreach (var m in triB[bar]) Pad(b, o, Hz(m - 12), per, .06f);
            }

            Finish(a, n, tail); Finish(b, n, tail);
            cMusA = Make("musA", a, n); cMusB = Make("musB", b, n);
        }

        static void Finish(float[] d, int n, int tail)
        {
            for (int i = 0; i < tail; i++) d[i] += d[n + i];          // 余韻を頭に折り返して継ぎ目なし
            float pk = 0; for (int i = 0; i < n; i++) pk = Mathf.Max(pk, Mathf.Abs(d[i]));
            float g = pk > 0 ? .85f / pk : 1f;
            for (int i = 0; i < n; i++) { float x = d[i] * g * 1.2f; d[i] = x / (1f + Mathf.Abs(x) * .6f) * 1.3f; }
        }

        static AudioClip Make(string name, float[] d, int n)
        {
            var o = new float[n]; System.Array.Copy(d, o, n);
            var c = AudioClip.Create(name, n, 1, SR, false); c.SetData(o, 0); return c;
        }

        static void Add(float[] d, int i, float v) { if (i >= 0 && i < d.Length) d[i] += v; }

        static void Kick(float[] d, int s, float vol)
        {
            int len = (int)(SR * .3f); double ph = 0;
            for (int i = 0; i < len; i++)
            {
                double t = (double)i / SR; double f = 48 + 110 * System.Math.Exp(-t * 28);
                ph += f / SR; Add(d, s + i, (float)(System.Math.Sin(ph * 6.2831853) * System.Math.Exp(-t * 11) * .95 * vol));
            }
        }

        static void Snare(float[] d, int s, float vol, System.Random r)
        {
            int len = (int)(SR * .22f); float lp = 0;
            for (int i = 0; i < len; i++)
            {
                double t = (double)i / SR; float nz = (float)(r.NextDouble() * 2 - 1); lp += (nz - lp) * .6f;
                float v = lp * (float)System.Math.Exp(-t * 20) * .7f + (float)(System.Math.Sin(6.2831853 * 190 * t) * System.Math.Exp(-t * 30) * .4);
                Add(d, s + i, v * vol);
            }
        }

        static void Hat(float[] d, int s, float vol, System.Random r)
        {
            int len = (int)(SR * .05f); float prev = 0;
            for (int i = 0; i < len; i++)
            {
                float nz = (float)(r.NextDouble() * 2 - 1); float hp = nz - prev; prev = nz;
                Add(d, s + i, hp * (float)System.Math.Exp(-(double)i / SR * 90) * .22f * vol);
            }
        }

        static void Bass(float[] d, int s, float f, float dur, float vol)
        {
            int len = (int)(SR * (dur + .06f)); double ph = 0; float lp = 0;
            for (int i = 0; i < len; i++)
            {
                double t = (double)i / SR; ph += f / SR; double fr = ph - System.Math.Floor(ph);
                float saw = (float)(2 * fr - 1); lp += (saw - lp) * Mathf.Lerp(.35f, .08f, (float)System.Math.Min(1, t * 8));
                float sub = (float)System.Math.Sin(ph * 6.2831853);
                float env = Mathf.Min(1, i / 80f) * (float)System.Math.Exp(-t * 4.5) * (i > len - 200 ? (len - i) / 200f : 1f);
                Add(d, s + i, (lp * .55f + sub * .55f) * env * vol * .5f);
            }
        }

        static void Pluck(float[] d, int s, float f, float vol, float dur)
        {
            int len = (int)(SR * (dur + .2f)); double ph = 0, ph2 = 0;
            for (int i = 0; i < len; i++)
            {
                double t = (double)i / SR; ph += f / SR; ph2 += f * 1.004 / SR;
                double a = 2 * (ph - System.Math.Floor(ph)) - 1, b = 4 * System.Math.Abs((ph2 - System.Math.Floor(ph2)) - .5) - 1;
                float env = Mathf.Min(1, i / 40f) * (float)System.Math.Exp(-t * 11);
                Add(d, s + i, (float)((a * .45 + b * .55) * env * vol * .45));
            }
        }

        static void Lead(float[] d, int s, float f, float dur, float vol)
        {
            int len = (int)(SR * (dur + .12f)); double ph = 0;
            for (int i = 0; i < len; i++)
            {
                double t = (double)i / SR; ph += (f * (1 + .004 * System.Math.Sin(6.2831853 * 5.5 * t))) / SR;
                double fr = ph - System.Math.Floor(ph); double sq = fr < .4 ? 1 : -1;
                float env = Mathf.Min(1, i / 120f) * (i > (int)(SR * dur) ? (float)System.Math.Max(0, 1 - (i - SR * dur) / (SR * .12)) : 1f) * (.75f + .25f * (float)System.Math.Exp(-t * 6));
                Add(d, s + i, (float)(sq * env * vol * .5));
            }
        }

        static void Stab(float[] d, int s, float f, float dur, float vol)
        {
            int len = (int)(SR * dur); double ph = 0, ph2 = 0;
            for (int i = 0; i < len; i++)
            {
                double t = (double)i / SR; ph += f / SR; ph2 += f * 1.007 / SR;
                double v = (2 * (ph - System.Math.Floor(ph)) - 1) + (2 * (ph2 - System.Math.Floor(ph2)) - 1);
                float env = Mathf.Min(1, i / 60f) * (float)System.Math.Exp(-t * 5);
                Add(d, s + i, (float)(v * env * vol * .5));
            }
        }

        static void Pad(float[] d, int s, float f, int len, float vol)
        {
            double ph = 0;
            for (int i = 0; i < len; i++)
            {
                double u = (double)i / len; ph += f / SR;
                float env = Mathf.Sin((float)(u * System.Math.PI));
                Add(d, s + i, (float)(System.Math.Sin(ph * 6.2831853) * env * vol));
            }
        }

        // ---------------- 再生 ----------------
        static void P(AudioClip c, float v = 1f) { if (I != null && On && c != null) I.sfx.PlayOneShot(c, v); }
        public static void Hit() { if (I) P(I.cHit); }
        public static void Pulse() { if (I) P(I.cPulse); }
        public static void Pop() { if (I) P(I.cPop); }
        public static void Tick() { if (I) P(I.cTick); }
        public static void Win() { if (I) P(I.cWin); }
        public static void Lose() { if (I) P(I.cLose); }
        public static void Warn() { if (I) P(I.cWarn, .8f); }
        public static void Gust() { if (I) P(I.cGust); }
        public static void Land() { if (I) P(I.cLand); }
        public static void SpinUp_() { if (I) P(I.cSpin, .9f); }

        // music: 0..1（タイトル.5/プレイ1/リザルト0）
        public static void Mix(float rotorLevel, float storm, float thr, float music = 0f)
        {
            if (I == null) return;
            float rv = On ? rotorLevel * (.16f + .12f * thr) : 0f;
            I.rotor.volume = rv; I.rotor.pitch = .78f + thr * .55f;
            I.servo.volume = On ? rotorLevel * (.015f + .05f * thr) : 0f; I.servo.pitch = .85f + thr * .6f;
            float wv = On ? storm * .35f : 0f;
            I.wind.volume = wv;
            if (rv > 0.001f && !I.rotor.isPlaying) { I.rotor.Play(); I.servo.Play(); }
            if (wv > 0.001f && !I.wind.isPlaying) I.wind.Play();
            I.musTarget = On ? music : 0f;
            I.musVol = Mathf.MoveTowards(I.musVol, I.musTarget, Time.unscaledDeltaTime * .6f);
            float mv = I.musVol * .42f;
            I.musA.volume = mv * (1f - storm); I.musB.volume = mv * storm;
            if (mv > 0.001f && !I.musA.isPlaying) { I.musA.Play(); I.musB.Play(); I.musB.timeSamples = 0; }
        }
    }
}
