using System.Collections.Generic;
using UnityEngine;

namespace Day111
{
    // ドローン配達便 3D版：横スクロールの配達ゲーム（ルールはHTML版の数値を基準、見た目と構成は再設計）
    public class DroneGame : MonoBehaviour
    {
        const float PX = Cfg.PX, G = Cfg.GROUND, LW = Cfg.LW;

        public GameObject pilotPrefab, receiverPrefab;
        public RuntimeAnimatorController pilotCtrl, receiverCtrl;

        enum Mode { Title, Intro, Play, End }
        class Dr { public float sx, sy, vx, vy, tilt, prop, pa, hp, bat, inv, cd, th, thv, L, stun, thr; }
        class Pulse { public float x, y, age, r; }
        class Warn { public float y, t; }
        class Fx { public SpriteRenderer sr; public float x, y, vx, vy, t, life, size; public Color col; public bool smoke, alive; }
        class Hur { public string state = "none"; public float t, nd; public int di, lastCyc = -1; public bool gw; public List<Vector2> q = new List<Vector2>(); }

        Camera cam; Light sun; Hud hud;
        Mode mode = Mode.Title;
        Dr d = new Dr();
        Hur hur = new Hur();
        List<Ob> OB = new List<Ob>();
        List<Pulse> pulses = new List<Pulse>();
        List<Warn> warns = new List<Warn>();
        List<Fx> fx = new List<Fx>();
        List<Vector3> warnWorld = new List<Vector3>();
        float t, mt, camX, camY, scrollV, playT, storm, flash, shake, upd, cflash, swingT, padT, endT, heartT, W = 960f, goalCam = Cfg.GOAL_CAM;
        float cargoHp, windX, windY, bannerShown;
        float deliverX, popP;
        bool touched, atGoal, hiSeen, crashed, resultOk, resultShown, paused, hasPlayed;
        int attempts, hits, shots;
        string resultReason = "";
        Vector2 fall; Vector2 fallV; float fallRot; bool falling; string cargoState = "ground";
        float intro1 = .9f, intro2 = 1.9f, intro3 = 3.0f, intro4 = 4.4f;

        // 入力
        float kbx, kby;
        Vector2 stick; bool padOn; int padId = -1; Vector2 padOrigin, padKnob; bool fireTouch;
        bool prevTap;
        bool bot, botDone; float simSpeed = 1f;

        // 見た目
        Transform droneRoot, cargoRoot, ropeT, worldRoot, fxRoot;
        Transform cargoTapeTop, cargoFlap; Transform[] cargoSpill; Renderer cargoBodyR; Material cargoBodyMat;
        Vector3 cargoTapeBaseScale; float cargoDmLerp;
        Transform[] rotors = new Transform[4];
        Renderer led;
        GameObject pilot, receiver; Animator pilotAnim, recvAnim; string pilotState = "", recvState = "";
        Transform padMark;
        List<SpriteRenderer> pulseSr = new List<SpriteRenderer>();
        Sprite circle, ring;
        Color baseSunCol, baseFog, baseAmbient; float baseSunInt, baseFogDensity;
        Vector3 camBase;

        // ================= 起動 =================
        void Awake()
        {
            Application.targetFrameRate = 60;
            Input.simulateMouseWithTouches = false;
            Application.runInBackground = false;
#if UNITY_EDITOR
            bot = PlayerPrefs.GetInt("d111bot", 0) == 1; simSpeed = Mathf.Max(1f, PlayerPrefs.GetFloat("d111speed", 1f));
#endif
            QualitySettings.shadows = ShadowQuality.HardOnly; QualitySettings.shadowDistance = 60f; QualitySettings.shadowCascades = 1;
            QualitySettings.antiAliasing = 0; QualitySettings.vSyncCount = 0; QualitySettings.skinWeights = SkinWeights.TwoBones; Application.targetFrameRate = 60;
            cam = Camera.main;
            if (cam == null) { var c = new GameObject("Main Camera"); cam = c.AddComponent<Camera>(); c.tag = "MainCamera"; }
            sun = FindFirstObjectByType<Light>();
            if (sun)
            {
                sun.shadows = LightShadows.Hard; sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
                baseSunCol = sun.color; baseSunInt = sun.intensity;
            }
            RenderSettings.skybox = null; RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 38f; RenderSettings.fogEndDistance = 190f; RenderSettings.fogColor = new Color(.74f, .73f, .74f);
            sky = new SkyBg(cam);
            baseFog = RenderSettings.fogColor; baseAmbient = RenderSettings.ambientLight;
            Hud.MakeSprites(); circle = Hud.Circle; ring = Hud.Ring;
            Snd.Boot(gameObject);
            hud = new Hud(transform);
            hud.ShowTouch = Input.touchSupported;
            worldRoot = new GameObject("World").transform; fxRoot = new GameObject("Fx").transform;
            BuildEnv(); BuildDrone(); BuildCharacters(); BuildFxPools();
            NewGame();
            SetMode(Mode.Title);
        }

        void OnApplicationFocus(bool f)
        {
#if !UNITY_EDITOR
            if (!f && (mode == Mode.Play || mode == Mode.Intro)) SetPause(true);
#endif
            if (!f) ClearInput();
        }

        void NewGame()
        {
            mt = 0; t = 0; camX = 0; camY = 0; scrollV = 0; playT = 0; storm = 0; flash = 0; shake = 0; upd = 0; cflash = 0; swingT = 0; padT = 0;
            d.sx = 360; d.sy = G - 20; d.vx = d.vy = 0; d.tilt = 0; d.prop = 0; d.hp = 100; d.bat = 100; d.inv = 0; d.cd = 0; d.th = 0; d.thv = 0; d.L = 131; d.stun = 0; d.thr = .4f;
            cargoHp = 100; cargoState = "ground"; touched = false; atGoal = false; hiSeen = false; crashed = false; resultShown = false; falling = false;
            hits = 0; shots = 0; pulses.Clear(); warns.Clear(); hur = new Hur(); windX = windY = 0; deliverX = Cfg.PAD_X;
            foreach (var f in fx) { f.alive = false; f.sr.gameObject.SetActive(false); }
            BuildCourse();
        }

        void SetMode(Mode m)
        {
            mode = m; mt = 0;
            hud.ShowTitle(m == Mode.Title);
            hud.SetHudVisible(m == Mode.Play || m == Mode.Intro || m == Mode.End);
            if (m != Mode.End) hud.ShowResult(false);
        }

        void StartFromTitle() { if (mode != Mode.Title) return; hasPlayed = true; StartRun(false); }

        void StartRun(bool skip)
        {
            NewGame();
            if (skip)
            {
                d.sx = 230; d.sy = 250; d.L = Cfg.ROPE; d.prop = 1; cargoState = "hang"; scrollV = Cfg.SCROLL * .6f;
                SetMode(Mode.Play); playT = 0;
            }
            else { SetMode(Mode.Intro); Snd.SpinUp_(); }
        }

        void Retry() { attempts++; StartRun(true); }

        void SetPause(bool p)
        {
            if (p == paused) return;
            paused = p; hud.ShowPause(p); if (p) ClearInput();
        }

        void ClearInput() { kbx = kby = 0; stick = Vector2.zero; padOn = false; padId = -1; fireTouch = false; }

        // ================= コース =================
        Ob A(Ob o) { o.alive = true; o.i = OB.Count; OB.Add(o); return o; }

