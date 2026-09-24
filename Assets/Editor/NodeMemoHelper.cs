using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using GraphProcessor;
using LazyPan;

/// <summary>
/// 节点说明书共享装配：便签折叠 + 刷新按钮 + 行对齐，各行为视图一行接入。
/// 说明文本按行为缓存，刷新按钮清缓存并存盘，平时不轮询不标脏。
/// </summary>
public static class NodeMemoHelper {
    static readonly Dictionary<string, string> sMemoCache = new Dictionary<string, string>();

    public static void Attach(BaseNodeView view, Action<VisualElement> onConfigRows = null, Action<object, List<string>, List<string>> extraCheck = null) {
        //节点本身强制展开，不然折叠后便签跟着被藏起来；已经展开就别碰，碰一次图就脏一次
        if (view.nodeTarget != null && !view.nodeTarget.expanded) {
            view.nodeTarget.expanded = true;
        }
        if (!view.expanded) {
            view.expanded = true;
        }

        // 每次打开节点清掉这份说明的缓存，保证资产里改了字，重开节点立刻看见，不用点按钮刷
        string sign = BehaviourSignOf(view);
        BehaviourPayloadDoc.ClearCache();
        if (sign != null) {
            sMemoCache.Remove(sign);
        }

        var controls = view.controlsContainer;

        //藏起节点自带的“参数便签”输入框，只留折叠里的 Label，两份不打架、不压 Config
        var builtinMemo = controls.Q("参数便签");
        if (builtinMemo != null) {
            builtinMemo.style.display = DisplayStyle.None;
        }

        var memoLabel = new UnityEngine.UIElements.Label();
        memoLabel.enableRichText = true;
        memoLabel.style.whiteSpace = WhiteSpace.Normal;
        memoLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleLeft;
        memoLabel.style.marginTop = 4f;
        memoLabel.style.marginBottom = 4f;

        // 说明书包一层折叠，标题叫“行为说明书”，默认折叠，想看点一下就行
        var memoFoldout = new Foldout { text = "行为说明书", value = false };
        memoFoldout.Add(memoLabel);
        controls.Insert(0, memoFoldout);

        // 上岗检查：点一下验这条配置能不能跑，红=本节点缺的，黄=跨实体的提醒，不拦保存
        var checkLabel = new UnityEngine.UIElements.Label();
        checkLabel.enableRichText = true;
        checkLabel.style.whiteSpace = WhiteSpace.Normal;
        checkLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleLeft;
        checkLabel.style.marginTop = 4f;
        var checkFoldout = new Foldout { text = "检查上岗条件", value = false };
        checkFoldout.Add(checkLabel);
        controls.Add(checkFoldout);
        var checkButton = new Button(() => {
            checkLabel.text = RunCheck(view, extraCheck);
            checkFoldout.value = true;
        }) { text = "检查上岗条件" };
        controls.Add(checkButton);

        Refresh(view, memoLabel, false, onConfigRows);
        // 别轮询：界面画好后补刷几次（行是异步建出来的），之后只点按钮才刷。
        // 按住鼠标（拖节点/改数）时不动手，拖一半改样式会把框架的排版顶丢，松手再刷。
        ScheduleSettle(view, memoLabel, onConfigRows, 5);
    }

    static void ScheduleSettle(BaseNodeView view, UnityEngine.UIElements.Label memoLabel, Action<VisualElement> onConfigRows, int triesLeft) {
        view.schedule.Execute(() => {
            // 视图关了就收工，别碰已拆的树
            if (view.controlsContainer == null || view.controlsContainer.panel == null) {
                return;
            }

            if (triesLeft > 0 && UnityEngine.Input.GetMouseButton(0)) {
                ScheduleSettle(view, memoLabel, onConfigRows, triesLeft - 1);
                return;
            }

            try {
                Refresh(view, memoLabel, false, onConfigRows);
            } catch {
                // 框架正在重建行（比如列表拖拽中途），这次改一半会留乱摊子，直接吞掉等下一拍重来
                if (triesLeft > 0) {
                    ScheduleSettle(view, memoLabel, onConfigRows, triesLeft - 1);
                }
            }
        }).StartingIn(400);
    }

