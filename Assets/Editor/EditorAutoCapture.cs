using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Runs the -autocapture play-through inside the editor's Play mode (used when the built
// player can't be launched, e.g. Smart App Control blocks unsigned exes).
// Usage: Unity.exe -projectPath <p> -autocapture -captureDir <dir> -executeMethod EditorAutoCapture.Run
[InitializeOnLoad]
public static class EditorAutoCapture
{
    private static double deadline;
    private static double nextBeat;

    static EditorAutoCapture()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-autocapture") < 0) return;
        deadline = EditorApplication.timeSinceStartup + 600;
        EditorApplication.update += Poll;
    }

    public static void Run()
    {
        string dir = CaptureDir();
        if (File.Exists(Path.Combine(dir, "log.txt"))) File.Delete(Path.Combine(dir, "log.txt"));
        EditorSceneManager.OpenScene("Assets/_BlindGunfight/Scenes/Lobby.unity");
        PlayModeWindow.SetCustomRenderingResolution(1280, 720, "AutoCapture 1280x720");
        EditorApplication.isPlaying = true;
    }

    private static void Poll()
    {
        bool done = EditorApplication.isPlaying && File.Exists(Path.Combine(CaptureDir(), "log.txt"));
        if (!done && EditorApplication.timeSinceStartup < deadline)
        {
            // keep the Game view rendering even when the editor window is in the background
            if (EditorApplication.isPlaying)
            {
                if (EditorApplication.timeSinceStartup >= nextBeat)
                {
                    nextBeat = EditorApplication.timeSinceStartup + 5;
                    Debug.Log($"[EditorAutoCapture] beat frame={Time.frameCount} t={Time.time:0.0} rt={Time.realtimeSinceStartup:0.0} paused={EditorApplication.isPaused} focused={UnityEditorInternal.InternalEditorUtility.isApplicationActive}");
                }
                EditorApplication.QueuePlayerLoopUpdate();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
            return;
        }
        EditorApplication.update -= Poll;
        Debug.Log("[EditorAutoCapture] " + (done ? "capture finished" : "timed out"));
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () => EditorApplication.Exit(done ? 0 : 1);
    }

    private static string CaptureDir()
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "-captureDir");
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.GetFullPath("Capture");
    }
}
