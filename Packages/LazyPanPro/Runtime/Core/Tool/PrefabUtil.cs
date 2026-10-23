using UnityEngine;

namespace LazyPan {
    public class PrefabUtil {
        public static GameObject FindEntityPrefab(string sourceSign) {
#if UNITY_EDITOR
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets(sourceSign + " t:Prefab")) {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !System.IO.Path.GetFileNameWithoutExtension(path)
                    .Equals(sourceSign, System.StringComparison.Ordinal)) continue;
                var pf = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (pf != null) return pf;
            }
#endif
            return null;
        }
    }
}