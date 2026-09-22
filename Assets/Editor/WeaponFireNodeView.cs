using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GraphProcessor;
using LazyPan;

/// <summary>
/// 开火节点编辑器扩展: 把每把枪的生成物行为列出来 传话包写错词一眼能对上
/// 触发器不管表现 但配的人要知道生成物认哪些词 这里只做显示 不拦保存
/// </summary>
[NodeCustomEditor(typeof(BehaviourNode_WeaponFire))]
public class WeaponFireNodeView : BaseNodeView {
    UnityEngine.UIElements.Label contractLabel;

    public override void Enable() {
        base.Enable();
        contractLabel = new UnityEngine.UIElements.Label();
        contractLabel.style.whiteSpace = WhiteSpace.Normal;
        var checkButton = new UnityEngine.UIElements.Button(RefreshContract) { text = "检查生成物契约" };
        var fillButton = new UnityEngine.UIElements.Button(FillPayload) { text = "一键补齐传话包" };
        controlsContainer.Add(contractLabel);
        controlsContainer.Add(checkButton);
        controlsContainer.Add(fillButton);
        RefreshContract();
    }

    /// <summary>
    /// 一键补齐 词从生成物行为的认词单里来 缺的自动加行带默认值 配的人只改数不打字
    /// 补完标脏走自动保存 图与 Setting 一起落盘
    /// </summary>
    void FillPayload() {
        var node = nodeTarget as BehaviourNode_WeaponFire;
        if (node == null || node.Config == null || node.Config.Weapons == null) {
            return;
        }

        int added = 0;
        foreach (WeaponItem weapon in node.Config.Weapons) {
            if (weapon == null || string.IsNullOrEmpty(weapon.SpawnSign)) {
                continue;
            }

            string spawnBehaviours = SafeGetSpawnBehaviours(weapon.SpawnSign);
            if (spawnBehaviours == null) {
                continue;
            }

            if (weapon.Payload == null) {
                weapon.Payload = new List<WeaponPayloadItem>();
            }

            var has = new HashSet<string>();
            foreach (WeaponPayloadItem item in weapon.Payload) {
                if (item != null && !string.IsNullOrEmpty(item.ParamSign)) {
                    has.Add(item.ParamSign);
                }
            }

            foreach (PayloadContractDef def in FindPayloadContracts(spawnBehaviours)) {
                if (def == null || string.IsNullOrEmpty(def.Sign) || has.Contains(def.Sign)) {
                    continue;
                }

                weapon.Payload.Add(new WeaponPayloadItem() {
                    ParamSign = def.Sign,
                    ValueType = def.ValueType,
                    BoolValue = def.BoolDefault,
                    IntValue = def.IntDefault,
                    FloatValue = def.FloatDefault,
                    StringValue = def.StringDefault,
                    Vector3Value = def.Vector3Default,
                });
                added++;
            }
        }

        if (added > 0 && owner != null && owner.graph != null) {
            EditorUtility.SetDirty(owner.graph);
        }

        RefreshContract();
    }

    /// <summary>
    /// 每把枪: 生成物在不在清单里 挂了哪些行为 传话包写了哪些词 三行对上就齐了
    /// </summary>
    void RefreshContract() {
        var node = nodeTarget as BehaviourNode_WeaponFire;
        if (node == null || node.Config == null) {
            contractLabel.text = "节点数据异常";
            return;
        }

        var lines = new List<string>();
        if (node.Config.Weapons == null || node.Config.Weapons.Count == 0) {
            contractLabel.text = "军火库是空的 先加一行武器";
            return;
        }

        foreach (WeaponItem weapon in node.Config.Weapons) {
            if (weapon == null || string.IsNullOrEmpty(weapon.WeaponID)) {
                lines.Add("✗ 有一行 WeaponID 是空的 开火时会被跳过!");
                continue;
            }

            if (weapon.Kind == WeaponKind.Orbit) {
                lines.Add($"✓ {weapon.WeaponID}(环绕) 不生成 只转圈");
                continue;
            }

            if (string.IsNullOrEmpty(weapon.SpawnSign)) {
                lines.Add($"✗ {weapon.WeaponID} 生成实体是空的 不会生东西!");
                continue;
            }

            string spawnBehaviours = SafeGetSpawnBehaviours(weapon.SpawnSign);

            if (spawnBehaviours == null) {
                lines.Add($"✗ {weapon.WeaponID} 生成物 {weapon.SpawnSign} 不在 ObjConfig 清单里!");
                continue;
            }

            List<string> payloadSigns = new List<string>();
            if (weapon.Payload != null) {
                foreach (WeaponPayloadItem item in weapon.Payload) {
                    if (item != null && !string.IsNullOrEmpty(item.ParamSign)) {
                        payloadSigns.Add(item.ParamSign);
                    }
                }
            }

            List<string> missing = FindMissingPayload(weapon.SpawnSign, spawnBehaviours, payloadSigns);
            if (missing.Count == 0) {
                lines.Add($"✓ {weapon.WeaponID}→{weapon.SpawnSign}[{spawnBehaviours}]传话:{string.Join("/", payloadSigns)}");
            foreach (string module in FindMissingModules(spawnBehaviours)) {
                lines.Add($"✗ {weapon.WeaponID} 生成物缺模块依赖:{module}(工具箱下载记得带上!)");
            }
            } else {
                lines.Add($"✗ {weapon.WeaponID}→{weapon.SpawnSign} 传话包缺:{string.Join("/", missing)}(生成物认但没给!)");
            }
        }

        contractLabel.text = string.Join("\n", lines);
    }

