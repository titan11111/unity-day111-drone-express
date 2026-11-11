using System.Collections.Generic;
using UnityEngine;

namespace Day111
{
    // 障害物・ギミック1つ分のデータ（ルールはHTML版の当たり判定矩形を基準）
    public class Ob
    {
        public string type;
        public float x, y, w, h, ph, yc, amp, T, sp, lb, tr, by;
        public int ch, col, c, k, i;
        public float[][] ex;
        public float[] ys;
        public bool alive = true, warned, act, fac;
        public string mode, st, phs;
        public float px, py, vx, vy, rot, mt, bt, hd, y0, t0, x0, x1, wr, pp, face, t, elev;
        public bool init;
        public GameObject go;
        public Transform a, b, c1, c2, c3;   // 動く部品
        public Transform[] list;

        public static readonly HashSet<string> Push = new HashSet<string> { "balloon", "trash", "crow", "pigeon", "debris", "monkey" };
        public static readonly HashSet<string> WarnOn = new HashSet<string> { "crow", "pigeon", "trash", "odrone", "crane", "vent", "steam", "sprinkler" };

        public static float Dmg(string t)
        {
            switch (t)
            {
                case "wire": return 12; case "house": return 20; case "building": return 20; case "tree": return 18;
                case "sign": return 20; case "car": return 16; case "viaduct": return 20; case "balloon": return 6;
                case "trash": return 6; case "crow": return 10; case "pigeon": return 8; case "debris": return 12;
                case "monkey": return 9; case "odrone": return 18; case "crane": return 20; case "steam": return 14;
                case "sprinkler": return 5; default: return 12;
            }
        }

        public void SetBounds()
        {
            switch (type)
            {
                case "wire": case "building": case "house": case "car": case "viaduct": x0 = x; x1 = x + w; break;
                case "tree": x0 = x - 200; x1 = x + 80; break;
                case "crane": x0 = x - 190; x1 = x + 190; break;
                case "sign": x0 = x; x1 = x + w; break;
                case "steam": case "vent": x0 = x - 40; x1 = x + 40; break;
                case "sprinkler": x0 = x - 230; x1 = x + 20; break;
                default: x0 = x - 60; x1 = x + 60; break;
            }
        }

        const float G = Cfg.GROUND;

        // 当たり判定矩形（世界座標px・y下向き）: [x,y,w,h]
        public float[][] Rects()
        {
            switch (type)
            {
                case "wire":
                    { var r = new float[ys.Length][]; for (int n = 0; n < ys.Length; n++) r[n] = new[] { x, ys[n] - 4, w, 8 }; return r; }
                case "house":
                    {
                        float bh = h * .62f, rh = h - bh, t = G - h;
                        var l = new List<float[]> { new[] { x, G - bh, w, bh }, new[] { x + w * .14f, t, w * .72f, rh } };
                        if (ch == 1) l.Add(new[] { x + w * .66f, t - 30, 14, 34 });
                        return l.ToArray();
                    }
                case "building":
                    {
                        float t = G - h;
                        var l = new List<float[]> { new[] { x, t, w, h } };
                        if (ex != null) foreach (var e in ex) l.Add(new[] { x + e[0], t - e[2], e[1], e[2] });
                        return l.ToArray();
                    }
                case "tree": return new[] { new[] { x - 10, G - 250, 20, 250 }, new[] { x - 70, 205, 140, 80 }, new[] { x - 190, 258, 125, 12 } };
                case "sign": return new[] { new[] { x + 8, y, w - 8, h } };
                case "viaduct": return new[] { new[] { x, 150, w, 75 } };
                case "car": return new[] { new[] { x, G - h, w, h } };
                case "balloon": return new[] { new[] { px - 12, py - 16, 24, 32 } };
                case "trash": return mode == "drift" ? new[] { new[] { px - 13, py - 11, 26, 22 } } : null;
                case "crow": return (mode == "perch" || mode == "alert" || mode == "chase") ? new[] { new[] { px - 14, py - 8, 28, 16 } } : null;
                case "pigeon": return (mode == "perch" || mode == "warn" || mode == "fly") ? new[] { new[] { px - 9, py - 6, 18, 12 } } : null;
                case "debris": return new[] { new[] { x - 17, y - 10, 34, 20 } };
                case "monkey": return st == "flee" ? null : new[] { new[] { x - 12, y - 32, 24, 32 } };
                case "cell": return new[] { new[] { x - 14, y - 12, 28, 24 } };
                case "odrone": return act ? new[] { new[] { px - 24, py - 12, 48, 30 } } : null;
                case "crane": return new[] { new[] { x - 190, 72, 380, 14 }, new[] { x + 130, 86, 40, 26 }, new[] { x + tr - 2, 86, 4, lb - 34 - 86 }, new[] { x + tr - 26, lb - 34, 52, 34 } };
                case "steam":
                    {
                        var l = new List<float[]> { new[] { x - 24, G - 34, 48, 34 } };
                        if (phs == "burst") l.Add(new[] { x - 28, 170, 56, G - 34 - 170 });
                        return l.ToArray();
                    }
                case "sprinkler":
                    {
                        if (phs != "burst") return null;
                        var l = new List<float[]>();
                        for (int n = 0; n <= 6; n++) { float u = n / 6f; l.Add(new[] { x - 210 * u - 10, G - 8 - 130 * 4 * u * (1 - u) - 10, 20, 20 }); }
                        return l.ToArray();
                    }
                default: return null;
            }
        }
    }
}
