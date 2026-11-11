using System.Collections.Generic;
using UnityEngine;

namespace Day111
{
    // Resources/D111Mats のマテリアルを名前で取得（WebGLでシェーダーが削られないよう、アセット参照で持つ）
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static readonly Dictionary<string, Material> tints = new Dictionary<string, Material>();

        public static Material Get(string n)
        {
            Material m;
            if (cache.TryGetValue(n, out m) && m) return m;
            m = Resources.Load<Material>("D111Mats/" + n);
            if (!m)
            {
                var sh = Shader.Find("Standard");
                if (!sh) sh = Shader.Find("Sprites/Default");
                m = new Material(sh);
                m.name = n + "(fallback)";
            }
            cache[n] = m;
            return m;
        }

        public static Material Tint(string baseName, Color c)
        {
            string k = baseName + "|" + ColorUtility.ToHtmlStringRGBA(c);
            Material m;
            if (tints.TryGetValue(k, out m) && m) return m;
            m = new Material(Get(baseName));
            m.color = c;
            tints[k] = m;
            return m;
        }
    }
}
