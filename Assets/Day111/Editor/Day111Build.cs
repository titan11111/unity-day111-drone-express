using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Day111Editor
{
    public static class Day111Build
    {
        const string MatDir = "Assets/Resources/D111Mats";

        static string VerDir { get { var p = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Verification")); Directory.CreateDirectory(p); return p; } }

        [MenuItem("Day111/1 Build All (materials, animation, scene, settings)")]
        public static void BuildAll()
        {
            var log = new StringBuilder();
            try
            {
                BuildMaterials(log);
                SetupModels(log);
                var ctrls = BuildControllers(log);
                BuildScene(log, ctrls);
                ApplySettings(log);
                AssetDatabase.SaveAssets();
                log.AppendLine("OK");
            }
            catch (Exception e) { log.AppendLine("ERROR " + e); }
            File.WriteAllText(Path.Combine(VerDir, "build-all-log.txt"), log.ToString());
            Debug.Log("Day111 BuildAll done");
        }

        static Material Mk(string name, Color c, float metal = 0f, float smooth = .25f, Color? emis = null, float emisK = 1f)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
            m.shader = Shader.Find("Standard");
            m.color = c; m.SetFloat("_Metallic", metal); m.SetFloat("_Glossiness", smooth);
            if (emis.HasValue)
            {
                m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emis.Value * emisK);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); }
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Transparent(Material m)
        {
            m.SetFloat("_Mode", 3); m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0); m.DisableKeyword("_ALPHATEST_ON"); m.EnableKeyword("_ALPHABLEND_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000; EditorUtility.SetDirty(m);
        }

        static Color C(float r, float g, float b, float a = 1f) { return new Color(r, g, b, a); }

        static void BuildMaterials(StringBuilder log)
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources/D111Mats"));
            AssetDatabase.Refresh();
            Mk("Wall0", C(.93f, .85f, .7f)); Mk("Wall1", C(.7f, .8f, .9f)); Mk("Wall2", C(.9f, .6f, .53f));
            Mk("Roof0", C(.58f, .2f, .16f)); Mk("Roof1", C(.28f, .32f, .4f)); Mk("Roof2", C(.18f, .43f, .4f));
            Mk("Door", C(.35f, .2f, .12f)); Mk("Brick", C(.55f, .25f, .2f));
            Mk("Glass", C(.35f, .45f, .6f), 0, .8f, C(1f, .75f, .35f), .9f);
            Mk("Bldg", C(.85f, .83f, .8f)); Mk("Factory", C(.66f, .64f, .62f));
            Mk("MetalDark", C(.2f, .22f, .26f), .6f, .5f); Mk("Metal", C(.66f, .69f, .73f), .8f, .55f);
            Mk("Wire", C(.07f, .07f, .08f)); Mk("Pole", C(.28f, .22f, .17f));
            Mk("Trunk", C(.36f, .23f, .13f)); Mk("Leaf", C(.2f, .5f, .2f)); Mk("Leaf2", C(.3f, .6f, .22f));
            Mk("Sign", C(.96f, .8f, .2f), 0, .4f, C(1f, .8f, .2f), .7f);
            Mk("Concrete", C(.62f, .62f, .6f)); Mk("CarBody", C(1f, 1f, 1f), .3f, .7f); Mk("Tire", C(.05f, .05f, .05f));
            Mk("Led", C(1f, .2f, .15f), 0, .5f, C(1f, .15f, .1f), 2.2f); Mk("Led2", C(.4f, .05f, .05f), 0, .5f, C(.4f, .02f, .02f), .5f);
            Mk("Balloon0", C(1f, .35f, .3f), 0, .85f); Mk("Balloon1", C(.3f, .7f, 1f), 0, .85f); Mk("Balloon2", C(1f, .82f, .25f), 0, .85f);
            Mk("Rope", C(.8f, .7f, .5f)); Mk("Trash", C(.5f, .56f, .62f), 0, .5f);
            Mk("Crow", C(.07f, .07f, .09f), 0, .5f); Mk("Pigeon", C(.55f, .57f, .62f)); Mk("Beak", C(.95f, .7f, .2f));
            Mk("Fur", C(.5f, .33f, .2f)); Mk("Skin0", C(.86f, .63f, .5f), 0, .35f); Mk("Skin1", C(.9f, .7f, .55f));
            Mk("Cell", C(.2f, .9f, .45f), .2f, .7f, C(.2f, 1f, .5f), 1.4f);
            Mk("DroneBody", C(.93f, .94f, .96f), .2f, .6f); Mk("DroneDark", C(.15f, .16f, .19f), .4f, .4f); Mk("Rotor", C(.25f, .27f, .32f), 0, .4f);
            Mk("Crane", C(.96f, .76f, .1f), .2f, .4f); Mk("Warn", C(.96f, .8f, .1f)); Mk("Wood", C(.55f, .38f, .2f));
            Mk("Grass", C(.36f, .5f, .26f)); Mk("Road", C(.18f, .18f, .2f)); Mk("Line", C(.92f, .92f, .86f));
            Mk("Pad", C(.2f, .8f, .4f), 0, .5f, C(.2f, .9f, .45f), .9f);
            Mk("Cargo", C(.72f, .52f, .3f)); Mk("Tape", C(.86f, .8f, .66f));
            var plume = Mk("FxPlume", C(.9f, .95f, 1f, .3f), 0, .6f); Transparent(plume);
            string sp = MatDir + "/FxSprite.mat";
            var fxs = AssetDatabase.LoadAssetAtPath<Material>(sp);
            if (fxs == null) { fxs = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(fxs, sp); }
            AssetDatabase.SaveAssets();
            log.AppendLine("materials ok");
        }

        static void SetupModels(StringBuilder log)
        {
            foreach (var n in new[] { "Pilot", "Receiver" })
            {
                string path = "Assets/Characters/" + n + ".fbx";
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp == null) { log.AppendLine("no importer " + n); continue; }
                var clips = imp.defaultClipAnimations;
                foreach (var c in clips) { c.loopTime = true; c.loopPose = true; }
                imp.clipAnimations = clips;
                imp.SaveAndReimport();
                log.AppendLine("model " + n + " clips=" + clips.Length);
            }
        }

        static RuntimeAnimatorController[] BuildControllers(StringBuilder log)
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Day111/Animation"));
            AssetDatabase.Refresh();
            var res = new RuntimeAnimatorController[2]; int idx = 0;
            foreach (var n in new[] { "Pilot", "Receiver" })
            {
                string cp = "Assets/Day111/Animation/" + n + ".controller";
                AssetDatabase.DeleteAsset(cp);
                var ctrl = AnimatorController.CreateAnimatorControllerAtPath(cp);
                var sm = ctrl.layers[0].stateMachine;
                foreach (var st in new[] { "Idle", "Wave", "Cheer" })
                {
                    AnimationClip clip = null;
                    foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Characters/" + n + ".fbx"))
                        if (o is AnimationClip ac && ac.name == n + "_" + st) clip = ac;
                    var s = sm.AddState(st); s.motion = clip;
                    if (st == "Idle") sm.defaultState = s;
                    log.AppendLine(n + " state " + st + " clip=" + (clip ? clip.name : "MISSING"));
                }
                res[idx++] = ctrl;
            }
            return res;
        }

        static void BuildScene(StringBuilder log, RuntimeAnimatorController[] ctrls)
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); camGo.AddComponent<AudioListener>();
            cam.fieldOfView = 28; cam.nearClipPlane = .3f; cam.farClipPlane = 260; cam.clearFlags = CameraClearFlags.Skybox; cam.allowHDR = false;
            camGo.transform.position = new Vector3(9.6f, 4f, -20f);

            var lgo = new GameObject("Sun"); var light = lgo.AddComponent<Light>();
            light.type = LightType.Directional; light.color = new Color(1f, .8f, .6f); light.intensity = 1.25f;
            light.shadows = LightShadows.Soft; light.shadowStrength = .75f; light.shadowBias = .05f;
            lgo.transform.rotation = Quaternion.Euler(20.5f, -40f, 0f);

            var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Day111/Sky.mat");
            if (sky == null) { sky = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(sky, "Assets/Day111/Sky.mat"); }
            sky.SetFloat("_SunSize", .08f); sky.SetFloat("_AtmosphereThickness", 1.6f); sky.SetColor("_SkyTint", new Color(.62f, .5f, .75f)); sky.SetColor("_GroundColor", new Color(.45f, .4f, .38f)); sky.SetFloat("_Exposure", 1.15f);
            RenderSettings.skybox = sky; RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.55f, .6f, .82f); RenderSettings.ambientEquatorColor = new Color(.62f, .5f, .5f); RenderSettings.ambientGroundColor = new Color(.3f, .27f, .25f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 28f; RenderSettings.fogEndDistance = 120f; RenderSettings.fogColor = new Color(.95f, .72f, .58f);

            var type = Type.GetType("Day111.DroneGame, Day111.Game");
            if (type == null) throw new Exception("Day111.DroneGame type not found (compile error?)");
            var game = new GameObject("Game"); var comp = game.AddComponent(type);
            var so = new SerializedObject(comp);
            so.FindProperty("pilotPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Pilot.fbx");
            so.FindProperty("receiverPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Receiver.fbx");
            so.FindProperty("pilotCtrl").objectReferenceValue = ctrls[0];
            so.FindProperty("receiverCtrl").objectReferenceValue = ctrls[1];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
            log.AppendLine("scene saved");
        }

        static void ApplySettings(StringBuilder log)
        {
            PlayerSettings.companyName = "Titan";
            PlayerSettings.productName = "111ドローン";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.SetPropertyBool("useHDR", false, BuildTargetGroup.WebGL);
            QualitySettings.shadowDistance = 70f;
            log.AppendLine("settings ok");
        }

        [MenuItem("Day111/2 Switch to WebGL")]
        public static void SwitchWebGL()
        {
            bool ok = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            File.WriteAllText(Path.Combine(VerDir, "switch-webgl.txt"), "switch=" + ok + " active=" + EditorUserBuildSettings.activeBuildTarget);
        }

        [MenuItem("Day111/3 Build WebGL")]
        public static void BuildWebGL()
        {
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../WebBuild"));
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/Main.unity" }, locationPathName = outDir, target = BuildTarget.WebGL, options = BuildOptions.None };
            var rep = BuildPipeline.BuildPlayer(opts);
            var sb = new StringBuilder();
            sb.AppendLine("result=" + rep.summary.result + " size=" + rep.summary.totalSize + " errors=" + rep.summary.totalErrors + " warnings=" + rep.summary.totalWarnings + " time=" + rep.summary.totalTime);
            foreach (var step in rep.steps) foreach (var m in step.messages) if (m.type == LogType.Error || m.type == LogType.Exception) sb.AppendLine("  " + m.type + ": " + m.content);
            File.WriteAllText(Path.Combine(VerDir, "build-report.txt"), sb.ToString());
        }

        [MenuItem("Day111/Bot ON (x6 speed)")]
        public static void BotOn() { PlayerPrefs.SetInt("d111bot", 1); PlayerPrefs.SetFloat("d111speed", 6f); PlayerPrefs.Save(); }

        [MenuItem("Day111/Bot OFF")]
        public static void BotOff() { PlayerPrefs.SetInt("d111bot", 0); PlayerPrefs.SetFloat("d111speed", 1f); PlayerPrefs.Save(); }

        [MenuItem("Day111/4 Force Save All")]
        public static void SaveAll() { AssetDatabase.SaveAssets(); EditorSceneManager.SaveOpenScenes(); }
    }
}
