using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// BehaviourName 字段绘制器 点击按钮弹出 BehaviourConfig.csv 中所有行为中文名供选择 代替手填
/// </summary>
[CustomPropertyDrawer(typeof(LazyPan.BehaviourNameAttribute))]
public class BehaviourNameDrawer : PropertyDrawer {
    static List<string> nameCache;
    static System.DateTime nameStamp;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var names = LoadNames();
        if (names.Count == 0) {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        var current = property.stringValue;
        string display = string.IsNullOrEmpty(current) ? "<选择行为>" : current;

        var labelWidth = position.width * 0.4f;
        var labelRect = new Rect(position.x, position.y, labelWidth, position.height);
        var buttonRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, position.height);
        if (buttonRect.width < 40f) {
            labelRect.width = 0;
            buttonRect.x = position.x;
            buttonRect.width = position.width;
        }

        using (new EditorGUI.DisabledScope(true))
            GUI.Label(labelRect, label);
        if (GUI.Button(buttonRect, display)) {
            var menu = new GenericMenu();
            foreach (var name in names) {
                var tmp = name;
                menu.AddItem(new GUIContent(tmp), tmp == current, () => {
                    property.stringValue = tmp;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
    }

    static List<string> LoadNames() {
        var path = System.IO.Path.Combine(Application.streamingAssetsPath, "Csv", "BehaviourConfig.csv");
        try {
            var stamp = System.IO.File.GetLastWriteTimeUtc(path);
            if (nameCache != null && stamp == nameStamp)
                return nameCache;

            nameCache = new List<string>();
            nameStamp = stamp;
            var seen = new HashSet<string>();
            var lines = System.IO.File.ReadAllLines(path);
            // BehaviourConfig.csv 只有2行表头(列名行/类型行 无中文行)
            for (int i = 2; i < lines.Length; i++) {
                var cols = lines[i].Split(',');
                if (cols.Length < 2)
                    continue;
                var name = cols[1].Trim();
                if (string.IsNullOrEmpty(name) || !seen.Add(name))
                    continue;
                nameCache.Add(name);
            }
            nameCache.Sort();
        } catch {
            nameCache = null;
            return new List<string>();
        }
        return nameCache;
    }
}