        void BuildCourse()
        {
            foreach (var o in OB) if (o.go) Destroy(o.go);
            OB.Clear();
            float[][] hs = { new[] { 560f, 150, 118, 1, 0 }, new[] { 1230f, 140, 112, 0, 1 }, new[] { 1980f, 160, 124, 1, 2 }, new[] { 2480f, 150, 116, 0, 0 }, new[] { 3020f, 170, 126, 1, 1 }, new[] { 3600f, 150, 114, 0, 2 } };
            foreach (var a in hs) A(new Ob { type = "house", x = a[0], w = a[1], h = a[2], ch = (int)a[3], col = (int)a[4] });
            A(new Ob { type = "wire", x = 900, w = 420, ys = new[] { 140f, 320f } });
            A(new Ob { type = "balloon", x = 1700, y = 300, c = 0, ph = 0 }); A(new Ob { type = "balloon", x = 1790, y = 215, c = 1, ph = 2 }); A(new Ob { type = "balloon", x = 1880, y = 335, c = 2, ph = 4 });
            A(new Ob { type = "tree", x = 2260 });
            A(new Ob { type = "trash", x = 2900, y = 250, k = 0 }); A(new Ob { type = "trash", x = 3150, y = 330, k = 1 });
            A(new Ob { type = "sprinkler", x = 3400, ph = 0 });
            A(new Ob { type = "building", x = 3900, w = 210, h = 215, ex = new[] { new[] { 40f, 60, 26 }, new[] { 130f, 40, 18 } } });
            A(new Ob { type = "sign", x = 4110, y = 215, w = 120, h = 60 });
            A(new Ob { type = "car", x = 4400, w = 96, h = 40, k = 0, sp = -130 });
            A(new Ob { type = "building", x = 4650, w = 180, h = 210, ex = new[] { new[] { 30f, 50, 22 } } });
            A(new Ob { type = "crow", x = 4740, y = G - 210 - 14 });
            float[][] cells = { new[] { 1500f, -40 }, new[] { 3100f, -110 }, new[] { 4500f, -150 }, new[] { 6000f, -70 }, new[] { 7300f, -190 }, new[] { 9000f, -120 }, new[] { 10200f, -60 }, new[] { 11100f, -170 } };
            for (int i = 0; i < cells.Length; i++) A(new Ob { type = "cell", x = cells[i][0], y = cells[i][1], ph = i });
            A(new Ob { type = "wire", x = 4260, w = 420, ys = new[] { -95f } }); A(new Ob { type = "wire", x = 7000, w = 360, ys = new[] { -20f, -170f } });
            A(new Ob { type = "monkey", x = 3650, ph = 0 }); A(new Ob { type = "monkey", x = 5880, ph = 1 }); A(new Ob { type = "monkey", x = 6540, ph = 2 });
            A(new Ob { type = "building", x = 5300, w = 200, h = 205, ex = new[] { new[] { 100f, 60, 24 } } });
            for (int i = 0; i < 6; i++) A(new Ob { type = "pigeon", x = 5380 + i * 14, y = G - 205 - 9, ph = i, k = 1, t = i * .07f });
            A(new Ob { type = "building", x = 5560, w = 170, h = 215, ex = new[] { new[] { 20f, 50, 22 } } });
            A(new Ob { type = "building", x = 5790, w = 190, h = 200, ex = new[] { new[] { 60f, 40, 40 } } });
            A(new Ob { type = "odrone", x = 6380, yc = 250, amp = 95, T = 4.6f });
            A(new Ob { type = "building", x = 6450, w = 200, h = 205, ex = new[] { new[] { 130f, 50, 22 } } });
            A(new Ob { type = "vent", x = 6540, y = G - 205, ph = 0 });
            A(new Ob { type = "crane", x = 7050, ph = 0, lb = 265, tr = 0 });
            A(new Ob { type = "balloon", x = 7900, y = 235, c = 1, ph = 1 }); A(new Ob { type = "balloon", x = 8090, y = 320, c = 0, ph = 3 });
            A(new Ob { type = "building", x = 7985, w = 150, h = 200, ex = new float[0][] });
            A(new Ob { type = "building", x = 8450, w = 270, h = 195, ex = new[] { new[] { 40f, 60, 24 }, new[] { 160f, 80, 30 } }, fac = true });
            A(new Ob { type = "steam", x = 8980, ph = 0 });
            A(new Ob { type = "viaduct", x = 9400, w = 700 });
            A(new Ob { type = "car", x = 9600, w = 100, h = 46, k = 1, sp = -110 }); A(new Ob { type = "car", x = 9880, w = 84, h = 40, k = 0, col = 1, sp = -150 });
            for (int i = 0; i < 14; i++)
            {
                int k = i % 4 == 3 ? 1 : 0;
                A(new Ob { type = "car", x = 900 + i * 880 + (i * 137 % 300), w = k == 1 ? 100 : 90, h = k == 1 ? 46 : 40, k = k, col = i % 6 + 1, sp = -(100 + (i * 31) % 60) });
            }
            A(new Ob { type = "building", x = 10350, w = 290, h = 200, ex = new[] { new[] { 30f, 70, 26 }, new[] { 200f, 50, 20 } }, fac = true });
            A(new Ob { type = "building", x = 10780, w = 230, h = 190, ex = new[] { new[] { 90f, 50, 22 } }, fac = true });
            float[][] eh = { new[] { 11450f, 150, 118, 1, 2 }, new[] { 11900f, 160, 124, 0, 1 }, new[] { 12350f, 150, 112, 1, 0 }, new[] { 12800f, 150, 120, 0, 2 } };
            foreach (var a in eh) A(new Ob { type = "house", x = a[0], w = a[1], h = a[2], ch = (int)a[3], col = (int)a[4] });
            foreach (var o in OB) o.SetBounds();
        }

        // ================= 入力 =================
        bool TapDown()
        {
            bool down = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton0) || Pad.AnyDown();
            foreach (var tc in Input.touches) if (tc.phase == TouchPhase.Began) down = true;
            return down;
        }

