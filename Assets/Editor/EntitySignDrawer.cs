using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// EntitySign 字段绘制器 点击按钮弹出 ObjConfig.csv 中所有实体 Sign 供选择 代替手填
/// 图资产改动走 Property 修回路自动标脏 并写入磁盘；Setting 资产同享
/// </summary>
[CustomPropertyDrawer(typeof(LazyPan.EntitySignAttribute))]
public class EntitySignDrawer : PropertyDrawer {
    static List<KeyValuePair<string, string>> signCache;
    static HashSet<string> duplicatedNames;
    static System.DateTime signStamp;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var signs = LoadSigns();
        if (signs.Count == 0) {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        var current = property.stringValue;
        string display;
        if (string.IsNullOrEmpty(current))
            display = "<选择实体>";
        else {
            display = current;
            foreach (var kv in signs) {
                if (kv.Key == current) {
                    display = MenuLabel(kv.Value, kv.Key);
                    break;
                }
            }
        }

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
            menu.AddItem(new GUIContent("<留空/自己>"), string.IsNullOrEmpty(current), () => {
                property.stringValue = "";
                property.serializedObject.ApplyModifiedProperties();
            });
            foreach (var kv in signs) {
                var tmp = kv;
                menu.AddItem(new GUIContent(MenuLabel(tmp.Value, tmp.Key)), tmp.Key == current, () => {
                    property.stringValue = tmp.Key;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
    }

    /// <summary>
    /// 菜单标签 默认只显示中文名 仅当名字重复时才补 Sign 以区分
    /// </summary>
    static string MenuLabel(string name, string sign) {
        if (duplicatedNames != null && duplicatedNames.Contains(name))
            return $"{name} ({sign})";
        return string.IsNullOrEmpty(name) ? sign : name;
    }

    static List<KeyValuePair<string, string>> LoadSigns() {
        var path = Path.Combine(Application.streamingAssetsPath, "Csv", "ObjConfig.csv");
        try {
            var stamp = File.GetLastWriteTimeUtc(path);
            if (signCache != null && stamp == signStamp)
                return signCache;

            signCache = new List<KeyValuePair<string, string>>();
            signStamp = stamp;
            var seen = new HashSet<string>();
            var nameCount = new Dictionary<string, int>();
            var lines = File.ReadAllLines(path);
            // 前3行为表头(Sign行/类型行/中文行)
            for (int i = 3; i < lines.Length; i++) {
                var cols = lines[i].Split(',');
                if (cols.Length < 4)
                    continue;
                var sign = cols[0].Trim();
                var name = cols[3].Trim();
                if (nameCount.ContainsKey(name))
                    nameCount[name]++;
                else
                    nameCount[name] = 1;
                if (string.IsNullOrEmpty(sign) || !seen.Add(sign))
                    continue;
                signCache.Add(new KeyValuePair<string, string>(sign, name));
            }

            duplicatedNames = new HashSet<string>();
            foreach (var kv in nameCount) {
                if (kv.Value > 1)
                    duplicatedNames.Add(kv.Key);
            }
        } catch {
            signCache = null;
            duplicatedNames = null;
            return new List<KeyValuePair<string, string>>();
        }
        return signCache;
    }
}