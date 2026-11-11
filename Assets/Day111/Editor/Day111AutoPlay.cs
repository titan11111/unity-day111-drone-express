#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Day111EditorTools
{
    // エディタ起動後に1回だけ：Mainシーンを開いて再生する（テストプレイ用）。
    [InitializeOnLoad]
    public static class Day111AutoPlay
    {
        const string Scene = "Assets/Scenes/Main.unity";

        static Day111AutoPlay()
        {
            if (SessionState.GetBool("d111autoplay", false)) return;
            SessionState.SetBool("d111autoplay", true);
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; EditorApplication.delayCall += Run; return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Run; return; }
            if (!System.IO.File.Exists(Scene)) { Debug.LogWarning("[Day111] Main.unity がありません。メニュー Day111 > Build All を実行してください。"); return; }
            if (EditorSceneManager.GetActiveScene().path != Scene) EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            Debug.Log("[Day111] Mainを開いて再生します");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
