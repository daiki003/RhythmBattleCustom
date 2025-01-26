using UnityEditor;
using UnityEngine;

public class BuildClass
{
	public static void Build()
	{
		// ビルド対象シーンリスト
		string[] sceneList = {
			"./Assets/Scenes/Main.unity"
		};


		// 実行
		var buildReport = BuildPipeline.BuildPlayer(
				sceneList,							//!< ビルド対象シーンリスト
				"C:/project/bin/myUnityProj.exe",	//!< 出力先
				BuildTarget.StandaloneWindows,		//!< ビルド対象プラットフォーム
				BuildOptions.Development			//!< ビルドオプション
		);


		// 結果出力
		if(buildReport == null)
        {
            Debug.LogError("[Error!] ");
        }
		else
        {
            Debug.Log("[Success!]");
        }
	}
}