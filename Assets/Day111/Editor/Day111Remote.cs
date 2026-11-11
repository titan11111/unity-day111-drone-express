#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Day111EditorTools
{
    // Verification/cmd.txt に命令を書くと実行する「リモコン」。
    //   play / stop / shot / status / clearlog / scene
    // 結果：Verification/status.txt, console.log, shot.png
    [InitializeOnLoad]
    public static class Day111Remote
    {
        static readonly string Dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Verification"));
        static readonly string Cmd = Path.Combine(Dir, "cmd.txt"), Log = Path.Combine(Dir, "console.log"), Stat = Path.Combine(Dir, "status.txt");
        const string MainScene = "Assets/Scenes/Main.unity";
        static DateTime last;
        static double nextCheck;
        static int shotCountdown = -1;

        static Day111Remote()
        {
            try { Directory.CreateDirectory(Dir); } catch { }
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            WriteStatus("loaded");
        }

        static void OnLog(string msg, string stack, LogType t)
        {
            if (t == LogType.Log) return;
            try { File.AppendAllText(Log, DateTime.Now.ToString("HH:mm:ss") + " [" + t + "] " + msg + "\n"); } catch { }
        }

        static void WriteStatus(string note)
        {
            try
            {
                File.WriteAllText(Stat, DateTime.Now.ToString("s") + " note=" + note + " playing=" + EditorApplication.isPlaying + " compiling=" + EditorApplication.isCompiling +
                    " scene=" + EditorSceneManager.GetActiveScene().path + " frame=" + Time.frameCount + "\n");
            }
            catch { }
        }

        static void Tick()
        {
            if (shotCountdown > 0 && --shotCountdown == 0)
            {
                shotCountdown = -1;
                try { ScreenCapture.CaptureScreenshot(Path.Combine(Dir, "shot.png"), 1); WriteStatus("shot requested"); } catch (Exception e) { OnLog(e.Message, "", LogType.Error); }
            }
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + .5;
            if (!File.Exists(Cmd)) return;
            var w = File.GetLastWriteTimeUtc(Cmd); if (w == last) return; last = w;
            string c; try { c = File.ReadAllText(Cmd).Trim().ToLowerInvariant(); } catch { return; }
            switch (c)
            {
                case "play":
                    if (EditorApplication.isCompiling || EditorApplication.isUpdating) { last = DateTime.MinValue; return; }
                    if (EditorSceneManager.GetActiveScene().path != MainScene && File.Exists(MainScene)) EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
                    EditorApplication.isPlaying = true; WriteStatus("play"); break;
                case "stop": EditorApplication.isPlaying = false; WriteStatus("stop"); break;
                case "shot":
                    if (!EditorApplication.isPlaying) { WriteStatus("shot skipped: not playing"); break; }
                    shotCountdown = 3; break;
                case "clearlog": try { File.WriteAllText(Log, ""); } catch { } WriteStatus("log cleared"); break;
                case "refresh": AssetDatabase.Refresh(); WriteStatus("refresh"); break;
                default: WriteStatus("status (cmd=" + c + ")"); break;
            }
        }
    }
}
#endif
