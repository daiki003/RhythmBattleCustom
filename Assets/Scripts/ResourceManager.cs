using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResourceManager
{
    private static Dictionary<string, GameObject> _loadedPrefabList = new Dictionary<string, GameObject>();

    private static string _prefabPath = "Prefabs/";

    public static GameObject LoadPrefab(string prefabName)
    {
        if (_loadedPrefabList.ContainsKey(prefabName))
        {
            return _loadedPrefabList[prefabName];
        }
        else
        {
            var prefab = Resources.Load<GameObject>(_prefabPath + prefabName);
            _loadedPrefabList.Add(prefabName, prefab);
            return prefab;
        }
    }
}
