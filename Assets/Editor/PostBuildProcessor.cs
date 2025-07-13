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
        var projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projPath);

#if UNITY_2020_1_OR_NEWER
        string targetGuid = proj.GetUnityMainTargetGuid();
        string frameworkTarget = proj.GetUnityFrameworkTargetGuid();
#else
        string targetGuid = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif

        // 自動署名とチームID設定
        proj.SetTeamId(targetGuid, "daiki watanabe"); // Apple DeveloperのTeam ID
        proj.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Automatic");
//         Debug.Log("targetGuid " + targetGuid);
//         // フラグを付けたいソースファイル
//         string fileName = "MusicLibraryMediaPicker.mm";
//         string filePath = Path.Combine("Libraries", "MusicPlayerMediaPicker", "Plugins", "iOS", fileName);

//         Debug.LogWarning("configNamesCount" + proj.BuildConfigNames().Count());
//         foreach (string configName in proj.BuildConfigNames())
//         {
//             Debug.LogWarning("configName" + configName);
//             string configGuid = proj.BuildConfigByName(targetGuid, configName);
//             if (string.IsNullOrEmpty(configGuid))
//             {
//                 Debug.LogWarning($"Config '{configName}' が見つかりませんでした。");
//                 continue;
//             }

//             proj.AddBuildPropertyForConfig(configGuid, "OTHER_CFLAGS", "-fno-objc-arc");
//         }

        // 保存
        proj.WriteToFile(projPath);
    }
}
