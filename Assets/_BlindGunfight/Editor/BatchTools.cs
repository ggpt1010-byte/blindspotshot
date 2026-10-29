using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BlindGunfight.EditorTools
{
    /// <summary>
    /// 명령줄에서 Unity 를 화면 없이 돌려 씬 생성과 Windows 빌드를 한 번에 한다.
    /// 예: Unity.exe -batchmode -quit -projectPath . -executeMethod BlindGunfight.EditorTools.BatchTools.BuildAll -logFile build.log
    /// 빌드 결과: Builds/Windows/BlindGunfight.exe ("-autocapture" 로 실행하면 자동 촬영)
    /// </summary>
    public static class BatchTools
    {
        private const string WindowsPlayerPath = "Builds/Windows/BlindGunfight.exe";

        public static void BuildAll()
        {
            PrototypeSceneBuilder.Build();
            BuildWindows();
        }

        [MenuItem("BlindGunfight/Build Windows Player")]
        public static void BuildWindows()
        {
            var scenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && File.Exists(scene.path)) scenes.Add(scene.path);
            }
            if (scenes.Count == 0)
            {
                Fail("Build Settings 에 씬이 없습니다. 먼저 Build Prototype Scene 을 실행하세요.");
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = WindowsPlayerPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"[BlindGunfight] 빌드 성공: {Path.GetFullPath(WindowsPlayerPath)} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:0}s)");
            else
                Fail($"빌드 실패: {summary.result}, 오류 {summary.totalErrors}개");
        }

        private static void Fail(string message)
        {
            Debug.LogError("[BlindGunfight] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
