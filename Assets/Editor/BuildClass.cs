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
				"~/RhythumBattle_iOS/myUnityProj.exe",	//!< 出力先
				BuildTarget.iOS,		//!< ビルド対象プラットフォーム
				BuildOptions.None			//!< ビルドオプション
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