using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LazyPan {
    /// <summary>
    /// 行为参数便签：反射读 Behaviour 头上的 MemoDoc（完整用户说明优先显示）/ RequiredPayload / RequiredModules，
    /// 图节点 tooltip 直接显示，不看源码、不手写文档。
    /// 全结果按行为缓存（反射扫全程序集+翻资产只跑一次），改了说明点刷新按钮清缓存。
    /// </summary>
    public static class BehaviourPayloadDoc {
        static readonly Dictionary<string, string> sCache = new Dictionary<string, string>();
        static Dictionary<string, Type> sTypeMap;

        /// <summary>行为类名 → Setting 资产名，资产里的 MemoDoc 优先显示，改说明只改资产</summary>
        static readonly Dictionary<string, string> sSettingAsset = new Dictionary<string, string> {
            { "Behaviour_Event_Death", "DeathSetting" },
            { "Behaviour_Auto_InputWASDMove", "InputWASDMoveSetting" },
            { "Behaviour_Auto_BodyExpand", "BodyExpandSetting" },
            { "Behaviour_Auto_ContactDamage", "ContactDamageSetting" },
            { "Behaviour_Auto_EntityTriggerController", "EntityTriggerControllerSetting" },
            { "Behaviour_Auto_FlyTrack", "FlyTrackSetting" },
            { "Behaviour_Auto_FollowHolder", "FollowHolderSetting" },
            { "Behaviour_Auto_LifeTimeout", "LifeTimeoutSetting" },
            { "Behaviour_Auto_TrackingEntityByNavMeshAgent", "TrackingEntitySetting" },
            { "Behaviour_Auto_Knockback", "KnockbackSetting" },            { "Behaviour_Event_BeginLogo", "BeginLogoSetting" },
            { "Behaviour_Event_DelayGenerateEntity", "DelayGenerateEntitySetting" },
            { "Behaviour_Event_EntityUIBinder", "EntityUIBinderSetting" },
            { "Behaviour_Event_EquipmentMountManager", "EquipmentMountSetting" },
            { "Behaviour_Event_ParamValue", "ParamValueSetting" },
            { "Behaviour_Event_StageProgress", "StageProgressSetting" },
            { "Behaviour_Event_TeleportFlow", "TeleportFlowSetting" },
            { "Behaviour_Event_UIPickOneOfThree", "UIPickOneOfThreeSetting" },
            { "Behaviour_Event_UIStatusDisplay", "UIStatusDisplaySetting" },
            { "Behaviour_Event_WaveManager", "WaveManagerSetting" },
            { "Behaviour_Event_WeaponFire", "WeaponSetting" },
            { "Behaviour_Auto_Teleportation", "TeleportationSetting" },
        };

        public static void ClearCache() {
            sCache.Clear();
        }

        public static bool TryGetBehaviourType(string behaviourSign, out Type t) {
            BuildTypeMap();
            return sTypeMap.TryGetValue(behaviourSign, out t);
        }

        static void BuildTypeMap() {
            if (sTypeMap != null) {
                return;
            }

            sTypeMap = new Dictionary<string, Type>();
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) {
                Type[] types;
                try { types = a.GetTypes(); } catch { continue; }
                foreach (var x in types) {
                    if (!sTypeMap.ContainsKey(x.Name)) {
                        sTypeMap[x.Name] = x;
                    }
                }
            }
        }

        public static string Get(string behaviourSign) {
            if (string.IsNullOrEmpty(behaviourSign)) {
                return "参数便签：未知行为";
            }
            if (sCache.TryGetValue(behaviourSign, out string hit)) {
                return hit;
            }
            string text = Build(behaviourSign);
            sCache[behaviourSign] = text;
            return text;
        }

        static string Build(string behaviourSign) {
            BuildTypeMap();
            sTypeMap.TryGetValue(behaviourSign, out Type t);
            if (t == null) {
                return $"参数便签：找不到行为 {behaviourSign}";
            }

            // 标题用人话（BehaviourConfig.csv 的中文名），取不到才回退类名
            string displayName = behaviourSign;
            try {
                BehaviourConfig.GetKeys();
                BehaviourConfig cfg = BehaviourConfig.Get(behaviourSign);
                if (cfg != null && !string.IsNullOrEmpty(cfg.Name)) {
                    displayName = cfg.Name.Trim();
                }
            } catch { }

            var sb = new StringBuilder();
            sb.AppendLine($"【{displayName}】");

            FieldInfo memo = t.GetField("MemoDoc", BindingFlags.Public | BindingFlags.Static);
            string memoText = memo?.GetValue(null) as string;
            // 说明允许在各自 Setting 资产里自由修改，资产非空优先用资产的
            if (sSettingAsset.TryGetValue(behaviourSign, out string settingName)) {
                string custom = LoadMemoByAssetName(settingName);
                if (!string.IsNullOrWhiteSpace(custom)) {
                    memoText = custom;
                }
            }
            if (!string.IsNullOrEmpty(memoText)) {
                sb.AppendLine(memoText.Trim());
                sb.AppendLine();
            }

            FieldInfo payload = t.GetField("RequiredPayload", BindingFlags.Public | BindingFlags.Static);
            object[] defs = payload?.GetValue(null) as object[];
            if (defs == null || defs.Length == 0) {
                sb.AppendLine("— 外部参数 —");
                sb.AppendLine("无，本行为开箱即用，无需配置。");
            } else {
                sb.AppendLine("— 需配参数（去 ParamValue 里配）—");
                foreach (object d in defs) {
                    if (d is PayloadContractDef c) {
                        sb.AppendLine($"- {c.Sign}：{c.ValueType}，默认 {DefaultOf(c)}");
                    }
                }
            }

            FieldInfo mods = t.GetField("RequiredModules", BindingFlags.Public | BindingFlags.Static);
            string[] m = mods?.GetValue(null) as string[];
            if (m != null && m.Length > 0) {
                sb.AppendLine();
                sb.AppendLine("— 依赖模块 —");
                sb.AppendLine(string.Join("、", m));
            }

            FieldInfo compMods = t.GetField("RequiredComponents", BindingFlags.Public | BindingFlags.Static);
            string[] cm = compMods?.GetValue(null) as string[];
            if (cm != null && cm.Length > 0) {
                sb.AppendLine();
                sb.AppendLine("— 依赖组件（预制体上必须挂） —");
                sb.AppendLine(string.Join("、", cm));
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>说明优先读 Setting 资产里的 MemoDoc，读不到才用代码默认值，编辑器和运行时都走这里。
        /// 直接按路径拿，不用 FindAssets 全盘扫（扫一次磁盘很贵，开图时每个节点都扫一次就是秒变十秒）。</summary>
        static string LoadMemoByAssetName(string settingName) {
            try {
#if UNITY_EDITOR
                var setting = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(
                    "Assets/LazyPan/Bundles/Configs/Setting/" + settingName + ".asset");
                if (setting == null) {
                    return null;
                }
#else
                var setting = UnityEngine.Resources.Load<UnityEngine.ScriptableObject>("Setting/" + settingName);
                if (setting == null) {
                    return null;
                }
#endif
                return setting.GetType().GetField("MemoDoc")?.GetValue(setting) as string;
            } catch { }
            return null;
        }

        static string DefaultOf(PayloadContractDef c) {
            switch (c.ValueType) {
                case ParamValueType.Bool: return c.BoolDefault.ToString();
                case ParamValueType.Int: return c.IntDefault.ToString();
                case ParamValueType.Float: return c.FloatDefault.ToString();
                case ParamValueType.String: return c.StringDefault ?? "";
                case ParamValueType.Vector3: return c.Vector3Default.ToString();
                default: return "";
            }
        }
    }
}
