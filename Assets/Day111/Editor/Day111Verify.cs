using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Day111Editor
{
    public static class Day111Verify
    {
        [MenuItem("Day111/Verify Characters")]
        public static void VerifyCharacters()
        {
            var sb = new StringBuilder();
            foreach (var name in new[] { "Pilot", "Receiver" })
            {
                string path = "Assets/Characters/" + name + ".fbx";
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                sb.AppendLine("== " + name + " ==");
                if (imp == null) { sb.AppendLine("importer null"); continue; }
                sb.AppendLine("animationType=" + imp.animationType);
                sb.AppendLine("globalScale=" + imp.globalScale + " useFileScale=" + imp.useFileScale);
                var all = AssetDatabase.LoadAllAssetsAtPath(path);
                Avatar av = null; int smr = 0; var clips = new StringBuilder();
                foreach (var o in all)
                {
                    if (o is Avatar a) av = a;
                    if (o is SkinnedMeshRenderer) smr++;
                    if (o is AnimationClip c && !c.name.StartsWith("__preview__"))
                        clips.AppendLine("  clip " + c.name + " len=" + c.length + " fps=" + c.frameRate + " loop=" + c.isLooping);
                }
                sb.AppendLine("avatar=" + (av ? av.name + " valid=" + av.isValid + " human=" + av.isHuman : "none") + " skinnedRenderers=" + smr);
                sb.AppendLine("clips:\n" + clips);
                sb.AppendLine("importedTakes=" + imp.importedTakeInfos.Length);
                foreach (var t in imp.importedTakeInfos) sb.AppendLine("  take " + t.name + " " + t.startTime + "-" + t.stopTime);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null)
                {
                    var b = new Bounds(Vector3.zero, Vector3.zero); bool first = true;
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                    { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
                    sb.AppendLine("bounds size=" + b.size + " center=" + b.center);
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                        foreach (var m in r.sharedMaterials)
                            if (m) sb.AppendLine("  mat " + m.name + " shader=" + m.shader.name + " color=" + (m.HasProperty("_Color") ? m.color.ToString() : "n/a"));
                }
            }
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Verification"));
            Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir, "humanoid-report.txt"), sb.ToString());
            Debug.Log("Day111 verify written");
        }
    }
}
