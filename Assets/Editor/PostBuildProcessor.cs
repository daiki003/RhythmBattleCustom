// Assets/Editor/PostBuildProcessor.cs
using System.IO;
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

#if UNITY_2019_3_OR_NEWER
        string targetGuid = proj.GetUnityFrameworkTargetGuid();
#else
        string targetGuid = proj.TargetGuidByName("UnityFramework");
#endif

        // フラグを付けたいソースファイル
        string fileName = "MusicLibraryMediaPicker.mm";
        string filePath = Path.Combine("Libraries", "Unity", fileName);

        // 対象ファイルの GUID を取得
        var fileGuid = proj.FindFileGuidByProjectPath(filePath);
        if (!string.IsNullOrEmpty(fileGuid))
        {
            proj.AddBuildPropertyForConfig(targetGuid, "OTHER_CFLAGS", "-fno-objc-arc");
        }

        // 保存
        proj.WriteToFile(projPath);
        Debug.Log("compilerFlags が UnityFramework に追加されました" + "\n" + fileName + "\n" + filePath + "\n" + fileGuid);
    }
}