    static string BehaviourSignOf(BaseNodeView view) {
        return (view.nodeTarget as BehaviourGraphNode)?.BehaviourSign;
    }

    /// <summary>
    /// 上岗检查：找行为类上静态 CheckContract(config, red, yellow)，有就跑，没有就回占位话。
    /// 只读配置不改东西，跨实体的只进黄单，红了也不拦保存。
    /// </summary>
    internal static string RunCheck(BaseNodeView view, Action<object, List<string>, List<string>> extraCheck = null) {
        string sign = BehaviourSignOf(view);
        if (string.IsNullOrEmpty(sign)) {
            return "节点异常：拿不到行为名";
        }

        if (!BehaviourPayloadDoc.TryGetBehaviourType(sign, out Type t) || t == null) {
            return "检查跑不起来：找不到行为类 " + sign;
        }

        var method = t.GetMethod("CheckContract",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (method == null) {
            return "该行为暂无检查项，照说明书配就行";
        }

        object config = null;
        try {
            // Config 可能是 struct，取出来是拷贝，检查只读，够用了
            config = view.nodeTarget.GetType().GetField("Config")?.GetValue(view.nodeTarget);
        } catch { }

        var red = new List<string>();
        var yellow = new List<string>();
        try {
            method.Invoke(null, new object[] { config, red, yellow });
        } catch (Exception e) {
            yellow.Add("检查自己跑飞了：" + (e.InnerException?.Message ?? e.Message));
        }

        // 跨实体存在性通用扫：所有 *EntitySign（除自己 SourceSign）去 Graph 资产里查，找不到只进黄单
        AppendEntityYellows(config, yellow);

        // 视图级加查（如武器查生成物契约），只读不写
        try {
            extraCheck?.Invoke(config, red, yellow);
        } catch (Exception e) {
            yellow.Add("加查跑飞了：" + (e.InnerException?.Message ?? e.Message));
        }

        // 本实体组件实查：CompTriggerSign 真去预制体上找 Comp，有没有给结论
        AppendCompCheck(config, red, yellow);

        var sb = new System.Text.StringBuilder();
        if (red.Count == 0 && yellow.Count == 0) {
            sb.Append("<color=#9CCC65>齐了，能跑</color>");
        }

        foreach (string r in red) {
            sb.AppendLine("<color=#EF5350>缺：</color>" + r);
        }

        foreach (string y in yellow) {
            sb.AppendLine("<color=#FFD54F>提醒：</color>" + y);
        }

        return sb.ToString().TrimEnd();
    }

    static readonly HashSet<string> sEntityKeywords = LoadEntityKeywords();

    static HashSet<string> LoadEntityKeywords() {
        var set = new HashSet<string> { "Self", "Any", "Triggerer", "Root", "" };
        try {
            var t = typeof(LazyPan.BehaviourGraphNode).Assembly.GetType("LazyPan.BehaviourSigns");
            foreach (string n in new[] { "Self", "Any", "Triggerer", "Root" }) {
                var f = t?.GetField(n, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (f?.GetValue(null) is string v && !string.IsNullOrEmpty(v)) {
                    set.Add(v);
                }
            }
        } catch { }

        return set;
    }

    /// <summary>
    /// 本实体 Comp 实查：配置里的 CompTriggerSign 真去本实体预制体上找，有没有都给结论。
    /// Root=查实体根上有没有 Comp；填了名的查有没有这个 Sign 的 Comp，顺带看有没有触发碰撞体。
    /// </summary>
    internal static void AppendCompCheck(object config, List<string> red, List<string> yellow) {
        if (config == null) {
            return;
        }

        string sourceSign = config.GetType().GetField("SourceSign")?.GetValue(config) as string;
        if (string.IsNullOrEmpty(sourceSign)) {
            return;
        }

        var wants = new List<string>();
        CollectCompTriggerSigns(config, wants, 0);
        if (wants.Count == 0) {
            return;
        }

        var prefab = FindEntityPrefab(sourceSign);
        if (prefab == null) {
            yellow.Add($"找不到 {sourceSign} 的预制体，确认 Prefabs/Obj 下有同名预制体");
            return;
        }

        var comps = prefab.GetComponentsInChildren<LazyPan.Comp>(true);
        // 真机制：CompTriggerSign 是实体根 Comp 的 Comps 登记表（CompData{Sign, Comp}）里的名字，
        // 不是 Comp 身上的字段。Root=用根 Comp 自己。
        var rootComp = prefab.GetComponent<LazyPan.Comp>();
        var registered = new Dictionary<string, LazyPan.Comp>();
        if (rootComp != null && rootComp.Comps != null) {
            foreach (var d in rootComp.Comps) {
                if (d != null && !string.IsNullOrEmpty(d.Sign) && d.Comp != null) {
                    registered[d.Sign] = d.Comp;
                }
            }
        }

        var triggerOk = new HashSet<string>();
        foreach (var kv in registered) {
            var col = kv.Value.GetComponent<UnityEngine.Collider>();
            if (col != null && col.isTrigger) {
                triggerOk.Add(kv.Key);
            }
        }

        string rootSign = KeywordValue("Root", "Root");
        foreach (string v in new HashSet<string>(wants)) {
            if (v == rootSign) {
                if (rootComp == null) {
                    red.Add("填了 Root，但实体根上没有 Comp");
                }

                continue;
            }

            if (sEntityKeywords.Contains(v)) {
                red.Add($"触发器填了 {v} 没用，填根 Comp 登记表里的 Sign 或 Root");
                continue;
            }

            if (!registered.TryGetValue(v, out LazyPan.Comp child)) {
                red.Add($"实体根 Comp 的登记表里没有 Sign={v}，先去根 Comp 的 Comps 里登记");
            } else if (!triggerOk.Contains(v)) {
                yellow.Add($"Sign={v} 登记到了，但它上面没开触发的碰撞体，进出事件不会触发");
            }
        }
    }

    static void CollectCompTriggerSigns(object o, List<string> outList, int depth) {
        if (o == null || depth > 3) {
            return;
        }

        var t = o.GetType();
        if (t.IsPrimitive || t.IsEnum || o is string || o is UnityEngine.Object) {
            return;
        }

        if (o is System.Collections.IEnumerable list) {
            foreach (var item in list) {
                CollectCompTriggerSigns(item, outList, depth + 1);
            }

            return;
        }

        foreach (var f in t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)) {
            object v;
            try {
                v = f.GetValue(o);
            } catch {
                continue;
            }

            if (v == null) {
                continue;
            }

            if (f.FieldType == typeof(string)) {
                if (f.Name == "CompTriggerSign" && !string.IsNullOrEmpty(v as string)) {
                    outList.Add(v as string);
                }
            } else if (!f.FieldType.IsPrimitive && !f.FieldType.IsEnum) {
                CollectCompTriggerSigns(v, outList, depth + 1);
            }
        }
    }

    static string KeywordValue(string name, string fallback) {
        try {
            var t = typeof(LazyPan.BehaviourGraphNode).Assembly.GetType("LazyPan.BehaviourSigns");
            var f = t?.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (f?.GetValue(null) is string v && !string.IsNullOrEmpty(v)) {
                return v;
            }
        } catch { }

        return fallback;
    }

    internal static UnityEngine.GameObject FindEntityPrefab(string sourceSign) {
        var guids = UnityEditor.AssetDatabase.FindAssets(sourceSign + " t:Prefab");
        foreach (string guid in guids) {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) {
                continue;
            }

            if (!System.IO.Path.GetFileNameWithoutExtension(path).Equals(sourceSign, System.StringComparison.Ordinal)) {
                continue;
            }

            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
            if (prefab != null) {
                return prefab;
            }
        }

        return null;
    }

