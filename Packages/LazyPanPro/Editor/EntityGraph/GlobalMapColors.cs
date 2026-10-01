using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 全局总览三类节点颜色 Project 设置里实时调 总览图即时生效(只读显示 不进运行时)
    /// </summary>
    [CreateAssetMenu(fileName = "GlobalMapColors", menuName = "LazyPan/GlobalMapColors")]
    public class GlobalMapColors : ScriptableObject {
        const string assetPath = "Assets/LazyPan/Bundles/Configs/GlobalMapColors.asset";

        [Header("开始 Start 节点颜色")]
        public Color Start = new Color(0.60f, 0.60f, 0.60f, 1f);

        [Header("启动器 Launch 节点颜色")]
        public Color Launch = new Color(0.65f, 0.45f, 0.90f, 1f);

        [Header("场景 Flow 节点颜色")]
        public Color Flow = new Color(0.30f, 0.55f, 0.95f, 1f);

        [Header("实体 Entity 节点颜色")]
        public Color Entity = new Color(0.25f, 0.75f, 0.45f, 1f);

        [Header("行为默认颜色(新行为首次出现时用)")]
        public Color BehaviourDefault = new Color(0.95f, 0.65f, 0.25f, 1f);

        [Header("各行为颜色 同名行为共用一个")]
        public List<BehaviourColorEntry> BehaviourColors = new List<BehaviourColorEntry>();

#if UNITY_EDITOR
        static GlobalMapColors cached;

        /// <summary>按行为中文名取颜色 仅复用行为走这里 独占行为直接用默认 不进列表</summary>
        public Color GetBehaviourColor(string behaviourName) {
            if (!string.IsNullOrEmpty(behaviourName)) {
                foreach (var entry in BehaviourColors) {
                    if (entry != null && entry.Name == behaviourName)
                        return entry.Color;
                }
                BehaviourColors.Add(new BehaviourColorEntry { Name = behaviourName, Color = BehaviourDefault });
                UnityEditor.EditorUtility.SetDirty(this);
            }
            return BehaviourDefault;
        }

        /// <summary>编辑器读颜色 资产不存在自动建一份默认 不阻塞总览</summary>
        public static GlobalMapColors Load() {
            if (cached != null)
                return cached;
            cached = UnityEditor.AssetDatabase.LoadAssetAtPath<GlobalMapColors>(assetPath);
            if (cached == null) {
                cached = CreateInstance<GlobalMapColors>();
                var folder = System.IO.Path.GetDirectoryName(assetPath);
                if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                    UnityEditor.AssetDatabase.CreateFolder("Assets/LazyPan/Bundles", "Configs");
                UnityEditor.AssetDatabase.CreateAsset(cached, assetPath);
                UnityEditor.AssetDatabase.SaveAssets();
            }
            return cached;
        }
#endif
    }

    /// <summary>行为颜色条目 中文名 → 颜色</summary>
    [System.Serializable]
    public class BehaviourColorEntry {
        [BehaviourName]
        public string Name = "";
        public Color Color = Color.white;
    }
}
