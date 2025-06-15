using UnityEditor.iOS.Xcode;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class PBXTargetInspector
{
    [MenuItem("Tools/Show All Xcode Targets")]
    public static void ShowAllTargets()
    {
        string buildPath = "YOUR_IOS_BUILD_PATH_HERE"; // 例: "build/ios"
        string projPath = PBXProject.GetPBXProjectPath(buildPath);
        var proj = new PBXProject();
        proj.ReadFromFile(projPath);

#if UNITY_2019_3_OR_NEWER
        string mainTarget = proj.GetUnityMainTargetGuid();
        string frameworkTarget = proj.GetUnityFrameworkTargetGuid();
#else
        string mainTarget = proj.TargetGuidByName("Unity-iPhone");
        string frameworkTarget = proj.TargetGuidByName("UnityFramework");
#endif

        // Debug.Log($"🔹 Main Target GUID: {mainTarget} -> Name: {proj.GetTargetName(mainTarget)}");
        // Debug.Log($"🔹 Framework GUID:   {frameworkTarget} -> Name: {proj.GetTargetName(frameworkTarget)}");

        // 手動でターゲット名を指定して GUID を取得してみる
        string unityMain = proj.TargetGuidByName("Unity-iPhone");
        string unityFramework = proj.TargetGuidByName("UnityFramework");

        Debug.Log($"Unity-iPhone GUID: {unityMain}");
        Debug.Log($"UnityFramework GUID: {unityFramework}");

        // 追加：全 GUID を調べる
        var allGuids = proj.GetType()
            .GetMethod("GetAllTargetGuids", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        if (allGuids != null)
        {
            // var guids = (string[])allGuids.Invoke(proj, null);
            // foreach (var guid in guids)
            // {
            //     string name = proj.GetTargetName(guid);
            //     Debug.Log($"🧩 GUID: {guid} → Name: {name}");
            // }
        }
        else
        {
            Debug.LogWarning("⚠️ proj.GetAllTargetGuids() はこの環境では使えません。");
        }
    }
}
