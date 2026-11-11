using UnityEngine;

namespace Day111
{
    // px単位（y上向き）でプリミティブを組み立てる小道具
    public static class Prims
    {
        const float PX = Cfg.PX;
        static Mesh prism;

        static Transform Make(PrimitiveType t, Transform parent, Material mat, Vector3 pos, Vector3 scale, Quaternion rot, bool shadow)
        {
            var go = GameObject.CreatePrimitive(t);
            var col = go.GetComponent<Collider>();
            if (col) Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = shadow;
            return go.transform;
        }

        // 中心(cx,cy)・幅w高さhの箱。cyは親原点から上向きpx、dは奥行px、zは奥行方向オフセットpx
        public static Transform Box(Transform p, Material m, float cx, float cy, float w, float h, float d = 40f, float z = 0f, bool shadow = true)
        {
            return Make(PrimitiveType.Cube, p, m, new Vector3(cx * PX, cy * PX, z * PX), new Vector3(w * PX, h * PX, d * PX), Quaternion.identity, shadow);
        }

        public static Transform Ball(Transform p, Material m, float cx, float cy, float w, float h, float d, float z = 0f, bool shadow = true)
        {
            return Make(PrimitiveType.Sphere, p, m, new Vector3(cx * PX, cy * PX, z * PX), new Vector3(w * PX, h * PX, d * PX), Quaternion.identity, shadow);
        }

        // 縦向き円柱（径dia・高さh）
        public static Transform Cyl(Transform p, Material m, float cx, float cy, float dia, float h, float z = 0f, bool shadow = true)
        {
            return Make(PrimitiveType.Cylinder, p, m, new Vector3(cx * PX, cy * PX, z * PX), new Vector3(dia * PX, h * PX * 0.5f, dia * PX), Quaternion.identity, shadow);
        }

        // 奥行方向(Z)を軸にした円柱（車輪など）
        public static Transform Wheel(Transform p, Material m, float cx, float cy, float dia, float width, float z = 0f)
        {
            return Make(PrimitiveType.Cylinder, p, m, new Vector3(cx * PX, cy * PX, z * PX), new Vector3(dia * PX, width * PX * 0.5f, dia * PX), Quaternion.Euler(90f, 0f, 0f), true);
        }

        // 2点間の棒（ワイヤー・ロープ）。座標は親ローカルpx（y上向き）
        public static Transform Rod(Transform p, Material m, Vector2 a, Vector2 b, float thick, float z = 0f)
        {
            Vector3 pa = new Vector3(a.x * PX, a.y * PX, z * PX), pb = new Vector3(b.x * PX, b.y * PX, z * PX);
            var t = Make(PrimitiveType.Cylinder, p, m, (pa + pb) * 0.5f, new Vector3(thick * PX, (pb - pa).magnitude * 0.5f, thick * PX), Quaternion.FromToRotation(Vector3.up, (pb - pa).normalized), false);
            return t;
        }

        public static void SetRod(Transform t, Vector2 a, Vector2 b, float thick)
        {
            Vector3 pa = new Vector3(a.x * PX, a.y * PX, t.localPosition.z), pb = new Vector3(b.x * PX, b.y * PX, t.localPosition.z);
            t.localPosition = (pa + pb) * 0.5f;
            t.localScale = new Vector3(thick * PX, (pb - pa).magnitude * 0.5f, thick * PX);
            t.localRotation = Quaternion.FromToRotation(Vector3.up, (pb - pa).normalized);
        }

        // 三角柱（切妻屋根）。底辺中心がcx、底がcy、幅w・高さh・奥行d
        public static Transform Roof(Transform p, Material m, float cx, float cy, float w, float h, float d, float z = 0f)
        {
            if (prism == null) prism = BuildPrism();
            var go = new GameObject("Roof");
            go.transform.SetParent(p, false);
            go.transform.localPosition = new Vector3(cx * PX, cy * PX, z * PX);
            go.transform.localScale = new Vector3(w * PX, h * PX, d * PX);
            go.AddComponent<MeshFilter>().sharedMesh = prism;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            return go.transform;
        }

        static Mesh BuildPrism()
        {
            // x:-0.5..0.5, y:0..1(頂点は中央), z:-0.5..0.5
            Vector3 a0 = new Vector3(-0.5f, 0, -0.5f), a1 = new Vector3(0.5f, 0, -0.5f), a2 = new Vector3(0, 1, -0.5f);
            Vector3 b0 = new Vector3(-0.5f, 0, 0.5f), b1 = new Vector3(0.5f, 0, 0.5f), b2 = new Vector3(0, 1, 0.5f);
            var v = new System.Collections.Generic.List<Vector3>();
            var tri = new System.Collections.Generic.List<int>();
            System.Action<Vector3, Vector3, Vector3> T = (p0, p1, p2) =>
            {
                int i = v.Count; v.Add(p0); v.Add(p1); v.Add(p2); tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
            };
            T(a0, a2, a1);               // 手前（カメラ側 z=-0.5）
            T(b0, b1, b2);               // 奥
            T(a0, b0, b2); T(a0, b2, a2); // 左斜面
            T(a1, a2, b2); T(a1, b2, b1); // 右斜面
            T(a0, a1, b1); T(a0, b1, b0); // 底
            var mesh = new Mesh();
            mesh.SetVertices(v);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
