using UnityEditor;
using UnityEngine;

public static class AssetDatabaseHelper
{
    public static string GetAssetGuid(Object asset)
    {
        if (asset == null) return string.Empty;
        return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
    }

    public static T LoadAssetFromGuid<T>(string guid) where T : Object
    {
        if (string.IsNullOrEmpty(guid)) return null;
        string path = AssetDatabase.GUIDToAssetPath(guid);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    public static bool IsValidGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return false;
        string path = AssetDatabase.GUIDToAssetPath(guid);
        return !string.IsNullOrEmpty(path) && AssetDatabase.LoadAssetAtPath<Object>(path) != null;
    }
}