    /// <summary>
    /// 反射扫配置里所有 *EntitySign / TargetType / SpawnSign / GenerateEntitySign，
    /// 真去 ObjConfig.csv 里查 Sign 列和 Type 列，找不到只提醒（跨实体不判红）。
    /// </summary>
    internal static void AppendEntityYellows(object config, List<string> yellow) {
        if (config == null) {
            return;
        }

        var seen = new HashSet<string>();
        ScanEntitySigns(config, yellow, seen, 0);
    }

    static System.DateTime sCsvTime;
    static HashSet<string> sCsvSigns;
    static HashSet<string> sCsvTypes;

    /// <summary>
    /// ObjConfig.csv 常驻内存，文件变了才重读：Sign 列是实体名，Type 列是类型名。
    /// </summary>
    static void EnsureObjCsv() {
        string path = "Assets/StreamingAssets/Csv/ObjConfig.csv";
        System.DateTime t;
        try {
            t = System.IO.File.GetLastWriteTime(path);
        } catch {
            return;
        }

        if (sCsvSigns != null && sCsvTime == t) {
            return;
        }

        sCsvTime = t;
        sCsvSigns = new HashSet<string>();
        sCsvTypes = new HashSet<string>();
        try {
            string[] lines = System.IO.File.ReadAllLines(path, System.Text.Encoding.UTF8);
            for (int i = 3; i < lines.Length; i++) {
                string[] cols = lines[i].Split(',');
                if (cols.Length < 3) {
                    continue;
                }

                string sign = cols[0].Trim();
                string type = cols[2].Trim();
                if (!string.IsNullOrEmpty(sign)) {
                    sCsvSigns.Add(sign);
                }

                if (!string.IsNullOrEmpty(type)) {
                    sCsvTypes.Add(type);
                }
            }
        } catch { }
    }