        void ReadInput(float dt, out float dx, out float dy, out bool fire)
        {
            float tx = 0, ty = 0;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) tx -= 1;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) tx += 1;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) ty -= 1;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) ty += 1;
            kbx += (tx - kbx) * Mathf.Min(1f, dt * (tx != 0 ? 12f : 18f));
            kby += (ty - kby) * Mathf.Min(1f, dt * (ty != 0 ? 12f : 18f));
            float gx = 0, gy = 0;
            try { gx = Input.GetAxisRaw("Horizontal"); gy = -Input.GetAxisRaw("Vertical"); } catch { }
            ReadTouch();
            Vector2 pm = Pad.Move();
            dx = Mathf.Clamp(kbx + stick.x + (Mathf.Abs(gx) > .2f ? gx : 0) + pm.x, -1, 1);
            dy = Mathf.Clamp(kby + stick.y + (Mathf.Abs(gy) > .2f ? gy : 0) + pm.y, -1, 1);
            fire = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.JoystickButton0) || Pad.Fire() || fireTouch;
        }

        void ReadTouch()
        {
            fireTouch = false; Vector2 fc = hud.FireCenterScreen(); float fr = hud.FireRadiusScreen() * 1.35f;
            bool found = false;
            foreach (var tc in Input.touches)
            {
                Vector2 p = tc.position;
                if ((p - fc).sqrMagnitude < fr * fr && tc.fingerId != padId) { fireTouch = true; continue; }
                if (padId < 0 && tc.phase == TouchPhase.Began && p.x < Screen.width * .6f) { padId = tc.fingerId; padOrigin = p; }
                if (tc.fingerId == padId)
                {
                    found = true;
                    if (tc.phase == TouchPhase.Ended || tc.phase == TouchPhase.Canceled) { found = false; break; }
                    float rad = Screen.height * .11f;
                    Vector2 v = (p - padOrigin) / rad;
                    if (v.magnitude > 1f) v = v.normalized;
                    stick = new Vector2(Mathf.Abs(v.x) < .07f ? 0 : v.x, Mathf.Abs(v.y) < .07f ? 0 : -v.y);
                    padKnob = padOrigin + v * rad;
                }
            }
            if (!found) { padId = -1; stick = Vector2.zero; }
            padOn = padId >= 0;
            if (hud.ShowTouch) hud.Touch(padOn, padOrigin, padKnob, fireTouch);
        }

        bool PauseTouch()
        {
            foreach (var tc in Input.touches) if (tc.phase == TouchPhase.Began && hud.PauseRectScreen().Contains(tc.position)) return true;
            if (Input.GetMouseButtonDown(0) && hud.PauseRectScreen().Contains(Input.mousePosition)) return true;
            return false;
        }

        // ================= 更新 =================
        float acc;
        void Update()
        {
            float dtf = Time.unscaledDeltaTime;
            bool portrait = Screen.height > Screen.width;
            hud.ShowRotate(portrait);
            if (portrait) { ClearInput(); return; }
            bool tap = TapDown();
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape) || Pad.PauseDown() || (mode != Mode.Title && mode != Mode.End && PauseTouch())) { if (mode == Mode.Play || mode == Mode.Intro) SetPause(!paused); }
            else if (paused && tap) SetPause(false);
            else if (mode == Mode.Title && tap && mt > .3f) { Snd.Boot(gameObject); StartFromTitle(); }
            else if (mode == Mode.End && resultShown && tap && endT > 1f) { hud.ShowResult(false); Retry(); }

            if (bot)
            {
                if (mode == Mode.Title && mt > 1f) StartFromTitle();
                if (mode == Mode.End && resultShown && endT > 2f && !botDone) { botDone = true; WriteBotLog(); }
            }
            if (!paused)
            {
                acc += Mathf.Min(dtf * simSpeed, .1f * simSpeed);
                int n = 0;
                while (acc >= 1f / 60f && n < 24) { Step(1f / 60f); acc -= 1f / 60f; n++; }
            }
            Render(dtf);
        }

        SkyBg sky;

        void Step(float dt)
        {
            t += dt; mt += dt;
            float dx, dy; bool fire;
            ReadInput(dt, out dx, out dy, out fire);
            if (bot && mode == Mode.Play) BotInput(ref dx, ref dy, ref fire);
            W = Mathf.Clamp(Cfg.H * cam.aspect, 720f, 1300f);
            goalCam = Mathf.Max(Cfg.GOAL_CAM, Cfg.CUST_X + 70f - W);
            if (mode == Mode.Title) StepTitle(dt);
            else if (mode == Mode.Intro) StepIntro(dt);
            else if (mode == Mode.Play) StepPlay(dt, dx, dy, fire);
            else StepEnd(dt);
            StepFx(dt);
            d.pa += dt * (6 + d.prop * 60) * d.prop;
            Snd.Mix(mode == Mode.Title ? 0f : d.prop * (mode == Mode.End && !resultOk ? .4f : 1f), storm, d.thr, mode == Mode.Title ? .55f : mode == Mode.Intro ? .8f : mode == Mode.Play ? 1f : 0f);
        }

        void StepTitle(float dt)
        {
            camX = 380 + (mt * 70f) % 2800f; scrollV = 70;
            d.sx = W * .4f; d.sy = 232 + Mathf.Sin(t * 1.3f) * 9; d.L = Cfg.ROPE; d.prop = 1; d.th = .15f * Mathf.Sin(t * 1.7f); d.tilt = .06f + .03f * Mathf.Sin(t * 1.3f); d.inv = 0;
            cargoState = "hang";
        }

        void StepIntro(float dt)
        {
            float m = mt;
            SetAnim(true, m < intro3 ? "Idle" : "Wave");
            if (m < intro1) d.prop = Mathf.Clamp01(m / intro1);
            else if (m < intro2) { d.prop = 1; float u = Cfg.Ease((m - intro1) / (intro2 - intro1)); d.sx = Mathf.Lerp(360, 290, u); d.sy = Mathf.Lerp(G - 20, G - 165, u); }
            else if (m < intro3) { d.prop = 1; float u = Cfg.Ease((m - intro2) / (intro3 - intro2)); d.sx = 290; d.sy = G - 165; d.L = Mathf.Lerp(131, Cfg.ROPE, u); cargoState = "hang"; }
            else if (m < intro4)
            {
                float u = Cfg.Ease((m - intro3) / (intro4 - intro3)); d.L = Cfg.ROPE; d.sy = Mathf.Lerp(G - 165, 250, u); d.sx = Mathf.Lerp(290, 230, u);
                scrollV = Cfg.SCROLL * u * .6f; camX += scrollV * dt; d.tilt = .12f * u;
            }
            else { SetMode(Mode.Play); playT = 0; }
            d.tilt = Mathf.Lerp(d.tilt, 0, .02f);
        }

        float[] PlayerRects(out float cx, out float cy, out float[] rd, out float[] rc)
        {
            float wx = camX + d.sx, L = d.L + 12;
            cx = wx + Mathf.Sin(d.th) * L; cy = d.sy + 10 + Mathf.Cos(d.th) * L;
            rd = new[] { wx - 25, d.sy - 14, 50, 24 }; rc = new[] { cx - 14, cy - 12, 28, 24 };
            return rd;
        }

        static bool Hit(float[] a, float[] b) { return a[0] < b[0] + b[2] && a[0] + a[2] > b[0] && a[1] < b[1] + b[3] && a[1] + a[3] > b[1]; }


        // ---- テスト用の自動操縦（エディタのみ）----
        void BotInput(ref float dx, ref float dy, ref bool fire)
        {
            float wx = camX + d.sx; float L = d.L + 12;
            if (atGoal)
            {
                float cx = wx + Mathf.Sin(d.th) * L; float err = Cfg.PAD_X - cx;
                dx = Mathf.Clamp(err / 60f, -1f, 1f) * (Mathf.Abs(err) < 25 ? .4f : 1f);
                float symaxx = G - 22 - L * Mathf.Cos(d.th);
                bool near = Mathf.Abs(err) < 28 && Mathf.Abs(d.vx) < 45 && Mathf.Abs(d.th) < .3f;
                float tgtY = near ? symaxx : Mathf.Min(symaxx - 60, 250f);
                dy = Mathf.Clamp((tgtY - d.sy) / 50f, -.6f, .5f);
                fire = false; return;
            }
            float best = d.sy, bestCost = 1e9f;
            for (float y = -180; y <= 330; y += 15)
            {
                float cost = Mathf.Abs(y - 220) * .02f + Mathf.Abs(y - d.sy) * .012f + (y < 50 ? (50 - y) * .08f : 0f);
                for (float a = 0; a <= 280 && cost < 900; a += 20)
                {
                    float[] rd = { wx + a - 38, y - 22, 76, 40 }, rc = { wx + a - 26, y + 10 + L - 20, 52, 40 };
                    foreach (var o in OB)
                    {
                        if (!o.alive || o.type == "cell" || o.type == "vent") continue;
                        if (IsStaticCull(o.type) && (o.x1 < wx - 100 || o.x0 > wx + 400)) continue;
                        var rs = o.Rects(); if (rs == null) continue;
                        foreach (var r in rs) if (Hit(rd, r) || Hit(rc, r)) { cost += 1000; break; }
                        if (cost >= 900) break;
                    }
                }
                if (cost < bestCost) { bestCost = cost; best = y; }
            }
            dy = Mathf.Clamp((best - d.sy) / 30f, -1f, 1f);
            dx = Mathf.Clamp((235f - d.sx) / 120f, -.5f, .5f);
            fire = false;
            foreach (var o in OB)
            {
                if (!o.alive || !Ob.Push.Contains(o.type)) continue;
                float ox = o.px != 0 ? o.px : o.x, oy = o.py != 0 ? o.py : o.y;
                if (ox > wx && ox < wx + 300 && Mathf.Abs(oy - d.sy) < 45) { fire = true; break; }
            }
        }

        void WriteBotLog()
        {
#if UNITY_EDITOR
            try
            {
                string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../Verification"));
                System.IO.Directory.CreateDirectory(dir);
                string line = System.DateTime.Now.ToString("s") + " ok=" + resultOk + " reason=" + resultReason + " time=" + playT.ToString("0.0") + " drone=" + Mathf.RoundToInt(d.hp) + " cargo=" + Mathf.RoundToInt(cargoHp) + " battery=" + Mathf.RoundToInt(d.bat) + " hits=" + hits + " shots=" + shots + " camX=" + Mathf.RoundToInt(camX) + "\n";
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "bot-run.txt"), line);
            }
            catch { }
