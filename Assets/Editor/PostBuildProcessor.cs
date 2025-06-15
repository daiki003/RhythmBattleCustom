// Assets/Editor/PostBuildProcessor.cs
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public class PostBuildProcessor
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget != BuildTarget.iOS) return;

        // project.pbxproj のパス
        var projPath = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj/project.pbxproj");

        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projPath);
        Debug.Log("projPath " + projPath);

#if UNITY_2019_3_OR_NEWER
        string targetGuid = proj.GetUnityFrameworkTargetGuid();
#else
        string targetGuid = proj.TargetGuidByName("UnityFramework");
#endif

        Debug.Log("targetGuid " + targetGuid);
        // フラグを付けたいソースファイル
        string fileName = "MusicLibraryMediaPicker.mm";
        string filePath = Path.Combine("Libraries", "MusicPlayerMediaPicker", "Plugins", "iOS", fileName);

        Debug.LogWarning("configNamesCount" + proj.BuildConfigNames().Count());
        foreach (string configName in proj.BuildConfigNames())
        {
            Debug.LogWarning("configName" + configName);
            string configGuid = proj.BuildConfigByName(targetGuid, configName);
            if (string.IsNullOrEmpty(configGuid))
            {
                Debug.LogWarning($"Config '{configName}' が見つかりませんでした。");
                continue;
            }

            proj.AddBuildPropertyForConfig(configGuid, "OTHER_CFLAGS", "-fno-objc-arc");
        }

        // 保存
        proj.WriteToFile(projPath);
    }
}
