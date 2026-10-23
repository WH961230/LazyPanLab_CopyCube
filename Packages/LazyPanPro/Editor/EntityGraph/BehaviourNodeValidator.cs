using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为节点手动检测：只读配置不改东西，红=本节点缺的，黄=跨实体提醒。
    /// 结果只写节点的检测灯/检测信息，不写Setting不污染配置。
    /// </summary>
    public static class BehaviourNodeValidator {
        public static void Validate(GraphProcessor.BaseGraph graph, string entitySign, out int pass, out int fail) {
            pass = 0;
            fail = 0;
            if (graph == null) {
                return;
            }
            foreach (var node in graph.nodes.OfType<BehaviourGraphNode>()) {
                string reason;
                bool ok = CheckOne(node, entitySign, out reason);
                node.检测灯 = ok ? 1 : 2;
                node.检测信息 = reason ?? "";
                if (ok) {
                    pass++;
                } else {
                    fail++;
                }
            }
        }

        public static void Clear(GraphProcessor.BaseGraph graph) {
            if (graph == null) {
                return;
            }
            foreach (var node in graph.nodes.OfType<BehaviourGraphNode>()) {
                node.检测灯 = 0;
                node.检测信息 = "";
            }
        }

        public static GameObject FindEntityPrefab(string sourceSign) {
            if (string.IsNullOrEmpty(sourceSign)) {
                return null;
            }
            foreach (string guid in AssetDatabase.FindAssets(sourceSign + " t:Prefab")) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) {
                    continue;
                }
                if (!System.IO.Path.GetFileNameWithoutExtension(path).Equals(sourceSign, System.StringComparison.Ordinal)) {
                    continue;
                }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) {
                    return prefab;
                }
            }
            return null;
        }

        static bool CheckOne(BehaviourGraphNode node, string entitySign, out string reason) {
            reason = "";
            var configField = node.GetType().GetField("Config");
            if (configField == null) {
                return true;
            }
            object cfg = configField.GetValue(node);
            if (cfg == null) {
                reason = "Config为空";
                return false;
            }
            var type = cfg.GetType();
            var sourceField = type.GetField("SourceSign");
            if (sourceField != null && string.IsNullOrEmpty(sourceField.GetValue(cfg) as string)) {
                reason = "SourceSign为空";
                return false;
            }
            if (type.Name == "InputWASDMoveSettingData") {
                string action = (type.GetField("InputControlSign")?.GetValue(cfg) as string) ?? "";
                float speed = Convert.ToSingle(type.GetField("MoveSpeed")?.GetValue(cfg) ?? 0f);
                if (string.IsNullOrEmpty(action)) {
                    reason = "InputControlSign为空";
                    return false;
                }
                if (speed <= 0f) {
                    reason = "MoveSpeed须>0";
                    return false;
                }
                string sourceSign = (type.GetField("SourceSign")?.GetValue(cfg) as string) ?? "";
                var prefab = FindEntityPrefab(sourceSign);
                if (prefab != null) {
                    bool hasCC = false;
                    var comp = prefab.GetComponent<Comp>();
                    if (comp != null && comp.CharacterControllers != null) {
                        foreach (var d in comp.CharacterControllers) {
                            if (d != null && d.Sign == "CharacterController" && d.Controller != null) {
                                hasCC = true;
                                break;
                            }
                        }
                    }
                    if (!hasCC) {
                        reason = $"预制体 {sourceSign} 的 Comp 里没有挂载 CharacterController（Sign=CharacterController），先去 Comp 的 CharacterControllers 里登记再跑";
                        return false;
                    }
                    bool hasBody = false;
                    if (comp != null && comp.Transforms != null) {
                        foreach (var t in comp.Transforms) {
                            if (t != null && t.Sign == "Body" && t.Tran != null) {
                                hasBody = true;
                                break;
                            }
                        }
                    }
                    if (!hasBody) {
                        reason = $"预制体 {sourceSign} 的 Comp 里没有登记 Transform:Body（Sign=Body），先去 Comp 的 Transforms 里登记再跑，不然不会转身";
                        return false;
                    }
                }
                return true;
            }
            bool hasNumber = false;
            bool allZero = true;
            foreach (var f in type.GetFields()) {
                if (f.FieldType == typeof(float) || f.FieldType == typeof(int)) {
                    hasNumber = true;
                    double v = Convert.ToDouble(f.GetValue(cfg));
                    if (Math.Abs(v) > 0.0001) {
                        allZero = false;
                        break;
                    }
                }
            }
            if (hasNumber && allZero) {
                reason = "数值全为0";
                return false;
            }
            return true;
        }
    }
}
