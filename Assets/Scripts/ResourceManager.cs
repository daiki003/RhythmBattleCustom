using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResourceManager
{
    private static Dictionary<string, GameObject> _loadedPrefabDic = new Dictionary<string, GameObject>();
    private static Dictionary<string, Sprite> _loadedSpriteDic = new Dictionary<string, Sprite>();

    private static string _basePrefabPath = "Prefabs/";

    public static GameObject LoadPrefab(string prefabPath)
    {
        if (_loadedPrefabDic.ContainsKey(prefabPath))
        {
            return _loadedPrefabDic[prefabPath];
        }
        else
        {
            var prefab = Resources.Load<GameObject>(_basePrefabPath + prefabPath);
            _loadedPrefabDic.Add(prefabPath, prefab);
            return prefab;
        }
    }

    public static Sprite LoadSprite(string spritePath)
    {
        if (_loadedSpriteDic.ContainsKey(spritePath))
        {
            return _loadedSpriteDic[spritePath];
        }
        else
        {
            var sprite = Resources.Load<Sprite>(spritePath);
            _loadedSpriteDic.Add(spritePath, sprite);
            return sprite;
        }
    }

    public static Sprite LoadSpriteWithDummyEnemy(string spritePath)
    {
        var sprite = LoadSprite(spritePath);
        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>("Enemy/Dummy");
        }
        return sprite;
    }
}