    /// <summary>
    /// 按生成物挂的行为反查它们认哪些词 传话包没给、生成物默认值里也没有的才标出来
    /// 生成物 ParamValue 自带默认值=认了 传话包只写覆盖 空传话包全绿才是对的
    /// </summary>
    static List<string> FindMissingPayload(string spawnSign, string behaviourNames, List<string> payloadSigns) {
        var missing = new List<string>();
        var payload = new HashSet<string>(payloadSigns);
        var defaults = FindSpawnDefaultSigns(spawnSign);
        foreach (PayloadContractDef def in FindPayloadContracts(behaviourNames)) {
            if (!payload.Contains(def.Sign) && !defaults.Contains(def.Sign) && !missing.Contains(def.Sign)) {
                missing.Add(def.Sign);
            }
        }

        return missing;
    }

    /// <summary>
    /// 生成物 ParamValue 自带的默认值标签 传话包不用重复写
    /// </summary>
    static HashSet<string> FindSpawnDefaultSigns(string spawnSign) {
        var defaults = new HashSet<string>();
        if (string.IsNullOrEmpty(spawnSign)) {
            return defaults;
        }

        var setting = AssetDatabase.LoadAssetAtPath<LazyPan.ParamValueSetting>(
            "Assets/LazyPan/Bundles/Configs/Setting/ParamValueSetting.asset");
        if (setting == null || setting.Datas == null) {
            return defaults;
        }

        foreach (LazyPan.ParamValueSettingData entry in setting.Datas) {
            if (entry == null || entry.SourceSign != spawnSign || entry.Items == null) {
                continue;
            }

            foreach (LazyPan.ParamValueItem item in entry.Items) {
                if (item != null && !string.IsNullOrEmpty(item.ParamSign)) {
                    defaults.Add(item.ParamSign);
                }
            }
        }

        return defaults;
    }

    /// <summary>
    /// 生成物行为的认词单三件套 读不到(老行为没贴)返回空 不拦
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

            System.Type type = System.Type.GetType("LazyPan." + sign + ", Assembly-CSharp");
            var field = type?.GetField("RequiredPayload",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            PayloadContractDef[] required = field?.GetValue(null) as PayloadContractDef[];
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

    /// 编辑器读清单 运行时 ObjConfig.Get 依赖当前流程(Flo) 编辑器里 Flo 是空的直接炸
    /// 这里照实体下拉的老路子 直接读 Csv 拿生成物挂的行为 不走运行时接口
    /// </summary>
    static string SafeGetSpawnBehaviours(string sign) {
        try {
            string path = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "Csv", "ObjConfig.csv");
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

    /// <summary>
    /// 生成物行为声明的模块依赖 清单里没挂的标出来 工具箱视角: 装一半跑起来必炸
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

            System.Type type = System.Type.GetType("LazyPan." + sign + ", Assembly-CSharp");
            var field = type?.GetField("RequiredModules",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            string[] required = field?.GetValue(null) as string[];
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
    /// 中文行为名反查 Sign 走 BehaviourConfig.csv 与运行时注册同一张表
    /// </summary>
    static string FindBehaviourSign(string behaviourName) {
        foreach (string key in LazyPan.BehaviourConfig.GetKeys()) {
            var config = LazyPan.BehaviourConfig.Get(key);
            if (config != null && config.Name == behaviourName) {
                return config.Sign;
            }
        }

        return null;
    }
}
