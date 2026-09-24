using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GraphProcessor;
using LazyPan;

/// <summary>
/// 开火节点视图：说明书 + 上岗检查走共享装配，生成物契约并进上岗检查。
/// 一键补齐不要了，传话包玩家自己配，缺词检查只报出来不动手。
/// </summary>
[NodeCustomEditor(typeof(BehaviourNode_WeaponFire))]
public class WeaponFireNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this, null, WeaponContractCheck);
    }

    /// <summary>
    /// 生成物契约：顺着枪→生成物→它挂的行为→认词单，跟传话包逐个对。
    /// 缺词=本节点没给，判红；生成物不在、模块缺失是跨实体的事，只提醒。
    /// </summary>
    internal static void WeaponContractCheck(object config, List<string> red, List<string> yellow) {
        if (!(config is WeaponSettingData c) || c.Weapons == null) {
            return;
        }

        var setting = AssetDatabase.LoadAssetAtPath<ParamValueSetting>(
            "Assets/LazyPan/Bundles/Configs/Setting/ParamValueSetting.asset");

        foreach (WeaponItem weapon in c.Weapons) {
            if (weapon == null || string.IsNullOrEmpty(weapon.WeaponID)) {
                continue;
            }

            if (weapon.Kind == WeaponKind.Orbit) {
                continue;
            }

            if (string.IsNullOrEmpty(weapon.SpawnSign)) {
                continue;
            }

            string behaviours = SafeGetSpawnBehaviours(weapon.SpawnSign);
            if (behaviours == null) {
                yellow.Add($"跨实体提醒：枪 {weapon.WeaponID} 的生成物 {weapon.SpawnSign} 不在 ObjConfig 清单里");
                continue;
            }

            var payload = new HashSet<string>();
            if (weapon.Payload != null) {
                foreach (WeaponPayloadItem item in weapon.Payload) {
                    if (item != null && !string.IsNullOrEmpty(item.ParamSign)) {
                        payload.Add(item.ParamSign);
                    }
                }
            }

            var defaults = FindSpawnDefaultSigns(setting, weapon.SpawnSign);
            var missing = new List<string>();
            foreach (PayloadContractDef def in FindPayloadContracts(behaviours)) {
                if (!payload.Contains(def.Sign) && !defaults.Contains(def.Sign) && !missing.Contains(def.Sign)) {
                    missing.Add(def.Sign);
                }
            }

            if (missing.Count > 0) {
                red.Add($"枪 {weapon.WeaponID} 传话包缺：{string.Join("、", missing)}（生成物认但没给，自己加行）");
            }

            foreach (string module in FindMissingModules(behaviours)) {
                yellow.Add($"跨实体提醒：枪 {weapon.WeaponID} 生成物缺模块依赖 {module}");
            }
        }
    }

    /// <summary>
    /// 生成物 ParamValue 自带的默认值标签，传话包不用重复写。
    /// </summary>
    static HashSet<string> FindSpawnDefaultSigns(ParamValueSetting setting, string spawnSign) {
        var defaults = new HashSet<string>();
        if (setting == null || setting.Datas == null) {
            return defaults;
        }

        foreach (ParamValueSettingData entry in setting.Datas) {
            if (entry == null || entry.SourceSign != spawnSign || entry.Items == null) {
                continue;
            }

            foreach (ParamValueItem item in entry.Items) {
                if (item != null && !string.IsNullOrEmpty(item.ParamSign)) {
                    defaults.Add(item.ParamSign);
                }
            }
        }

        return defaults;
    }

    /// <summary>
    /// 生成物行为的认词单，读不到（老行为没贴）返回空，不拦。
    /// </summary>
    static List<PayloadContractDef> FindPayloadContracts(string behaviourNames) {
        var contracts = new List<PayloadContractDef>();
        if (string.IsNullOrEmpty(behaviourNames)) {
            return contracts;
        }

        foreach (string behaviourName in behaviourNames.Split('|')) {
            string name = behaviourName.Trim();
            if (string.IsNullOrEmpty(name)) {
                continue;
            }

            string sign = FindBehaviourSign(name);
            if (string.IsNullOrEmpty(sign)) {
                continue;
            }

            if (!BehaviourPayloadDoc.TryGetBehaviourType(sign, out System.Type type) || type == null) {
                continue;
            }

            var field = type.GetField("RequiredPayload",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var required = field?.GetValue(null) as PayloadContractDef[];
            if (required == null) {
                continue;
            }

            foreach (PayloadContractDef def in required) {
                if (def == null || string.IsNullOrEmpty(def.Sign)) {
                    continue;
                }

                bool dup = false;
                foreach (PayloadContractDef exist in contracts) {
                    if (exist.Sign == def.Sign) {
                        dup = true;
                        break;
                    }
                }

                if (!dup) {
                    contracts.Add(def);
                }
            }
        }

        return contracts;
    }

    /// <summary>
    /// 生成物行为声明的模块依赖，清单里没挂的标出来。
    /// </summary>
    static List<string> FindMissingModules(string behaviourNames) {
        var missing = new List<string>();
        if (string.IsNullOrEmpty(behaviourNames)) {
            return missing;
        }

        var mounted = new HashSet<string>();
        foreach (string behaviourName in behaviourNames.Split('|')) {
            string name = behaviourName.Trim();
            if (!string.IsNullOrEmpty(name)) {
                mounted.Add(name);
            }
        }

        foreach (string behaviourName in behaviourNames.Split('|')) {
            string name = behaviourName.Trim();
            if (string.IsNullOrEmpty(name)) {
                continue;
            }

            string sign = FindBehaviourSign(name);
            if (string.IsNullOrEmpty(sign)) {
                continue;
            }

            if (!BehaviourPayloadDoc.TryGetBehaviourType(sign, out System.Type type) || type == null) {
                continue;
            }

            var field = type.GetField("RequiredModules",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var required = field?.GetValue(null) as string[];
            if (required == null) {
                continue;
            }

            foreach (string module in required) {
                if (!mounted.Contains(module) && !missing.Contains(module)) {
                    missing.Add(module);
                }
            }
        }

        return missing;
    }

    /// <summary>
    /// 中文行为名反查 Sign，走 BehaviourConfig.csv 与运行时注册同一张表。
    /// </summary>
    static string FindBehaviourSign(string behaviourName) {
        foreach (string key in BehaviourConfig.GetKeys()) {
            var config = BehaviourConfig.Get(key);
            if (config != null && config.Name == behaviourName) {
                return config.Sign;
            }
        }

        return null;
    }

    /// <summary>
    /// 编辑器读清单，运行时接口依赖当前流程，编辑器里直接读 Csv。
    /// </summary>
    static string SafeGetSpawnBehaviours(string sign) {
        try {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Csv", "ObjConfig.csv");
            if (!System.IO.File.Exists(path)) {
                return null;
            }

            string[] lines = System.IO.File.ReadAllLines(path, System.Text.Encoding.UTF8);
            for (int i = 3; i < lines.Length; i++) {
                string[] cols = lines[i].Split(',');
                if (cols.Length < 6 || cols[0].Trim() != sign) {
                    continue;
                }

                return cols[5].Trim();
            }
        } catch {
        }

        return null;
    }
}
