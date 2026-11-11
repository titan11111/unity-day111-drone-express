using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Day111
{
    // uGUIをコードで組むHUD（英数字のみ。日本語フォント同梱なしでWebGLでも崩れない）
    public class Hud
    {
        public static Sprite Circle, Ring, Square;
        public RectTransform Root;
        public bool ShowTouch;

        Font font;
        Image barHp, barCargo, barBat, progFill;
        Text txtTime, txtBanner, txtTitleBlink, txtRetry, txtResTitle, txtResSub, txtResRows, txtRank, txtPause;
        GameObject goTitle, goResult, goPause, goHud, goTouch;
        RectTransform stickBase, stickKnob, fireBtn;
        Image fireImg;
        readonly List<Text> warnPool = new List<Text>(), popPool = new List<Text>();
        readonly List<float> popT = new List<float>();
        readonly List<Vector3> popW = new List<Vector3>();
        float bannerT, bannerTT;
        public float Scale = 1f;

        public static void MakeSprites()
        {
            if (Circle != null) return;
            Circle = MakeCircle(64, 0f, .9f);
            Ring = MakeCircle(128, .8f, .98f);
            var t = new Texture2D(4, 4); var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = Color.white;
            t.SetPixels(px); t.Apply();
            Square = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4);
        }

        // inner=0..1 の内側半径比（0なら塗りつぶし）、soft=外周のぼかし開始比
        static Sprite MakeCircle(int n, float inner, float soft)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            float c = (n - 1) * .5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = d > 1f ? 0f : (d > soft ? 1f - (d - soft) / (1f - soft) : 1f);
                    if (inner > 0f) a = d < inner - .06f ? 0f : (d < inner ? (d - (inner - .06f)) / .06f * a : a);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                }
            t.SetPixels32(px); t.filterMode = FilterMode.Bilinear; t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100);
        }

        public Hud(Transform parent)
        {
            MakeSprites();
            font = Resources.Load<Font>("D111Font");
            var cv = new GameObject("HudCanvas", typeof(RectTransform));
            cv.transform.SetParent(parent, false);
            var canvas = cv.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var cs = cv.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1280, 720); cs.matchWidthOrHeight = 1f;
            Root = cv.GetComponent<RectTransform>();

            goHud = Panel("Hud");
            // バー
            barHp = Bar(goHud.transform, "ドローン", 0, new Color(.2f, .75f, 1f));
            barCargo = Bar(goHud.transform, "荷物", 1, new Color(1f, .75f, .25f));
            barBat = Bar(goHud.transform, "バッテリー", 2, new Color(.45f, 1f, .6f));
            // 進行バー
            var pb = Img(goHud.transform, "Prog", Square, new Color(0, 0, 0, .45f));
            Anchor(pb.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -28), new Vector2(420, 10));
            progFill = Img(pb.transform, "Fill", Square, new Color(1f, .85f, .3f)); progFill.type = Image.Type.Filled; progFill.fillMethod = Image.FillMethod.Horizontal;
            Stretch(progFill.rectTransform);
            var pin = Img(pb.transform, "Pin", Circle, Color.white); Anchor(pin.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(22, 22)); pin.name = "Pin";
            txtTime = Txt(goHud.transform, "Time", 34, TextAnchor.UpperRight, Color.white);
            Anchor(txtTime.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-110, -18), new Vector2(240, 50)); txtTime.rectTransform.pivot = new Vector2(1, 1);
            var pbtn = Img(goHud.transform, "PauseBtn", Circle, new Color(0, 0, 0, .45f));
            Anchor(pbtn.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-50, -50), new Vector2(64, 64));
            var pt = Txt(pbtn.transform, "T", 30, TextAnchor.MiddleCenter, Color.white); pt.text = "II"; Stretch(pt.rectTransform);
            txtBanner = Txt(goHud.transform, "Banner", 46, TextAnchor.MiddleCenter, Color.white);
            Anchor(txtBanner.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -150), new Vector2(1000, 70));
            for (int i = 0; i < 8; i++) { var w = Txt(goHud.transform, "Warn", 60, TextAnchor.MiddleCenter, new Color(1f, .3f, .25f)); w.text = "!"; w.gameObject.SetActive(false); warnPool.Add(w); }
            for (int i = 0; i < 10; i++) { var p = Txt(goHud.transform, "Pop", 30, TextAnchor.MiddleCenter, Color.white); p.gameObject.SetActive(false); popPool.Add(p); popT.Add(0); popW.Add(Vector3.zero); }

            // タッチ操作（スティック＋パルスボタン）
            goTouch = Panel("Touch");
            fireImg = Img(goTouch.transform, "Fire", Circle, new Color(1f, 1f, 1f, .28f)); fireBtn = fireImg.rectTransform;
            Anchor(fireBtn, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-160, 160), new Vector2(200, 200));
            var ft = Txt(fireBtn, "T", 34, TextAnchor.MiddleCenter, Color.white); ft.text = "エアパルス"; Stretch(ft.rectTransform);
            var sb = Img(goTouch.transform, "StickBase", Ring, new Color(1, 1, 1, .45f)); stickBase = sb.rectTransform;
            Anchor(stickBase, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(220, 220)); stickBase.gameObject.SetActive(false);
            var sk = Img(goTouch.transform, "StickKnob", Circle, new Color(1, 1, 1, .5f)); stickKnob = sk.rectTransform;
            Anchor(stickKnob, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(100, 100)); stickKnob.gameObject.SetActive(false);

            // タイトル
            goTitle = Panel("Title");
            var dim = Img(goTitle.transform, "Dim", Square, new Color(.03f, .05f, .12f, .35f)); Stretch(dim.rectTransform);
            var tt = Txt(goTitle.transform, "T1", 110, TextAnchor.MiddleCenter, new Color(1f, .93f, .75f)); tt.text = "ドローン配達便";
            Anchor(tt.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 150), new Vector2(1200, 150));
            var st = Txt(goTitle.transform, "T2", 40, TextAnchor.MiddleCenter, new Color(.85f, .95f, 1f)); st.text = "荷物を安全にお届けしよう";
            Anchor(st.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 60), new Vector2(1200, 60));
            txtTitleBlink = Txt(goTitle.transform, "T3", 52, TextAnchor.MiddleCenter, Color.white); txtTitleBlink.text = "タップ / スペースでスタート";
            Anchor(txtTitleBlink.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -90), new Vector2(1200, 80));
            var hint = Txt(goTitle.transform, "T4", 28, TextAnchor.LowerCenter, new Color(1, 1, 1, .85f)); hint.text = "矢印 / WASD / スティック：移動     スペース / ボタン：エアパルス     P / ＋：ポーズ";
            Anchor(hint.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0, 40), new Vector2(1400, 60));
            var ver = Txt(goTitle.transform, "Ver", 18, TextAnchor.LowerLeft, new Color(1, 1, 1, .5f)); ver.text = "ビルド " + Cfg.BUILD_ID;
            Anchor(ver.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(14, 10), new Vector2(400, 30)); ver.rectTransform.pivot = new Vector2(0, 0);

            // 結果
            goResult = Panel("Result");
            var rd = Img(goResult.transform, "Dim", Square, new Color(0, 0, 0, .55f)); Stretch(rd.rectTransform);
            txtResTitle = Txt(goResult.transform, "R1", 100, TextAnchor.MiddleCenter, Color.white);
            Anchor(txtResTitle.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 190), new Vector2(1200, 130));
            txtResSub = Txt(goResult.transform, "R2", 34, TextAnchor.MiddleCenter, new Color(.9f, .95f, 1f));
            Anchor(txtResSub.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 110), new Vector2(1200, 50));
            txtResRows = Txt(goResult.transform, "R3", 40, TextAnchor.UpperCenter, Color.white);
            Anchor(txtResRows.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-120, 20), new Vector2(700, 260)); txtResRows.rectTransform.pivot = new Vector2(.5f, 1f);
            txtRank = Txt(goResult.transform, "R4", 190, TextAnchor.MiddleCenter, new Color(1f, .85f, .3f));
            Anchor(txtRank.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(330, -70), new Vector2(360, 260));
            txtRetry = Txt(goResult.transform, "R5", 48, TextAnchor.MiddleCenter, Color.white); txtRetry.text = "タップ / スペースでリトライ";
            Anchor(txtRetry.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -250), new Vector2(1200, 80));

            // ポーズ
            goPause = Panel("Pause");
            var pd = Img(goPause.transform, "Dim", Square, new Color(0, 0, 0, .55f)); Stretch(pd.rectTransform);
            txtPause = Txt(goPause.transform, "P1", 100, TextAnchor.MiddleCenter, Color.white); txtPause.text = "ポーズ中";
            Anchor(txtPause.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 40), new Vector2(1200, 140));
            var pr = Txt(goPause.transform, "P2", 44, TextAnchor.MiddleCenter, Color.white); pr.text = "タップ / P で再開";
            Anchor(pr.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -70), new Vector2(1200, 70));

            ShowTitle(false); ShowResult(false); ShowPause(false); SetHudVisible(false);
        }

        GameObject Panel(string name)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(Root, false);
            Stretch(g.GetComponent<RectTransform>());
            return g;
        }

        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero; }

        static void Anchor(RectTransform r, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size)
        {
            r.anchorMin = amin; r.anchorMax = amax; r.anchoredPosition = pos; r.sizeDelta = size;
        }

        Image Img(Transform p, string n, Sprite s, Color c)
        {
            var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false);
            var im = g.AddComponent<Image>(); im.sprite = s; im.color = c; im.raycastTarget = false; return im;
        }

        Text Txt(Transform p, string n, int size, TextAnchor a, Color c)
        {
            var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false);
            var t = g.AddComponent<Text>(); t.font = font; t.fontSize = size; t.alignment = a; t.color = c; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.fontStyle = FontStyle.Normal;
            var o = g.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, .75f); o.effectDistance = new Vector2(2, -2);
            return t;
        }

        Image Bar(Transform p, string label, int row, Color col)
        {
            var bg = Img(p, "Bar" + label, Square, new Color(0, 0, 0, .5f));
            Anchor(bg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150 + 160, -30 - row * 34), new Vector2(320, 20));
            var f = Img(bg.transform, "Fill", Square, col); f.type = Image.Type.Filled; f.fillMethod = Image.FillMethod.Horizontal; Stretch(f.rectTransform);
            var l = Txt(p, "L" + label, 22, TextAnchor.MiddleLeft, Color.white); l.text = label;
            Anchor(l.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24 + 60, -30 - row * 34), new Vector2(140, 28));
            return f;
        }

        public void SetHudVisible(bool v) { goHud.SetActive(v); goTouch.SetActive(v && ShowTouch); }
        public void ShowTitle(bool v) { goTitle.SetActive(v); }
        public void ShowPause(bool v) { goPause.SetActive(v); }
        public void ShowResult(bool v) { goResult.SetActive(v); }

        public void SetResult(bool ok, string reason, string rows, string rank)
        {
            txtResTitle.text = ok ? "お届け完了！" : "配達失敗…";
            txtResTitle.color = ok ? new Color(.65f, 1f, .7f) : new Color(1f, .5f, .45f);
            txtResSub.text = reason;
            txtResRows.text = rows; txtRank.text = rank;
        }

        public void Bars(float hp, float cargo, float bat, float prog, float time)
        {
            barHp.fillAmount = hp / 100f; barCargo.fillAmount = cargo / 100f; barBat.fillAmount = bat / 100f;
            barHp.color = hp > 50 ? new Color(.2f, .75f, 1f) : hp > 25 ? new Color(.95f, .65f, .1f) : new Color(.9f, .25f, .2f);
            barCargo.color = cargo > 50 ? new Color(1f, .75f, .25f) : cargo > 25 ? new Color(.95f, .5f, .1f) : new Color(.9f, .25f, .2f);
            progFill.fillAmount = Mathf.Clamp01(prog);
            var pin = progFill.transform.parent.Find("Pin") as RectTransform;
            if (pin) { var rt = (RectTransform)progFill.transform.parent; pin.anchoredPosition = new Vector2(rt.rect.width * Mathf.Clamp01(prog), 0); }
            int m = (int)(time / 60), s = (int)(time % 60);
            txtTime.text = m + ":" + s.ToString("00");
        }

        public void Banner(string text, float t, Color c) { txtBanner.text = text; txtBanner.color = c; bannerT = bannerTT = t; txtBanner.gameObject.SetActive(true); }

        public void Pop(string text, Vector3 world, Color c)
        {
            for (int i = 0; i < popPool.Count; i++)
                if (!popPool[i].gameObject.activeSelf) { popPool[i].text = text; popPool[i].color = c; popPool[i].gameObject.SetActive(true); popT[i] = 0; popW[i] = world; return; }
        }

        public void Tick(float dt, Camera cam, List<Vector3> warns)
        {
            Scale = Screen.height / 720f;
            if (bannerT > 0) { bannerT -= dt; var c = txtBanner.color; c.a = Mathf.Clamp01(Mathf.Min(bannerT, .4f) / .4f); txtBanner.color = c; if (bannerT <= 0) txtBanner.gameObject.SetActive(false); }
            else txtBanner.gameObject.SetActive(false);
            // 予告マーカー（画面右端）
            int n = 0;
            if (warns != null && goHud.activeSelf)
                foreach (var w in warns)
                {
                    if (n >= warnPool.Count) break;
                    var sp = cam.WorldToScreenPoint(w);
                    var t = warnPool[n++];
                    t.gameObject.SetActive(true);
                    ((RectTransform)t.transform).anchorMin = ((RectTransform)t.transform).anchorMax = Vector2.zero;
                    ((RectTransform)t.transform).anchoredPosition = new Vector2(Screen.width / Scale - 50, Mathf.Clamp(sp.y / Scale, 60, 660));
                    var tc = t.color; tc.a = .6f + .4f * Mathf.Sin(Time.unscaledTime * 18f); t.color = tc;
                }
            for (; n < warnPool.Count; n++) warnPool[n].gameObject.SetActive(false);
            for (int i = 0; i < popPool.Count; i++)
            {
                if (!popPool[i].gameObject.activeSelf) continue;
                popT[i] += dt;
                if (popT[i] > 1f) { popPool[i].gameObject.SetActive(false); continue; }
                var sp = cam.WorldToScreenPoint(popW[i]);
                var rt = (RectTransform)popPool[i].transform; rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.anchoredPosition = new Vector2(sp.x / Scale, sp.y / Scale + popT[i] * 60f);
                var pc = popPool[i].color; pc.a = 1f - popT[i]; popPool[i].color = pc;
            }
            if (goTitle.activeSelf) { var c = txtTitleBlink.color; c.a = .55f + .45f * Mathf.Sin(Time.unscaledTime * 4f); txtTitleBlink.color = c; }
            if (goResult.activeSelf) { var c = txtRetry.color; c.a = .55f + .45f * Mathf.Sin(Time.unscaledTime * 4f); txtRetry.color = c; }
        }

        // タッチUIの配置情報
        public Vector2 FireCenterScreen() { return new Vector2(Screen.width - 160 * Scale, 160 * Scale); }
        public float FireRadiusScreen() { return 100f * Scale; }
        public Rect PauseRectScreen() { return new Rect(Screen.width - 100 * Scale, Screen.height - 100 * Scale, 100 * Scale, 100 * Scale); }

        public void Touch(bool stickOn, Vector2 originScreen, Vector2 knobScreen, bool fire)
        {
            stickBase.gameObject.SetActive(stickOn); stickKnob.gameObject.SetActive(stickOn);
            if (stickOn) { stickBase.anchoredPosition = originScreen / Scale; stickKnob.anchoredPosition = knobScreen / Scale; }
            fireImg.color = new Color(1f, fire ? .8f : 1f, fire ? .4f : 1f, fire ? .6f : .28f);
        }
    }
}