    static void ScanEntitySigns(object o, List<string> yellow, HashSet<string> seen, int depth) {
        if (o == null || depth > 3) {
            return;
        }

        var t = o.GetType();
        if (t.IsPrimitive || t.IsEnum || o is string || o is UnityEngine.Object) {
            return;
        }

        if (o is System.Collections.IEnumerable list) {
            foreach (var item in list) {
                ScanEntitySigns(item, yellow, seen, depth + 1);
            }

            return;
        }

        foreach (var f in t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)) {
            object v;
            try {
                v = f.GetValue(o);
            } catch {
                continue;
            }

            if (v == null) {
                continue;
            }

            if (f.FieldType == typeof(string)) {
                bool isEntity = f.Name.EndsWith("EntitySign") && f.Name != "SourceSign";
                bool isSpawn = f.Name == "SpawnSign" || f.Name == "GenerateEntitySign";
                bool isType = f.Name == "TargetType";
                if (!isEntity && !isSpawn && !isType) {
                    continue;
                }

                string sign = v as string;
                if (string.IsNullOrEmpty(sign) || sEntityKeywords.Contains(sign) || !seen.Add(f.Name + "=" + sign)) {
                    continue;
                }

                EnsureObjCsv();
                if (isType) {
                    if (sCsvTypes != null && !sCsvTypes.Contains(sign)) {
                        yellow.Add($"跨实体提醒：类型 {sign} 在 ObjConfig.csv 的类型列里找不到，确认写的是类型不是实体名");
                    }
                } else if (sCsvSigns != null && !sCsvSigns.Contains(sign)) {
                    yellow.Add($"跨实体提醒：{sign} 在 ObjConfig.csv 里找不到，确认该实体存在（在别的图里也算有）");
                }
            } else if (!f.FieldType.IsPrimitive && !f.FieldType.IsEnum) {
                ScanEntitySigns(v, yellow, seen, depth + 1);
            }
        }
    }

    static void Refresh(BaseNodeView view, UnityEngine.UIElements.Label memoLabel, bool saveSnapshot, Action<VisualElement> onConfigRows) {
        string sign = BehaviourSignOf(view);
        if (sign != null) {
            // 说明文本缓存起来，一个行为只反射读一次，不每次翻资产
            if (!sMemoCache.TryGetValue(sign, out string text)) {
                text = BehaviourPayloadDoc.Get(sign);
                if (string.IsNullOrEmpty(text)) {
                    text = "便签为空：先等脚本编译完，再点“刷新便签”。";
                }

                sMemoCache[sign] = text;
            }

            //内容没变就不碰，避免重排把 Config 顶来顶去
            if (memoLabel.text != text) {
                memoLabel.text = text;
            }

            // 快照只在点按钮时存盘：写节点字段会标脏整张图，每次打开就存是巨卡
            if (saveSnapshot && view.nodeTarget is BehaviourGraphNode node && node.参数便签 != text) {
                node.参数便签 = text;
            }
        }

        var configField = FindConfig(view.controlsContainer);
        if (configField != null) {
            AlignConfigRows(configField);
            ShieldListDrag(configField);
            onConfigRows?.Invoke(configField);
        }
    }

    /// <summary>
    /// 列表拖拽和节点拖拽打架：从列表上按下的拖动到列表这层就拦住，不再往上冒泡，
    /// 节点收不到就不会跟着走。只拦冒泡，列表自己的点击和拖拽不受影响。
    /// 注意两个都要拦：PointerDown 是新事件，MouseDown 是老事件，框架节点拖拽认的是老的，
    /// 只拦新的等于没拦，两边照样一起拽，打架卡死。
    /// </summary>
    internal static void ShieldListDrag(VisualElement configField) {
        foreach (var lv in configField.Query<BaseListView>().ToList()) {
            if (lv.userData is bool done && done) {
                continue;
            }

            lv.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            lv.RegisterCallback<MouseDownEvent>(e => e.StopPropagation());
            lv.userData = true;
        }
    }

    internal static VisualElement FindConfig(VisualElement controls) {
        return controls
            .Query<PropertyField>()
            .ToList()
            .FirstOrDefault(p =>
                p.name == "Config" ||
                (!string.IsNullOrEmpty(p.bindingPath) && p.bindingPath.EndsWith(".Config")));
    }

    /// <summary>
    /// Config 下每行左标签定宽，右边输入框全部顶到同一 x 起跑，两表看着齐。
    /// 只管直接子行；整过一次的行打标记，下次直接跳过，不反复碰样式。
    /// </summary>
    internal static void AlignConfigRows(VisualElement configField) {
        foreach (var child in configField.Query<PropertyField>().ToList()) {
            if (child.parent != configField) {
                continue;
            }

            if (child.userData is bool done && done) {
                continue;
            }

            child.style.flexDirection = FlexDirection.Row;
            child.style.paddingLeft = 0f;
            child.style.marginLeft = 0f;

            // 行自己的标签优先（parent 就是本行），定制绘制的行里第一个 Label 可能藏在值区，那种只定行级不管标签
            var ownLabel = child.Q<UnityEngine.UIElements.Label>();
            if (ownLabel != null && ownLabel.parent == child) {
                ownLabel.style.flexBasis = new Length(42, LengthUnit.Percent);
                ownLabel.style.flexGrow = 0f;
                ownLabel.style.flexShrink = 0f;
                ownLabel.style.marginLeft = 0f;
                ownLabel.style.marginRight = 4f;
                ownLabel.style.paddingLeft = 0f;
                ownLabel.style.whiteSpace = WhiteSpace.Normal;
            }

            child.userData = true;
        }
    }
}
