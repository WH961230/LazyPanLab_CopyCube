using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 通用全局总览（包内，F2）：只把 StreamingAssets 下的 Csv 当纯文本读，按 场景-实体-行为 三层树展示。
    /// 不引用任何业务类型，所以不违反“包内无业务”；点实体行通过 EntityGraphHook 打开实体图
    /// （新项目走包内通用图，跑完引导拷贝样板后自动走 Assets 完整图）。
    /// 老项目 Assets 里的图版总览改走“全局总览（图）”菜单，F2 快捷键只留给这一个，避免重复注册打架。
    /// </summary>
    public class GenericGlobalMapWindow : EditorWindow {
        private Vector2 _scroll;
        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
        private List<SceneRow> _scenes = new List<SceneRow>();
        private List<EntityRow> _entities = new List<EntityRow>();
        private string _notice = "";

        private class SceneRow {
            public string Code;
            public string Name;
        }

        private class EntityRow {
            public string Sign;
            public string Flow;
            public string Type;
            public string CnName;
            public string[] Behaviours;
        }

        [MenuItem("Tools/LazyPan/全局总览 _F2", priority = 0)]
        public static void Open() {
            var window = GetWindow<GenericGlobalMapWindow>("全局总览");
            window.Reload();
            window.Show();
        }

        private void OnFocus() {
            Reload();
        }

        private void Reload() {
            _notice = "";
            _scenes = ReadCsv("SceneConfig", cols => cols.Length >= 2
                ? new SceneRow { Code = cols[0].Trim(), Name = cols[1].Trim() }
                : null);
            _entities = ReadCsv("ObjConfig", cols => cols.Length >= 4 && !string.IsNullOrWhiteSpace(cols[0])
                ? new EntityRow {
                    Sign = cols[0].Trim(),
                    Flow = cols.Length > 1 ? cols[1].Trim() : "",
                    Type = cols.Length > 2 ? cols[2].Trim() : "",
                    CnName = cols[3].Trim(),
                    Behaviours = cols.Length > 5
                        ? cols[5].Split('|').Select(b => b.Trim()).Where(b => !string.IsNullOrEmpty(b)).ToArray()
                        : new string[0]
                }
                : null);
            if (_scenes.Count == 0 && _entities.Count == 0) {
                _notice = "没读到 Csv（Assets/StreamingAssets/Csv 下没有 SceneConfig.csv / ObjConfig.csv），请先跑引导第一、二步建目录并拷贝样板。";
            }
        }

        private static List<T> ReadCsv<T>(string fileName, System.Func<string[], T> map) where T : class {
            var result = new List<T>();
            string path = Path.Combine(Application.streamingAssetsPath, "Csv", fileName + ".csv");
            if (!File.Exists(path)) {
                return result;
            }
            string[] lines;
            try {
                lines = File.ReadAllLines(path, Encoding.UTF8);
            } catch {
                return result;
            }
            for (int i = 3; i < lines.Length; i++) {
                if (string.IsNullOrWhiteSpace(lines[i])) {
                    continue;
                }
                T row = map(lines[i].Split(','));
                if (row != null) {
                    result.Add(row);
                }
            }
            return result;
        }

        private void OnGUI() {
            if (GUILayout.Button("刷新")) {
                Reload();
            }
            if (!string.IsNullOrEmpty(_notice)) {
                EditorGUILayout.HelpBox(_notice, MessageType.Info);
                return;
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var scene in _scenes) {
                string key = "scene:" + scene.Code;
                if (!_foldouts.TryGetValue(key, out bool open)) {
                    open = true;
                }
                bool next = EditorGUILayout.Foldout(open, $"{scene.Name}（{scene.Code}）", true);
                _foldouts[key] = next;
                if (!next) {
                    continue;
                }
                EditorGUI.indentLevel++;
                var inScene = _entities.Where(e => e.Flow == scene.Name || e.Flow == scene.Code).ToList();
                if (inScene.Count == 0) {
                    EditorGUILayout.LabelField("（该场景下暂无实体）");
                }
                foreach (var entity in inScene) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{entity.CnName}（{entity.Sign}）", GUILayout.Width(320));
                    if (GUILayout.Button("实体设置", GUILayout.Width(80))) {
                        EntityGraphHook.OpenEntityGraph?.Invoke(entity.Sign, entity.Behaviours);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("行为：" + (entity.Behaviours.Length > 0
                        ? string.Join(" | ", entity.Behaviours)
                        : "（无）"));
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            // 场景表里没登记但实体表里有的，按流名称兜底再列一遍，不丢东西
            var known = new HashSet<string>(_scenes.SelectMany(s => new[] { s.Name, s.Code }));
            var orphans = _entities.Where(e => !known.Contains(e.Flow)).ToList();
            if (orphans.Count > 0) {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("未归档实体（流名称不在场景表里）：", EditorStyles.boldLabel);
                foreach (var entity in orphans) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{entity.CnName}（{entity.Sign}）[{entity.Flow}]", GUILayout.Width(320));
                    if (GUILayout.Button("实体设置", GUILayout.Width(80))) {
                        EntityGraphHook.OpenEntityGraph?.Invoke(entity.Sign, entity.Behaviours);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