#endif
        }

        void StepPlay(float dt, float dx, float dy, bool fire)
        {
            playT += dt;
            float target = Cfg.SCROLL; if (camX > goalCam - 360) target = Cfg.SCROLL * Mathf.Clamp((goalCam - camX) / 360f, .18f, 1f);
            scrollV = Mathf.Lerp(scrollV, target, Mathf.Min(1, dt * 3));
            camX = Mathf.Min(goalCam, camX + scrollV * dt);
            atGoal = camX >= goalCam - .5f; if (atGoal) scrollV = 0;
            StepHurricane(dt);
            camY = Mathf.Lerp(camY, Mathf.Clamp(140 - d.sy, 0, 400), Mathf.Min(1, dt * 5));
            float hi = Mathf.Clamp((30 - d.sy) / 60f, 0, 1);
            if (hi > 0) { windX -= 120 * hi; if (!hiSeen && d.sy < 10) { hiSeen = true; hud.Banner("高度注意：向かい風＆バッテリー消耗", 3f, new Color(.35f, .82f, 1f)); } }
            d.stun = Mathf.Max(0, d.stun - dt); cflash = Mathf.Max(0, cflash - dt);
            d.bat -= (.62f + 1.1f * hi) * dt;
            if (d.bat <= 0) { d.bat = 0; Finish(false, "バッテリー切れ"); return; }
            d.prop = 1; d.cd = Mathf.Max(0, d.cd - dt); d.inv = Mathf.Max(0, d.inv - dt);
            float sp = (atGoal ? .62f : 1f) * (d.stun > 0 ? .25f : 1f), tvx = dx * 205 * sp, tvy = dy * 190 * sp, k = 9f;
            float ovx = d.vx;
            d.vx += ((tvx - d.vx) * k + windX) * dt;
            d.vy += ((tvy - d.vy) * k + windY + upd) * dt;
            float ax = (d.vx - ovx) / dt;
            d.thr = Mathf.Lerp(d.thr, Mathf.Clamp(.32f + .42f * Mathf.Sqrt(Sq(d.vx / 205f) + Sq(d.vy / 190f)) + .26f * Mathf.Clamp(-d.vy / 190f, 0, 1), 0, 1), Mathf.Min(1, dt * 6));
            if (d.hp < 40 && Random.value < dt * (8 + (40 - d.hp) * .3f)) Puff(camX + d.sx + (Random.value < .5f ? -26 : 26), d.sy - 10, new Color(.2f, .2f, .22f, .6f), 1, 22, 1.1f, 9, true);
            d.sx += d.vx * dt; if (d.sx < 50) { d.sx = 50; d.vx = Mathf.Max(0, d.vx); } if (d.sx > W - 100) { d.sx = W - 100; d.vx = Mathf.Min(0, d.vx); }
            d.sy += d.vy * dt; if (d.sy < -260) { d.sy = -260; d.vy = Mathf.Max(0, d.vy); }
            float cth = Mathf.Cos(d.th), symax = G - 22 - (d.L + 12) * cth;
            if (d.sy > symax)
            {
                float vyHit = d.vy; d.sy = symax; d.vy = Mathf.Min(0, d.vy);
                if (!touched && vyHit > 0) { touched = true; if (atGoal) { if (vyHit > 150) { cargoHp -= 8; shake = .25f; Snd.Hit(); } else Snd.Land(); } }
            }
            else touched = false;
            d.tilt = Mathf.Lerp(d.tilt, Mathf.Clamp(d.vx / 205f, -1, 1) * .22f + Mathf.Clamp(ax / 2500f, -.15f, .15f), Mathf.Min(1, dt * 8));
            float acc = -(Cfg.GRAV / d.L) * Mathf.Sin(d.th) - (ax / d.L) * Mathf.Cos(d.th) + (windX * 2f / d.L) * Mathf.Cos(d.th) - (1.5f + ((Mathf.Abs(dx) + Mathf.Abs(dy)) < .06f ? 1.8f : 0f)) * d.thv;
            d.thv += acc * dt; d.th += d.thv * dt; d.th = Mathf.Clamp(d.th, -1.4f, 1.4f);
            if (Mathf.Abs(d.th) > 1.15f) { swingT += dt; if (swingT > .15f) { cargoHp -= 9 * dt; cflash = Mathf.Max(cflash, .12f); } } else swingT = Mathf.Max(0, swingT - dt);
            if (fire && d.cd <= 0 && d.bat > 2 && !atGoal) FirePulse();
            for (int i = pulses.Count - 1; i >= 0; i--) { var q = pulses[i]; q.age += dt; q.x += 520 * dt; q.r = 14 + q.age * 60; if (q.age > .38f) pulses.RemoveAt(i); }
            float cx, cy; float[] rd, rc; PlayerRects(out cx, out cy, out rd, out rc);
            float pwx = camX + d.sx, pwy = d.sy;
            for (int i = 0; i < OB.Count; i++) { var o = OB[i]; if (o.alive) UpdOb(o, dt, pwx, pwy, cx, cy); }
            // パルス判定
            foreach (var q in pulses)
                foreach (var o in OB)
                {
                    if (!o.alive || !Ob.Push.Contains(o.type)) continue;
                    var rs = o.Rects(); if (rs == null) continue; var r = rs[0];
                    float qx = Mathf.Clamp(q.x, r[0], r[0] + r[2]), qy = Mathf.Clamp(q.y, r[1], r[1] + r[3]);
                    if (Mathf.Sqrt(Sq(q.x - qx) + Sq(q.y - qy)) > q.r) continue;
                    if (o.type == "balloon") { o.alive = false; Snd.Pop(); AddPop("パン！", o.px, o.py - 18, new Color(1f, .95f, .65f)); Puff(o.px, o.py, BalloonCol(o.c), 14, 140, .6f, 8, false); }
                    else if (o.type == "trash") { o.mode = "blown"; o.bt = 0; o.vx = 380; o.vy = -120; Snd.Pop(); AddPop("ポイッ！", o.px, o.py - 18, new Color(.75f, .92f, 1f)); }
                    else if (o.type == "monkey" && o.st != "flee") { o.st = "flee"; o.vx = 150; o.vy = -250; o.mt = 0; Snd.Pop(); AddPop("キーッ！", o.x, o.y - 40, new Color(1f, .85f, .63f)); }
                    else if (o.type == "debris") { o.alive = false; Snd.Pop(); Puff(o.x, o.y, new Color(.85f, .936f, .62f), 10, 140, .6f, 8, false); }
                    else if (o.type == "crow" && o.mode != "flee") { o.mode = "flee"; o.mt = 0; o.vx = 260; o.vy = -150; Snd.Pop(); AddPop("カァ！", o.px, o.py - 20, new Color(.9f, .88f, 1f)); Puff(o.px, o.py, new Color(.1f, .1f, .1f), 6, 90, .6f, 6, false); }
                    else if (o.type == "pigeon" && o.mode != "scatter") { o.mode = "scatter"; o.mt = 0; o.vx = 220 + Random.value * 80; o.vy = -130 - Random.value * 80; if (playT - popP > .6f) { popP = playT; AddPop("バサバサ！", o.px, o.py - 20, new Color(.9f, .88f, 1f)); } }
                }
            // 衝突
            upd = 0;
            for (int i = 0; i < OB.Count; i++)
            {
                var o = OB[i]; if (!o.alive) continue;
                if (IsStaticCull(o.type) && (o.x1 < camX - 60 || o.x0 > camX + LW + 60)) continue;
                if (o.type == "cell")
                {
                    var cr = new[] { o.x - 16, o.y - 14, 32, 28 };
                    if (Hit(rd, cr) || Hit(rc, cr)) { o.alive = false; d.bat = Mathf.Min(100, d.bat + 21); Snd.Tick(); AddPop("+21 バッテリー", o.x, o.y - 24, new Color(.55f, 1f, .75f)); Puff(o.x, o.y, new Color(.55f, 1f, .75f), 10, 110, .5f, 6, false); }
                    continue;
                }
                if (o.type == "vent") { if (o.phs == "burst" && Hit(rd, new[] { o.x - 36, 40, 72, o.y - 40 })) upd = -1500; continue; }
                var rs = o.Rects(); if (rs == null) continue;
                if (d.inv > 0) continue;
                foreach (var r in rs) { if (Hit(rd, r)) { Hurt('d', o, r); break; } if (Hit(rc, r)) { Hurt('c', o, r); break; } }
            }
            if (atGoal)
            {
                PlayerRects(out cx, out cy, out rd, out rc);
                float cb = cy + 12; bool slow = Mathf.Abs(d.vx) < 70 && Mathf.Abs(d.vy) < 60 && Mathf.Abs(d.th) < .4f;
                if (cb >= G - 3 && Mathf.Abs(cx - Cfg.PAD_X) < 44 && slow) padT += dt; else padT = Mathf.Max(0, padT - dt * 2);
                if (padT >= .7f) { deliverX = cx; Finish(true, ""); return; }
            }
            if (d.hp <= 0) { d.hp = 0; Finish(false, "ドローンが墜落した"); return; }
            if (cargoHp <= 0) { cargoHp = 0; Finish(false, "荷物が壊れてしまった"); return; }
        }

        static float Sq(float v) { return v * v; }
        static bool IsStaticCull(string ty) { return ty == "house" || ty == "building" || ty == "wire" || ty == "tree" || ty == "sign" || ty == "viaduct" || ty == "car" || ty == "crane" || ty == "steam" || ty == "sprinkler"; }

        void FirePulse()
        {
            pulses.Add(new Pulse { x = camX + d.sx + 30, y = d.sy + 1 });
            Puff(camX + d.sx + 26, d.sy + 1, new Color(.92f, .98f, 1f, .7f), 7, 90, .35f, 8, false);
            d.cd = .45f; d.bat -= .45f; shots++; Snd.Pulse();
        }

        static Color BalloonCol(int c) { return c == 0 ? new Color(1f, .35f, .3f) : c == 1 ? new Color(.3f, .7f, 1f) : new Color(1f, .82f, .25f); }

        void Hurt(char kind, Ob o, float[] r)
        {
            if (d.inv > 0) return;
            float dmg = Ob.Dmg(o.type);
            float cx, cy; float[] rd, rc; PlayerRects(out cx, out cy, out rd, out rc);
            if (kind == 'd') d.hp -= dmg; else { cargoHp -= dmg * .9f; cflash = .55f; Puff(cx, cy, new Color(.8f, .64f, .42f), 14, 120, .5f, 6, false); AddPop("荷物 -" + Mathf.RoundToInt(dmg * .9f) + "%", cx, cy + 26, new Color(1f, .6f, .55f)); }
            float px = kind == 'd' ? camX + d.sx : cx, py = kind == 'd' ? d.sy : cy;
            float ox = r != null ? r[0] + r[2] / 2 : (o.px != 0 ? o.px : o.x), oy = r != null ? r[1] + r[3] / 2 : (o.py != 0 ? o.py : o.y);
            float kx = px - ox, ky = py - oy; float m = Mathf.Sqrt(kx * kx + ky * ky); if (m < .001f) m = 1; kx /= m; ky /= m;
            if (r != null && r[2] > 120) kx = Mathf.Min(kx, -.3f);
            float f = o.type == "monkey" ? 300 : 380;
            d.vx = Mathf.Clamp(d.vx * .3f + kx * f - 40, -460, 400); d.vy = Mathf.Clamp(d.vy * .3f + ky * f * .8f, -330, 330); d.stun = .4f; d.th += kx * .35f;
            d.inv = 1.2f; hits++; shake = .35f; Snd.Hit();
            AddPop("ガツン！", px, py - 24, new Color(1f, .6f, .42f)); Puff(px, py, new Color(1f, .82f, .25f), 12, 160, .5f, 5, false);
            if (o.type == "monkey") { o.st = "flee"; o.vx = -70; o.vy = -230; o.mt = 0; }
            else if (o.type == "crow") { o.mode = "flee"; o.mt = 0; o.vx = -120; o.vy = -170; }
            else if (Ob.Push.Contains(o.type)) o.alive = false;
        }

        float RoofTop(float x)
        {
            float y = G;
            foreach (var o in OB)
            {
                if (o.type == "house" && x >= o.x && x <= o.x + o.w) { float u = Mathf.Min(x - o.x, o.x + o.w - x) / o.w, pk = G - o.h, ev = G - o.h * .62f; float tt = u < .14f ? ev + (pk - ev) * (u / .14f) : pk; y = Mathf.Min(y, tt); }
                else if (o.type == "building" && x >= o.x && x <= o.x + o.w) y = Mathf.Min(y, G - o.h);
            }
            return y;
        }

        void AddWarn(float y) { warns.Add(new Warn { y = Mathf.Clamp(y, -300, 440), t = 1.8f }); Snd.Warn(); }

        void UpdOb(Ob o, float dt, float pwx, float pwy, float pcx, float pcy)
        {
            float cam0 = camX, T = playT;
            if (Ob.WarnOn.Contains(o.type) && !o.warned && o.x - cam0 < LW + 60 && o.x - cam0 > -200 && o.type != "vent" && o.type != "steam" && o.type != "sprinkler")
            {
                o.warned = true;
                float wy = o.type == "crow" || o.type == "pigeon" || o.type == "trash" ? o.y : o.type == "odrone" ? o.yc : 180;
                if (o.type != "pigeon" || o.i == FirstPigeon()) AddWarn(wy);
            }
            switch (o.type)
            {
                case "balloon": o.px = o.x + Mathf.Sin(T * 1.1f + o.ph) * 9; o.py = o.y + Mathf.Sin(T * 1.5f + o.ph * 1.7f) * 12; break;
                case "trash":
                    if (o.mode == null) { o.mode = "idle"; o.px = o.x; o.py = o.y; o.rot = 0; }
                    if (o.mode == "idle" && o.x - cam0 < LW + 40) { o.mode = "drift"; o.vx = -75; o.vy = 0; }
                    if (o.mode == "drift") { o.vy += Mathf.Clamp((pwy - o.py) * 1.1f, -70, 70) * dt * 3; o.vy = Mathf.Clamp(o.vy, -65, 65); o.px += o.vx * dt; o.py += (o.vy + Mathf.Sin(T * 5 + o.i) * 25) * dt; o.rot += dt * 4; if (o.px < cam0 - 100) o.alive = false; }
                    else if (o.mode == "blown") { o.px += o.vx * dt; o.py += o.vy * dt; o.rot += dt * 9; o.bt += dt; if (o.bt > 1.4f) o.alive = false; }
                    break;
                case "crow":
                    if (o.mode == null) { o.mode = "perch"; o.px = o.x; o.py = o.y; o.mt = 0; o.hd = Mathf.PI; }
                    if (o.mode == "perch" && o.px - cam0 < LW - 80) { o.mode = "alert"; o.mt = 0; Snd.Warn(); }
                    else if (o.mode == "alert") { o.mt += dt; o.py -= 16 * dt; if (o.mt > .8f) { o.mode = "chase"; o.mt = 0; } }
                    else if (o.mode == "chase")
                    {
                        o.mt += dt; float ang = Mathf.Atan2(pwy - o.py, pwx - o.px); float df = ang - o.hd;
                        while (df > Mathf.PI) df -= 2 * Mathf.PI; while (df < -Mathf.PI) df += 2 * Mathf.PI;
                        o.hd += Mathf.Clamp(df, -2f * dt, 2f * dt); o.px += Mathf.Cos(o.hd) * 205 * dt; o.py += Mathf.Sin(o.hd) * 205 * dt;
                        if (o.px < cam0 - 150 || o.mt > 7) o.alive = false;
                    }
                    else if (o.mode == "flee") { o.mt += dt; o.px += o.vx * dt; o.py += o.vy * dt; if (o.mt > 3) o.alive = false; }
                    break;
                case "pigeon":
                    if (o.mode == null) { o.mode = "perch"; o.px = o.x; o.py = o.y; o.mt = 0; }
                    if (o.mode == "perch" && o.px - cam0 < LW - 120) { o.mode = "warn"; o.mt = -o.t; }
                    else if (o.mode == "warn") { o.mt += dt; if (o.mt > .8f) { o.mode = "fly"; o.mt = 0; o.y0 = o.py; } }
                    else if (o.mode == "fly") { o.mt += dt; o.px -= (205 + o.ph * 8) * dt; o.py = o.y0 - o.mt * 24 * (1 + o.ph * .15f) + Mathf.Sin(o.mt * 5 + o.ph * 1.3f) * 26 + (o.ph - 2.5f) * 20 * Mathf.Min(1, o.mt); if (o.px < cam0 - 100) o.alive = false; }
                    else if (o.mode == "scatter") { o.mt += dt; o.px += o.vx * dt; o.py += o.vy * dt; if (o.mt > 2) o.alive = false; }
                    break;
                case "monkey":
                    {
                        const float GG = 900f;
                        if (o.st == null) { o.st = "sit"; o.mt = 0; o.vx = 0; o.vy = 0; o.face = -1; o.rot = 0; o.y = RoofTop(o.x); }
                        o.mt += dt; float sx = o.x - cam0;
                        if (sx < -160 || o.y > G + 60) { o.alive = false; break; }
                        if (o.st == "sit")
                        {
                            o.y = RoofTop(o.x);
                            if (sx < LW - 50 && o.mt > .5f)
                            {
                                float ddx = pcx - o.x, ddy = o.y - pcy, TT = .55f, lx = (scrollV + d.vx) * TT, ly = d.vy * TT;
                                if (Mathf.Abs(ddx) < 170 && ddy > -10 && ddy < 175) { o.vx = (ddx + lx) / TT; o.vy = (pcy + ly - o.y - .5f * GG * TT * TT) / TT; o.st = "leap"; o.mt = 0; o.face = ddx < 0 ? -1 : 1; AddPop("キィ！", o.x, o.y - 42, new Color(1f, .85f, .63f)); }
                                else if (ddy < 260) { float dir = ddx < 0 ? -1 : 1, tx = o.x + dir * 70; if (RoofTop(tx) < G - 60 && Mathf.Abs(ddx) > 40) { o.vx = dir * 115; o.vy = -270; o.st = "hop"; o.mt = 0; o.face = dir; } else o.mt = .1f; }
                            }
                        }
                        else if (o.st == "hop" || o.st == "leap")
                        {
                            o.vy += GG * dt; o.x += o.vx * dt; o.y += o.vy * dt;
                            float rt = RoofTop(o.x);
                            if (o.vy > 0 && o.y >= rt) { string was = o.st; o.y = rt; o.st = "sit"; o.mt = was == "leap" ? -.8f : .2f; o.vx = 0; o.vy = 0; }
                            if (o.st == "leap" && o.mt > 1.1f) { o.st = "sit"; o.mt = -.3f; }
                        }
                        else if (o.st == "flee") { o.vy += GG * dt; o.x += o.vx * dt; o.y += o.vy * dt; o.rot += dt * 9; if (o.mt > 2) o.alive = false; }
                        break;
                    }
                case "car":
                    if (!o.init) { o.init = true; o.vx = 0; }
                    if (o.vx == 0 && o.x - cam0 < LW + 80) o.vx = o.sp != 0 ? o.sp : -120;
                    if (o.vx != 0) { o.x += o.vx * dt; o.x0 = o.x; o.x1 = o.x + o.w; if (o.x + o.w < cam0 - 120) o.alive = false; }
                    break;
                case "debris": o.x += o.vx * dt; o.y += Mathf.Sin(T * 3 + o.i) * 8 * dt; o.rot += dt * 5; if (o.x < cam0 - 100) o.alive = false; break;
                case "odrone":
                    if (!o.act && o.x - cam0 < LW + 120) { o.act = true; o.t0 = T; o.x0 = o.x; Snd.Warn(); }
                    if (o.act) { float tt = T - o.t0; o.px = o.x0 - 70 * tt; o.py = o.yc + o.amp * Mathf.Sin(6.2832f * tt / o.T); if (o.px < cam0 - 150) o.alive = false; }
                    break;
                case "crane": o.lb = 265 + 15 * Mathf.Sin(6.2832f * T / 7 + o.ph); o.tr = 90 * Mathf.Sin(6.2832f * T / 9 + o.ph); break;
                case "vent": case "steam": case "sprinkler":
                    {
                        float cyc = o.type == "vent" ? 4.0f : o.type == "steam" ? 3.6f : 4.2f;
                        float a = o.type == "vent" ? 1.6f : o.type == "steam" ? 1.3f : 1.6f, b = o.type == "vent" ? 2.5f : o.type == "steam" ? 2.1f : 2.3f, c = o.type == "sprinkler" ? 3.7f : cyc;
                        float ph = (T + o.ph) % cyc; string nph = ph < a ? "idle" : ph < b ? "warn" : ph < c ? "burst" : "idle";
                        if (nph != o.phs && o.x - cam0 < LW && o.x - cam0 > -100)
                        {
                            if (nph == "warn") { AddWarnAt(o.type == "steam" ? 220 : o.type == "vent" ? 200 : 380, .9f); }
                            if (nph == "burst") Snd.Gust();
                        }
                        o.phs = nph; o.pp = ph; break;
                    }
            }
        }

        int firstPigeon = -2;
        int FirstPigeon() { if (firstPigeon < 0 || firstPigeon >= OB.Count || OB[firstPigeon].type != "pigeon") { firstPigeon = OB.FindIndex(q => q.type == "pigeon"); } return firstPigeon; }
        void AddWarnAt(float y, float tt) { warns.Add(new Warn { y = y, t = tt }); Snd.Warn(); }

        void StepHurricane(float dt)
        {
            windX = 0; windY = 0;
            var h = hur;
            if (h.state == "none" && camX >= Cfg.HUR_WARN) { h.state = "warn"; hud.Banner("ハリケーン接近！ 強風に備えろ！", 3.4f, new Color(1f, .42f, .35f)); Snd.Gust(); }
            if (h.state == "warn" && camX >= Cfg.HUR_START) { h.state = "active"; h.t = 0; h.nd = .6f; h.di = 0; }
            if (h.state == "active" && camX >= Cfg.HUR_END) { h.state = "done"; hud.Banner("風がおさまった", 2.2f, new Color(.45f, .82f, .55f)); }
            float tgt = h.state == "warn" ? .45f : h.state == "active" ? 1f : 0f;
            storm = Mathf.Lerp(storm, tgt, Mathf.Min(1, dt * .8f));
            if (h.state == "active")
            {
                h.t += dt; float cyc = 2.5f, ph = h.t % cyc; int n = Mathf.FloorToInt(h.t / cyc);
                windX = -70;
                bool gw = ph >= 1.0f && ph < 1.7f; if (gw && !h.gw) Snd.Warn(); h.gw = gw;
                if (ph >= 1.7f && ph < 2.4f) { if (h.lastCyc != n) { h.lastCyc = n; Snd.Gust(); } float u = Mathf.Sin((ph - 1.7f) / .7f * Mathf.PI); windX = -70 - 420 * u; windY = (n % 2 == 1 ? -1 : 1) * 150 * u; }
                h.nd -= dt;
                if (h.nd <= 0) { float[] ys = { 160, 320, 235, 375, 125, 290, 205, 345 }; float y = ys[h.di % ys.Length]; h.di++; h.nd = 1.5f; h.q.Add(new Vector2(1.1f, y)); warns.Add(new Warn { y = y, t = 1.1f }); Snd.Warn(); }
                if (Random.value < dt * .25f) flash = .28f;
            }
            for (int i = h.q.Count - 1; i >= 0; i--)
            {
                var q = h.q[i]; q.x -= dt; h.q[i] = q;
                if (q.x <= 0) { h.q.RemoveAt(i); var o = new Ob { type = "debris", x = camX + W + 30, y = q.y, vx = -250, alive = true, i = OB.Count, k = (int)q.y % 3 }; o.x0 = o.x1 = 0; OB.Add(o); }
            }
        }

        void Finish(bool ok, string reason)
        {
            resultOk = ok; resultReason = reason; endT = 0; heartT = 0; resultShown = false; crashed = false;
            SetMode(Mode.End);
            float cx, cy; float[] rd, rc; PlayerRects(out cx, out cy, out rd, out rc);
            if (ok) { cargoState = "delivered"; Snd.Win(); Puff(deliverX, G - 20, new Color(1f, .95f, .77f), 18, 120, .9f, 10, false); Celebrate(Cfg.CUST_X, G - 90, 46); SetAnim(false, "Cheer"); }
            else { falling = true; fall = new Vector2(cx, cy); fallV = new Vector2(d.vx * .5f, 0); fallRot = d.th; cargoState = "fall"; Snd.Lose(); Puff(camX + d.sx, d.sy, new Color(1f, .55f, .23f), 20, 180, .6f, 6, false); }
            hud.SetHudVisible(true);
        }

        void StepEnd(float dt)
        {
            endT += dt;
            if (resultOk)
            {
                d.th = Mathf.Lerp(d.th, 0, .05f); d.vy = Mathf.Lerp(d.vy, -70, dt * 2); if (endT > .8f) d.sy += d.vy * dt; d.sx += 40 * dt;
                heartT -= dt; if (heartT <= 0 && endT < 3.4f) { heartT = .18f; if (endT < 1.6f) Celebrate(Cfg.CUST_X, G - 100, 6); }
                if (endT > 3.4f && !resultShown) ShowResult();
            }
            else
            {
                d.vy += 800 * dt; d.sy += d.vy * dt; d.tilt += dt * 3; d.prop = Mathf.Max(0, d.prop - dt);
                if (d.sy > G - 14) { d.sy = G - 14; d.vy = 0; if (!crashed) { crashed = true; Puff(camX + d.sx, G - 14, new Color(.3f, .3f, .32f), 14, 100, 1.1f, 14, true); shake = .4f; } }
                if (falling) { fallV.y += 900 * dt; fall.x += fallV.x * dt; fall.y += fallV.y * dt; fallRot += dt * 3; if (fall.y > G - 12) { fall.y = G - 12; fallV = new Vector2(fallV.x * .8f, 0); } }
                if (endT > 1.8f && !resultShown) ShowResult();
            }
        }

        string Fmt(float tt) { int m = Mathf.FloorToInt(tt / 60), s = Mathf.FloorToInt(tt % 60); return m + ":" + s.ToString("00"); }

        void ShowResult()
        {
            resultShown = true;
            string rows, rank = "-";
            if (resultOk)
            {
                rows = "タイム　　" + Fmt(playT) + "\n荷物　　　" + Mathf.RoundToInt(cargoHp) + "%\n機体　　　" + Mathf.RoundToInt(d.hp) + "%\n電池　　　" + Mathf.RoundToInt(d.bat) + "%";
                float cg = cargoHp, hp = d.hp;
                rank = (cg >= 90 && hp >= 80 && playT <= 112) ? "S" : (cg >= 75 && hp >= 50) ? "A" : cg >= 50 ? "B" : "C";
            }
            else rows = "到達度　　" + Mathf.RoundToInt(camX / goalCam * 100) + "%\n飛行時間　" + Fmt(playT) + "\n荷物　　　" + Mathf.RoundToInt(Mathf.Max(0, cargoHp)) + "%\n機体　　　" + Mathf.RoundToInt(Mathf.Max(0, d.hp)) + "%";
            hud.SetResult(resultOk, resultOk ? "お客さんが笑顔で荷物を受け取ったよ！" : resultReason + "。もう一回！", rows, rank);
            hud.ShowResult(true); ClearInput();
        }

        // ================= FX =================
        void BuildFxPools()
        {
            for (int i = 0; i < 260; i++)
            {
                var g = new GameObject("fx"); g.transform.SetParent(fxRoot, false);
                var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = circle; sr.sharedMaterial = Mats.Get("FxSprite"); g.SetActive(false);
                fx.Add(new Fx { sr = sr });
            }
            for (int i = 0; i < 6; i++)
            {
                var g = new GameObject("pulse"); g.transform.SetParent(fxRoot, false);
                var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = ring; sr.sharedMaterial = Mats.Get("FxSprite"); sr.color = new Color(.85f, .97f, 1f, .8f); g.SetActive(false);
                pulseSr.Add(sr);
            }
        }

        void Puff(float x, float y, Color col, int n, float spd, float life, float r, bool smoke)
        {
            for (int i = 0; i < n; i++)
            {
                Fx f = null; foreach (var q in fx) if (!q.alive) { f = q; break; }
                if (f == null) return;
                float a = Random.value * 6.2832f, s = Random.value * spd;
                f.alive = true; f.x = x; f.y = y; f.vx = Mathf.Cos(a) * s; f.vy = Mathf.Sin(a) * s - 20; f.t = 0; f.life = life * (.6f + Random.value * .5f); f.size = r * (.6f + Random.value * .8f); f.col = col; f.smoke = smoke;
                f.sr.gameObject.SetActive(true);
            }
        }

        void Celebrate(float x, float y, int n)
        {
            Color[] cs = { new Color(1f, .35f, .4f), new Color(1f, .85f, .3f), new Color(.4f, .85f, 1f), new Color(.6f, 1f, .5f), new Color(1f, .6f, .9f) };
            for (int i = 0; i < n; i++) Puff(x + Random.Range(-40f, 40f), y + Random.Range(-30f, 30f), cs[Random.Range(0, cs.Length)], 1, 260, 1.4f, 6, false);
        }

        void StepFx(float dt)
        {
            foreach (var f in fx)
            {
                if (!f.alive) continue;
                f.t += dt; if (f.t >= f.life) { f.alive = false; f.sr.gameObject.SetActive(false); continue; }
                f.x += f.vx * dt; f.y += f.vy * dt; f.vy += (f.smoke ? -30 : 180) * dt; f.vx *= (1f - dt * 1.5f);
            }
        }

        void AddPop(string text, float wx, float wy, Color c) { hud.Pop(text, Cfg.P(wx, wy, -.3f), c); }

        // ================= 見た目の構築 =================
        void BuildDrone()
        {
            droneRoot = new GameObject("Drone").transform;
            var body = droneRoot;
            Prims.Box(body, Mats.Get("DroneBody"), 0, 0, 50, 18, 34, 0f);
            Prims.Box(body, Mats.Get("DroneDark"), 0, 10, 36, 6, 28, 0f);
            Prims.Box(body, Mats.Get("DroneDark"), 0, -10, 30, 6, 20, 0f);
            Prims.Ball(body, Mats.Get("Glass"), 14, 2, 14, 12, 16, -14f, false);
            var l = Prims.Ball(body, Mats.Get("Led"), -16, 0, 6, 6, 6, -17f, false); led = l.GetComponent<Renderer>();
            float[] rx = { -34, 34, -34, 34 }, rz = { -18, -18, 18, 18 };
            for (int i = 0; i < 4; i++)
            {
                Prims.Rod(body, Mats.Get("DroneDark"), new Vector2(rx[i] * .3f, 2), new Vector2(rx[i], 8), 4f, rz[i] * .55f);
                Prims.Cyl(body, Mats.Get("DroneDark"), rx[i], 8, 8, 6, rz[i], false);
                var rt = Prims.Cyl(body, Mats.Get("Rotor"), rx[i], 12, 30, 1.5f, rz[i], false);
                var blade = Prims.Box(rt, Mats.Get("DroneDark"), 0, 0, 1, 1, 1, 0f, false); blade.localScale = new Vector3(1.05f, 3f, .08f); blade.localPosition = Vector3.zero;
                rotors[i] = rt;
            }
            Prims.Box(body, Mats.Get("DroneDark"), -16, -16, 3, 12, 3, 12f, false); Prims.Box(body, Mats.Get("DroneDark"), 16, -16, 3, 12, 3, 12f, false);
            // 荷物
            cargoRoot = new GameObject("Cargo").transform;
            var cargoBody = Prims.Box(cargoRoot, Mats.Get("Cargo"), 0, 0, 28, 24, 24, 0f);
            cargoBodyR = cargoBody.GetComponent<Renderer>(); cargoBodyMat = new Material(Mats.Get("Cargo")); cargoBodyR.material = cargoBodyMat;
            Prims.Box(cargoRoot, Mats.Get("Tape"), 0, 0, 6, 25, 25, 0f, false);
            cargoTapeTop = Prims.Box(cargoRoot, Mats.Get("Tape"), 0, 12, 29, 2, 25, 0f, false);
            Prims.Box(cargoRoot, Mats.Get("Metal"), 0, 14, 8, 5, 8, 0f, false);
            // 破損表現：フタが開き中身がはみ出す（低HPのみ見える）
            cargoFlap = Prims.Box(cargoRoot, Mats.Get("Cargo"), 0, 12, 26, 2, 22, 0f);
            cargoTapeBaseScale = cargoTapeTop.localScale;
            var spillMat = new Material(Mats.Get("Tape")); spillMat.color = new Color(.93f, .85f, .62f);
            cargoSpill = new Transform[3];
            cargoSpill[0] = Prims.Ball(cargoRoot, spillMat, -6, 15, 7, 7, 7, 0f);
            cargoSpill[1] = Prims.Ball(cargoRoot, spillMat, 3, 18, 8, 8, 8, 0f);
            cargoSpill[2] = Prims.Ball(cargoRoot, spillMat, 9, 14, 6, 6, 6, 0f);
            foreach (var s in cargoSpill) s.gameObject.SetActive(false);
            ropeT = Prims.Rod(new GameObject("RopeRoot").transform, Mats.Get("Rope"), Vector2.zero, Vector2.up, 2.2f, 0f);
        }

        // 荷物の損傷表現：HTML版drawCargoBoxの3段階（汚れ→テープが剥がれる→フタが開き中身がはみ出す）を3Dで再現
        static readonly Color CargoClean = new Color(.72f, .52f, .3f), CargoDirty = new Color(.33f, .24f, .14f);
        void UpdateCargoDamage(float hp)
        {
            hp = Mathf.Clamp(hp, 0, 100);
            float dm = (100f - hp) / 100f;
            cargoDmLerp = Mathf.Lerp(cargoDmLerp, dm, Mathf.Min(1f, Time.unscaledDeltaTime * 6f));
            if (cargoBodyMat) cargoBodyMat.color = Color.Lerp(CargoClean, CargoDirty, cargoDmLerp);
            // テープが剥がれる（hp<72で徐々にめくれ・縮む）
            float peel = Mathf.Clamp01((72f - hp) / 44f);
            if (cargoTapeTop)
            {
                cargoTapeTop.localPosition = new Vector3(peel * 5f * PX, (12f + peel * 3f) * PX, -peel * 3f * PX);
                cargoTapeTop.localRotation = Quaternion.Euler(peel * 50f, 0, peel * 14f);
                cargoTapeTop.localScale = new Vector3(cargoTapeBaseScale.x * (1f - peel * .35f), cargoTapeBaseScale.y, cargoTapeBaseScale.z);
            }
            // フタが開き、中身がはみ出す（hp<38）
            float open = Mathf.Clamp01((38f - hp) / 30f);
            if (cargoFlap) { cargoFlap.localPosition = new Vector3(0, (12f + open * 6f) * PX, -open * 10f * PX); cargoFlap.localRotation = Quaternion.Euler(-open * 55f, 0, 0); }
            if (cargoSpill != null)
                for (int i = 0; i < cargoSpill.Length; i++)
                {
                    float u = Mathf.Clamp01(open * 1.4f - i * .18f);
                    cargoSpill[i].gameObject.SetActive(u > .02f);
                    cargoSpill[i].localScale = Vector3.one * u;
                }
        }

        void BuildCharacters()
        {
            if (pilotPrefab)
            {
                pilot = Instantiate(pilotPrefab); pilot.name = "Pilot"; FixSkin(pilot, "Pilot_skin");
                pilot.transform.position = Cfg.P(205, G, -.2f); pilot.transform.rotation = Quaternion.Euler(0, 55f, 0); pilot.transform.localScale = Vector3.one * .936f;
                pilot.transform.position = new Vector3(pilot.transform.position.x, .045f, pilot.transform.position.z);
                pilotAnim = pilot.GetComponent<Animator>(); if (pilotAnim && pilotCtrl) pilotAnim.runtimeAnimatorController = pilotCtrl;
                pilot.AddComponent<FootGround>().Init(pilotAnim, pilot.transform.position);
            }
            if (receiverPrefab)
            {
                receiver = Instantiate(receiverPrefab); receiver.name = "Receiver";
                receiver.transform.position = Cfg.P(Cfg.CUST_X, G, -.2f); receiver.transform.rotation = Quaternion.Euler(0, -55f, 0); receiver.transform.localScale = Vector3.one * .936f;
                receiver.transform.position = new Vector3(receiver.transform.position.x, .045f, receiver.transform.position.z);
                recvAnim = receiver.GetComponent<Animator>(); if (recvAnim && receiverCtrl) recvAnim.runtimeAnimatorController = receiverCtrl;
                receiver.AddComponent<FootGround>().Init(recvAnim, receiver.transform.position);
            }
        }

        void FixSkin(GameObject g, string match)
        {
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                var ms = r.sharedMaterials; bool ch = false;
                for (int i = 0; i < ms.Length; i++) if (ms[i] && ms[i].name.StartsWith(match)) { ms[i] = Mats.Get("Skin0"); ch = true; }
                if (ch) r.sharedMaterials = ms;
            }
        }

        void SetAnim(bool isPilot, string state)
        {
            if (isPilot) { if (pilotAnim && pilotState != state) { pilotState = state; if (pilotAnim.runtimeAnimatorController) pilotAnim.CrossFade(state, .2f); } }
            else { if (recvAnim && recvState != state) { recvState = state; if (recvAnim.runtimeAnimatorController) recvAnim.CrossFade(state, .2f); } }
        }

        static void NoShadow(Transform t)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
        }

        void BuildEnv()
        {
            var g = new GameObject("Ground").transform; g.SetParent(worldRoot, false);
            Prims.Box(g, Mats.Get("Grass"), 7400, -30, 15600, 60, 3600, 800, false);
            Prims.Box(g, Mats.Get("Road"), 7400, 1, 15600, 2, 300, 0f, false);
            for (float x = 0; x < 14600; x += 120) Prims.Box(g, Mats.Get("Line"), x, 2.2f, 60, .6f, 6, 0f, false);
            Prims.Box(g, Mats.Get("Concrete"), 7400, 1.5f, 15600, 3, 80, -190f, false);
            Prims.Box(g, Mats.Get("Concrete"), 7400, 1.5f, 15600, 3, 80, 190f, false);
            // 着地パッド
            padMark = new GameObject("Pad").transform; padMark.SetParent(worldRoot, false); padMark.position = Cfg.P(Cfg.PAD_X, G, 0f);
            Prims.Cyl(padMark, Mats.Get("Pad"), 0, 2, 96, 4, 0f, false);
            Prims.Cyl(padMark, Mats.Get("Line"), 0, 4.5f, 70, 1, 0f, false);
            Prims.Cyl(padMark, Mats.Get("Pad"), 0, 5.5f, 44, 1, 0f, false);
            // お客様の家
            var hg = new GameObject("CustomerHouse");
            hg.transform.SetParent(worldRoot, false); hg.transform.position = Cfg.P(13960f, G, 320f * PX);
            Detail.AmericanHouse(hg.transform);
            // 遠景の街並み
            var far = new GameObject("Far").transform; far.SetParent(worldRoot, false);
            var rnd = new System.Random(1111);
            for (float x = -200; x < 15200; x += 130 + rnd.Next(0, 120))
            {
                var ob = new Ob { type = "building", x = 0, w = 120 + rnd.Next(0, 160), h = 250 + rnd.Next(0, 520), ex = new float[0][], fac = false, i = -1 };
                var go = ObFactory.Build(ob); go.transform.SetParent(far, false);
                go.transform.position = Cfg.P(x, G, 18f + (float)rnd.NextDouble() * 22f);
                go.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            NoShadow(far); StaticBatchingUtility.Combine(far.gameObject);
            // 中景の家並み
            var mid = new GameObject("Mid").transform; mid.SetParent(worldRoot, false);
            for (float x = 100; x < 14800; x += 330 + rnd.Next(0, 220))
            {
                if (x > 13500 && x < 14600) continue;
                var ob = new Ob { type = "house", x = 0, w = 130 + rnd.Next(0, 60), h = 100 + rnd.Next(0, 40), ch = rnd.Next(0, 2), col = rnd.Next(0, 3), i = -1 };
                var go = ObFactory.Build(ob); go.transform.SetParent(mid, false); go.transform.position = Cfg.P(x, G, 7f + (float)rnd.NextDouble() * 4f);
            }
            NoShadow(mid); StaticBatchingUtility.Combine(mid.gameObject);
            // 街灯
            for (float x = 300; x < 14500; x += 520)
            {
                var lp = new GameObject("Lamp").transform; lp.SetParent(worldRoot, false); lp.position = Cfg.P(x, G, -3.2f);
                Prims.Cyl(lp, Mats.Get("Pole"), 0, 120, 7, 240, 0f); Prims.Box(lp, Mats.Get("Pole"), 14, 238, 34, 5, 8, 0f); Prims.Box(lp, Mats.Get("Led2"), 28, 232, 14, 5, 10, 0f, false);
            }
        }

        // ================= 描画同期 =================
        bool Near(Ob o)
        {
            float lo, hi;
            switch (o.type)
            {
                case "balloon": case "trash": case "crow": case "pigeon": case "odrone": lo = hi = o.px; if (o.px == 0) return false; break;
                case "monkey": case "cell": case "debris": lo = hi = o.x; break;
                default: lo = o.x0; hi = o.x1; if (o.x0 == 0 && o.x1 == 0) { lo = hi = o.x; } break;
            }
            return hi > camX - 260 && lo < camX + W + 260;
        }

        void Render(float dt)
        {
            // 障害物の生成・同期
            for (int i = 0; i < OB.Count; i++)
            {
                var o = OB[i];
                bool vis = o.alive && Near(o);
                if (vis && o.go == null) { o.go = ObFactory.Build(o); o.go.transform.SetParent(worldRoot, false); }
                if (o.go != null)
                {
                    if (o.go.activeSelf != vis) o.go.SetActive(vis);
                    if (vis) ObFactory.Sync(o, t, camX);
                }
            }
            // ドローン
            float wx = camX + d.sx;
            droneRoot.position = Cfg.P(wx, d.sy, 0f);
            droneRoot.rotation = Quaternion.Euler(0, 0, -d.tilt * Mathf.Rad2Deg);
            bool showDrone = true;
            if (d.inv > 0 && Mathf.FloorToInt(t * 16f) % 2 == 0 && mode == Mode.Play) showDrone = false;
            droneRoot.gameObject.SetActive(showDrone);
            for (int i = 0; i < 4; i++) if (rotors[i]) rotors[i].localRotation = Quaternion.Euler(0, d.pa * 57.3f * 4f, 0);
            if (led) led.sharedMaterial = Mats.Get(Mathf.FloorToInt(t * 2f) % 2 == 0 ? "Led" : "Led2");
            // 荷物＆ロープ
            float cx = wx + Mathf.Sin(d.th) * (d.L + 12), cy = d.sy + 10 + Mathf.Cos(d.th) * (d.L + 12);
            bool ropeOn = false;
            if (cargoState == "hang" || (mode == Mode.Play) || (mode == Mode.End && resultOk && cargoState == "hang"))
            {
                cargoRoot.gameObject.SetActive(true);
                cargoRoot.position = Cfg.P(cx, cy, 0f); cargoRoot.rotation = Quaternion.Euler(0, 0, d.th * Mathf.Rad2Deg);
                ropeOn = true;
            }
            else if (cargoState == "ground") { cargoRoot.gameObject.SetActive(true); cargoRoot.position = Cfg.P(300, G - 12, 0f); cargoRoot.rotation = Quaternion.identity; }
            else if (cargoState == "delivered") { cargoRoot.gameObject.SetActive(true); cargoRoot.position = Cfg.P(deliverX, G - 12, 0f); cargoRoot.rotation = Quaternion.identity; }
            else if (cargoState == "fall") { cargoRoot.gameObject.SetActive(true); cargoRoot.position = Cfg.P(fall.x, fall.y, 0f); cargoRoot.rotation = Quaternion.Euler(0, 0, fallRot * Mathf.Rad2Deg); }
            if (mode == Mode.Intro && mt < intro2) ropeOn = false;
            ropeT.parent.gameObject.SetActive(ropeOn);
            if (ropeOn) Prims.SetRod(ropeT, new Vector2(wx, G - d.sy), new Vector2(cx, G - cy + 12), 2.4f);
            cargoRoot.localScale = Vector3.one * (cflash > 0 ? 1f + Mathf.Sin(t * 60f) * .06f : 1f);
            UpdateCargoDamage(cargoState == "ground" ? 100f : cargoHp);
            // キャラクター
            if (mode == Mode.Title) { SetAnim(true, "Idle"); SetAnim(false, "Idle"); }
            else if (mode == Mode.Play) SetAnim(false, atGoal ? "Wave" : "Idle");
            // パルス
            for (int i = 0; i < pulseSr.Count; i++)
            {
                if (i < pulses.Count)
                {
                    var q = pulses[i]; pulseSr[i].gameObject.SetActive(true);
                    pulseSr[i].transform.position = Cfg.P(q.x, q.y, -.2f);
                    pulseSr[i].transform.localScale = Vector3.one * (q.r * 2f * PX / (ring.rect.width / ring.pixelsPerUnit));
                    var c = pulseSr[i].color; c.a = .8f * (1f - q.age / .38f); pulseSr[i].color = c;
                }
                else pulseSr[i].gameObject.SetActive(false);
            }
            // パーティクル
            foreach (var f in fx)
            {
                if (!f.alive) continue;
                float u = f.t / f.life;
                f.sr.transform.position = Cfg.P(f.x, f.y, -.15f);
                float s = f.size * 2f * PX / (circle.rect.width / circle.pixelsPerUnit) * (f.smoke ? (1f + u * 1.8f) : (1f - u * .4f));
                f.sr.transform.localScale = Vector3.one * s;
                var c = f.col; c.a = f.col.a * (1f - u); f.sr.color = c;
            }
            // 予告マーカー
            warnWorld.Clear();
            for (int i = warns.Count - 1; i >= 0; i--) { warns[i].t -= dt; if (warns[i].t <= 0) warns.RemoveAt(i); }
            foreach (var w in warns) warnWorld.Add(Cfg.P(camX + W - 30, w.y, 0f));
            // カメラ・ライト
            UpdateCamera(dt);
            float prog = Mathf.Clamp01((camX + d.sx) / (Cfg.PAD_X));
            hud.Bars(d.hp, cargoHp, d.bat, prog, mode == Mode.Play || mode == Mode.End ? playT : 0f);
            hud.Tick(dt, cam, warnWorld);
        }

        void UpdateCamera(float dt)
        {
            float fov = 28f; cam.fieldOfView = fov; cam.orthographic = false;
            float distance = (Cfg.H * PX * .5f) / Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
            float cxu = (camX + W * .5f) * PX, cyu = (G - (270f - camY)) * PX;
            shake = Mathf.Max(0, shake - dt);
            Vector3 sh = shake > 0 ? new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * shake * .5f : Vector3.zero;
            if (mode == Mode.Title) { cyu += .3f; }
            cam.transform.position = new Vector3(cxu, cyu, -distance) + sh;
            cam.transform.rotation = Quaternion.identity;
            if (sky != null) sky.Tick(camX, t, storm);
            // 嵐の演出
            if (sun)
            {
                sun.color = Color.Lerp(baseSunCol, new Color(.6f, .65f, .75f), storm);
                sun.intensity = Mathf.Lerp(baseSunInt, baseSunInt * .45f, storm) + (flash > 0 ? 1.2f : 0);
            }
            flash = Mathf.Max(0, flash - dt);
            RenderSettings.fogColor = Color.Lerp(baseFog, new Color(.26f, .28f, .33f), storm);
            RenderSettings.ambientLight = Color.Lerp(baseAmbient, new Color(.28f, .3f, .38f), storm);
        }
    }
}
