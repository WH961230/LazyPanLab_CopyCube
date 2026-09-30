using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 通用全局总览（包内，F2）：只把 StreamingAssets/Csv 下的 SceneConfig / ObjConfig 当纯文本读，
    /// 按场景—实体—行为三层树列出来，不碰任何业务类型。
    /// 每行实体有个“实体设置”按钮，走 EntityGraphHook（新项目弹通用图，老项目弹完整图）。
    /// Csv 还没拷贝时显示中文提示，不静默。
    /// </summary>
    public class GenericGlobalMapWindow : EditorWindow {
        private Vector2 _scroll;
        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
        private List<SceneRow> _scenes = new List<SceneRow>();
        private List<EntityRow> _entities = new List<EntityRow>();
        private string _hint = "";

        private class SceneRow {
            public string Code;
            public string Name;
        }

        private class EntityRow {
            public string Sign;
            public string Flow;
            public string Type;
            public string Chinese;
            public string[] Behaviours = new string[0];
        }

        [MenuItem("Tools/LazyPan/全局总览 _F2", priority = 0)]
        public static void Open() {
            var window = GetWindow<GenericGlobalMapWindow>("全局总览");
            window.Reload();
            window.Show();
        }

        private void OnEnable() {
            Reload();
        }

        private void Reload() {
            _scenes.Clear();
            _entities.Clear();
            _hint = "";
            string csvDir = Path.Combine(Application.streamingAssetsPath, "Csv");
            if (!Directory.Exists(csvDir)) {
                _hint = "还没看到 StreamingAssets/Csv，请先跑引导第一、二步（建目录+拷贝），再按 F2。";
                return;
            }
            _scenes = ReadSceneConfig(Path.Combine(csvDir, "SceneConfig.csv"));
            _entities = ReadObjConfig(Path.Combine(csvDir, "ObjConfig.csv"));
            if (_scenes.Count == 0 && _entities.Count == 0) {
                _hint = "Csv 是空的或还没拷贝，请先跑引导第二步拷贝，再按 F2。";
            }
        }

        private static List<SceneRow> ReadSceneConfig(string path) {
            var list = new List<SceneRow>();
            foreach (string[] cols in ReadCsvRows(path)) {
                if (cols.Length < 2 || string.IsNullOrWhiteSpace(cols[0])) {
                    continue;
                }
                list.Add(new SceneRow { Code = cols[0].Trim(), Name = cols[1].Trim() });
            }
            return list;
        }

        private static List<EntityRow> ReadObjConfig(string path) {
            var list = new List<EntityRow>();
            foreach (string[] cols in ReadCsvRows(path)) {
                if (cols.Length < 6 || string.IsNullOrWhiteSpace(cols[0])) {
                    continue;
                }
                string[] behaviours = cols[5].Split('|');
                var cleaned = new List<string>();
                foreach (string b in behaviours) {
                    string t = b.Trim();
                    if (!string.IsNullOrEmpty(t)) {
                        cleaned.Add(t);
                    }
                }
                list.Add(new EntityRow {
                    Sign = cols[0].Trim(),
                    Flow = cols[1].Trim(),
                    Type = cols[2].Trim(),
                    Chinese = cols[3].Trim(),
                    Behaviours = cleaned.ToArray()
                });
            }
            return list;
        }

        private static List<string[]> ReadCsvRows(string path) {
            var rows = new List<string[]>();
            if (!File.Exists(path)) {
                return rows;
            }
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 3; i < lines.Length; i++) {
                if (string.IsNullOrWhiteSpace(lines[i])) {
                    continue;
                }
                rows.Add(lines[i].Split(','));
            }
            return rows;
        }

        private void OnGUI() {
            if (!string.IsNullOrEmpty(_hint)) {
                EditorGUILayout.HelpBox(_hint, MessageType.Info);
                if (GUILayout.Button("重新加载")) {
                    Reload();
                }
                return;
            }
            if (GUILayout.Button("刷新")) {
                Reload();
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (SceneRow scene in _scenes) {
                string key = "scene:" + scene.Code;
                if (!_foldouts.ContainsKey(key)) {
                    _foldouts[key] = true;
                }
                _foldouts[key] = EditorGUILayout.Foldout(_foldouts[key], $"{scene.Name}（{scene.Code}）", true);
                if (!_foldouts[key]) {
                    continue;
                }
                EditorGUI.indentLevel++;
                foreach (EntityRow e in _entities) {
                    if (e.Flow != scene.Name && e.Flow != scene.Code) {
                        continue;
                    }
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{e.Chinese}（{e.Sign}）[{e.Type}]", GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("实体设置", GUILayout.Width(80))) {
                        EntityGraphHook.OpenEntityGraph?.Invoke(e.Sign, e.Behaviours);
                    }
                    GUILayout.EndHorizontal();
                    EditorGUI.indentLevel++;
                    foreach (string b in e.Behaviours) {
                        GUILayout.Label("· " + b);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
