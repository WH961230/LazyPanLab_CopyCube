using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GraphProcessor;

namespace LazyPan {
    public class NodeGraphToolWindow : BaseGraphWindow {
        const string graphFolder = "Assets/LazyPan/Bundles/Configs/Graph";
        const double autoSaveDelay = 0.8;

        double lastDirtyTime;

        [InitializeOnLoadMethod]
        static void RegisterHook() {
            EntityGraphHook.OpenEntityGraph = OpenForEntity;
        }

        public static void Open() {
            OpenForEntity("空实体", new string[0]);
        }

        public static void OpenForEntity(string entitySign, string[] behaviourNames) {
            var window = CreateWindow<NodeGraphToolWindow>();
            var graphPath = $"{graphFolder}/{entitySign}.asset";
            var graph = AssetDatabase.LoadAssetAtPath<BaseGraph>(graphPath);

            if (graph == null) {
                if (!AssetDatabase.IsValidFolder(graphFolder))
                    AssetDatabase.CreateFolder("Assets/LazyPan/Bundles/Configs", "Graph");
                graph = ScriptableObject.CreateInstance<BaseGraph>();
                AssetDatabase.CreateAsset(graph, AssetDatabase.GenerateUniqueAssetPath(graphPath));
            }

            // 清单以 ObjConfig.csv 文件为准 调用方(实体面板内存数据)可能是删节点之前的旧数据
            // 直接用旧的会把刚删掉的节点又补回来 导致删了还出现
            string[] effectiveNames = ResolveBehaviourNames(entitySign, behaviourNames);
            // 面板里又加回来的行为 允许下次删除时重新弹窗
            PruneConfirmedRemoves(entitySign, effectiveNames);
            // 把该实体已配置的行为节点补进图(已有的不重复添加)并立即落盘
            AddBehaviourNodes(graph, entitySign, effectiveNames);
            SaveGraph(graph);

            window.InitializeGraph(graph);
            window.Show();
        }

        /// <summary>
        /// 按实体在 ObjConfig 里配置的行为中文名 找到对应行为图节点并加入图 已存在则跳过
        /// 节点参数从对应 Setting 资产中按 SourceSign == 实体Sign 的那条加载(仅填空 不覆盖已有编辑)
        /// </summary>
        static void AddBehaviourNodes(BaseGraph graph, string entitySign, string[] behaviourNames) {
            if (behaviourNames == null || behaviourNames.Length == 0)
                return;

            var nameToSign = new Dictionary<string, string>();
            foreach (var sign in BehaviourConfig.GetKeys()) {
                var cfg = BehaviourConfig.Get(sign);
                if (cfg != null && !string.IsNullOrEmpty(cfg.Name))
                    nameToSign[cfg.Name] = sign;
            }

            var signToNodeType = new Dictionary<string, Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<BehaviourGraphNode>()) {
                if (type.IsAbstract)
                    continue;
                var node = (BehaviourGraphNode)Activator.CreateInstance(type);
                if (node != null && !string.IsNullOrEmpty(node.BehaviourSign))
                    signToNodeType[node.BehaviourSign] = type;
            }

            var existingSigns = graph.nodes
                .OfType<BehaviourGraphNode>()
                .Select(n => n.BehaviourSign)
                .ToHashSet();

            // 只对"从未初始化过"的节点补数据 用户改过/清空过的不动(NodeInitialized 随图序列化)
            foreach (var oldNode in graph.nodes.OfType<BehaviourGraphNode>()) {
                if (!oldNode.NodeInitialized) {
                    LoadNodeConfig(oldNode, entitySign);
                    oldNode.NodeInitialized = true;
                }
            }

            float x = 60f;
            float y = 80f;
            foreach (var rawName in behaviourNames) {
                string name = rawName?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;
                if (!nameToSign.TryGetValue(name, out var sign))
                    continue;
                if (!signToNodeType.TryGetValue(sign, out var nodeType))
                    continue;
                if (existingSigns.Contains(sign))
                    continue;

                var node = BaseNode.CreateFromType(nodeType, new Vector2(x, y));
                if (node != null) {
                    LoadNodeConfig(node, entitySign);
                    ((BehaviourGraphNode)node).NodeInitialized = true;
                    graph.AddNode(node);
                }
                existingSigns.Add(sign);
                x += 240f;
            }
        }

        /// <summary>
        /// 从对应 Setting 资产把该实体的那条配置填进节点
        /// </summary>
        static void LoadNodeConfig(BaseNode node, string entitySign) {
            switch (node) {
                case BehaviourNode_Death n: {
                    var s = LoadSetting<DeathSetting>("DeathSetting");
                    if (s != null && s.TryGet(entitySign, out var d)) n.Config = d;
                    break;
                }
                case BehaviourNode_TrackingEntity n: {
                    var s = LoadSetting<TrackingEntitySetting>("TrackingEntitySetting");
                    if (s != null && s.TryGet(entitySign, out var d)) n.Config = d;
                    break;
                }
                case BehaviourNode_DelayGenerate n: {
                    var s = LoadSetting<DelayGenerateEntitySetting>("DelayGenerateSetting");
                    if (s != null && s.TryGet(entitySign, out var d)) n.Config = d;
                    break;
                }
                case BehaviourNode_EntityUIBinder n: {
                    var s = LoadSetting<EntityUIBinderSetting>("EntityUIBinderSetting");
                    if (s != null && s.TryGet(entitySign, out var d)) n.Config = d;
                    break;
                }
                case BehaviourNode_InputWASDMove n: {
                    var s = LoadSetting<InputWASDMoveSetting>("InputWASDMoveSetting");
                    if (s != null && s.TryGet(entitySign, out var d)) n.Config = d;
                    break;
                }
                case BehaviourNode_WaveManager n: {
                    var s = LoadSetting<WaveManagerSetting>("WaveManagerSetting");
                    if (s != null && s.TryGet(entitySign, out var d)) n.Config = d;
                    break;
                }
                case BehaviourNode_EquipmentMount n: {
                    var s = LoadSetting<EquipmentMountSetting>("EquipmentMountSetting");
                    if (s != null && s.TryGet(entitySign, out var d)) n.Config = d;
                    break;
                }
                case BehaviourNode_BeginLogo n: {
                    var s = LoadSetting<BeginLogoSetting>("BeginLogoSetting");
                    if (s != null && s.TryGet(entitySign, out var d)) n.Config = d;
                    break;
                }
                case BehaviourNode_EntityTriggerController n: {
                    var s = LoadSetting<EntityTriggerControllerSetting>("EntityTriggerControllerSetting");
                    if (s != null && s.TryGet(entitySign, out var d)) n.Config = d;
                    break;
                }
                case BehaviourNode_ParamValue n: {
                    var s = LoadSetting<ParamValueSetting>("ParamValueSetting");
                    if (s != null && s.TryGet(entitySign, out var d)) n.Config = d;
                    break;
                }
            }
        }

        static T LoadSetting<T>(string assetName) where T : Setting {
            return AssetDatabase.LoadAssetAtPath<T>($"Assets/LazyPan/Bundles/Configs/Setting/{assetName}.asset");
        }

        /// <summary>按实体 Sign 从 ObjConfig.csv 取其行为中文名数组(总览跳转用)</summary>
        public static string[] GetBehaviourNames(string entitySign) {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, "Csv", "ObjConfig.csv");
            string[] lines;
            try {
                lines = System.IO.File.ReadAllLines(path);
            } catch {
                return new string[0];
            }
            foreach (var line in lines.Skip(3)) {
                var cols = line.Split(',');
                if (cols.Length < 6 || cols[0].Trim() != entitySign)
                    continue;
                return cols[5].Split('|').Select(b => b.Trim()).Where(b => !string.IsNullOrEmpty(b)).ToArray();
            }
            return new string[0];
        }

        static bool SyncGraphToSettings(BaseGraph graph, string entitySign) {
            bool changed = false;
            foreach (var node in graph.nodes.OfType<BehaviourGraphNode>()) {
                string assetName = GetSettingAssetName(node);
                if (string.IsNullOrEmpty(assetName))
                    continue;
                var configField = node.GetType().GetField("Config");
                if (configField == null)
                    continue;
                changed |= SyncSettingData(assetName, entitySign, configField.GetValue(node));
            }
            return changed;
        }

        static string GetSettingAssetName(BehaviourGraphNode node) {
            switch (node) {
                case BehaviourNode_Death _: return "DeathSetting";
                case BehaviourNode_TrackingEntity _: return "TrackingEntitySetting";
                case BehaviourNode_DelayGenerate _: return "DelayGenerateSetting";
                case BehaviourNode_EntityUIBinder _: return "EntityUIBinderSetting";
                case BehaviourNode_InputWASDMove _: return "InputWASDMoveSetting";
                case BehaviourNode_WaveManager _: return "WaveManagerSetting";
                case BehaviourNode_EquipmentMount _: return "EquipmentMountSetting";
                case BehaviourNode_BeginLogo _: return "BeginLogoSetting";
                case BehaviourNode_EntityTriggerController _: return "EntityTriggerControllerSetting";
                case BehaviourNode_ParamValue _: return "ParamValueSetting";
                default: return null;
            }
        }

        static bool SyncSettingData(string assetName, string entitySign, object graphData) {
            if (graphData == null)
                return false;
            var setting = LoadSetting<Setting>(assetName);
            var listField = setting?.GetType().GetField("Datas");
            var list = listField?.GetValue(setting) as System.Collections.IList;
            var sourceField = graphData.GetType().GetField("SourceSign");
            if (list == null || sourceField == null)
                return false;

            string nodeSign = sourceField.GetValue(graphData) as string;

            // 1) 按"图的实体Sign"定位(稳定身份 正常情况走这里)
            int index = FindIndexBySign(list, sourceField, entitySign);
            // 2) 退化: 按节点当前Sign定位(Sign被改成别的实体时)
            if (index < 0 && !string.IsNullOrEmpty(nodeSign))
                index = FindIndexBySign(list, sourceField, nodeSign);
            // 3) 再退化: 节点Sign为空时 若列表里只剩一条空Sign条目 视为本图的镜像
            if (index < 0 && string.IsNullOrEmpty(nodeSign))
                index = FindSingleEmptyIndex(list, sourceField);

            if (index >= 0) {
                list[index] = graphData;
                EditorUtility.SetDirty(setting);
                return true;
            }

            // 4) 都定位不到 且节点有有效Sign 才新增条目(空Sign时无法安全识别 跳过避免重复)
            if (string.IsNullOrEmpty(nodeSign))
                return false;
            var addMethod = list.GetType().GetMethod("Add");
            if (addMethod == null)
                return false;
            addMethod.Invoke(list, new[] { graphData });
            EditorUtility.SetDirty(setting);
            return true;
        }

        static int FindIndexBySign(System.Collections.IList list, System.Reflection.FieldInfo sourceField, string sign) {
            for (int i = 0; i < list.Count; i++) {
                var item = list[i];
                if (item != null && sourceField.GetValue(item) as string == sign)
                    return i;
            }
            return -1;
        }

        static int FindSingleEmptyIndex(System.Collections.IList list, System.Reflection.FieldInfo sourceField) {
            int found = -1;
            for (int i = 0; i < list.Count; i++) {
                var item = list[i];
                if (item == null || !string.IsNullOrEmpty(sourceField.GetValue(item) as string))
                    continue;
                if (found >= 0)
                    return -1; // 多条空Sign 无法安全识别
                found = i;
            }
            return found;
        }

        static void SaveGraph(BaseGraph graph) {
            if (graph == null)
                return;
            // 删节点同步: 图上少掉的行为节点 反写 ObjConfig.csv 清单与 Setting 条目(方向B 以图为准)
            // 新增节点由 AddBehaviourNodes 在打开时处理 这里只处理删除 避免与补节点逻辑打架
            SyncRemovedNodes(graph);
            // 图内数据回写到对应 Setting 资产 保持一一对应(按图资产名=实体Sign 定位 Setting 条目)
            SyncGraphToSettings(graph, graph.name);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 删除同步: 图上已没有但 ObjConfig 清单里还有的行为 视为用户在图中删掉了
        /// 弹窗确认后反写清单并删除 Setting 对应条目 取消则把节点恢复回图
        /// 已确认的不再重复弹窗(内存态) 但 Setting 条目只要还在 每次保存都会重试删除(落盘态)
        /// 这样即使某次 Setting 删除没写进磁盘 下次保存也会补删 不会留下空条目
        /// </summary>
        static readonly System.Collections.Generic.HashSet<string> confirmedRemoves
            = new System.Collections.Generic.HashSet<string>();

        static void SyncRemovedNodes(BaseGraph graph) {
            string entitySign = graph != null ? graph.name : null;
            if (string.IsNullOrEmpty(entitySign))
                return;
            var names = GetBehaviourNames(entitySign);
            if (names == null || names.Length == 0)
                return;
            var graphSigns = graph.nodes.OfType<BehaviourGraphNode>()
                .Select(n => n.BehaviourSign).Where(s => !string.IsNullOrEmpty(s)).ToHashSet();
            var nameToSign = new Dictionary<string, string>();
            foreach (var sign in BehaviourConfig.GetKeys()) {
                var cfg = BehaviourConfig.Get(sign);
                if (cfg != null && !string.IsNullOrEmpty(cfg.Name))
                    nameToSign[cfg.Name] = sign;
            }
            foreach (var name in names) {
                if (!nameToSign.TryGetValue(name, out var sign))
                    continue;
                if (graphSigns.Contains(sign))
                    continue;
                string key = entitySign + "|" + sign;
                bool alreadyConfirmed = confirmedRemoves.Contains(key);
                if (!alreadyConfirmed) {
                    bool confirm = EditorUtility.DisplayDialog("删除行为",
                        $"检测到实体 {entitySign} 的行为 [{name}] 已从图中删除\n\n确定: ObjConfig 与 Setting 对应条目一起删 下次打开不再补回\n取消: 恢复该节点到图中",
                        "确定删除", "取消恢复");
                    if (!confirm) {
                        RestoreNode(graph, entitySign, sign);
                        continue;
                    }
                    confirmedRemoves.Add(key);
                }
                RemoveBehaviourFromObjConfig(entitySign, name);
                RemoveSettingEntryBySign(sign, entitySign);
            }
        }

        /// <summary>取消删除时把节点恢复回图 并从 Setting 回填数据</summary>
        static void RestoreNode(BaseGraph graph, string entitySign, string behaviourSign) {
            foreach (var type in TypeCache.GetTypesDerivedFrom<BehaviourGraphNode>()) {
                if (type.IsAbstract)
                    continue;
                var probe = (BehaviourGraphNode)Activator.CreateInstance(type);
                if (probe == null || probe.BehaviourSign != behaviourSign)
                    continue;
                var node = BaseNode.CreateFromType(type, new Vector2(60f, 80f));
                if (node == null)
                    return;
                LoadNodeConfig(node, entitySign);
                ((BehaviourGraphNode)node).NodeInitialized = true;
                graph.AddNode(node);
                EditorUtility.SetDirty(graph);
                return;
            }
        }

        /// <summary>按 BehaviourSign 找到 Setting 资产名 再删该实体的条目</summary>
        static void RemoveSettingEntryBySign(string behaviourSign, string entitySign) {
            string assetName = null;
            switch (behaviourSign) {
                case nameof(Behaviour_Event_Death): assetName = "DeathSetting"; break;
                case nameof(Behaviour_Auto_TrackingEntityByNavMeshAgent): assetName = "TrackingEntitySetting"; break;
                case nameof(Behaviour_Event_DelayGenerateEntity): assetName = "DelayGenerateSetting"; break;
                case nameof(Behaviour_Event_EntityUIBinder): assetName = "EntityUIBinderSetting"; break;
                case nameof(Behaviour_Auto_InputWASDMove): assetName = "InputWASDMoveSetting"; break;
                case nameof(Behaviour_Event_WaveManager): assetName = "WaveManagerSetting"; break;
                case nameof(Behaviour_Event_EquipmentMountManager): assetName = "EquipmentMountSetting"; break;
                case nameof(Behaviour_Event_BeginLogo): assetName = "BeginLogoSetting"; break;
                case nameof(Behaviour_Auto_EntityTriggerController): assetName = "EntityTriggerControllerSetting"; break;
                case nameof(Behaviour_Event_ParamValue): assetName = "ParamValueSetting"; break;
            }
            RemoveSettingEntry(assetName, entitySign);
        }

        /// <summary>
        /// 生效行为名解析: 优先读 ObjConfig.csv 文件(真相) 调用方传入的只做补充(面板里新勾的)
        /// 这样删节点反写清单后 即使实体面板没刷新 下次打开也不会用旧数据补回已删节点
        /// </summary>
        static string[] ResolveBehaviourNames(string entitySign, string[] passedNames) {
            var fileNames = GetBehaviourNames(entitySign);
            var fileSet = new HashSet<string>(fileNames ?? new string[0]);
            // 调用方是文件子集时(面板没刷新/删前旧数据) 以文件为准 防止删掉的又补回来
            bool passedIsSubset = true;
            if (passedNames != null) {
                foreach (var n in passedNames) {
                    var t = n?.Trim();
                    if (string.IsNullOrEmpty(t))
                        continue;
                    if (!fileSet.Contains(t)) {
                        passedIsSubset = false;
                        break;
                    }
                }
            }
            if (passedIsSubset)
                return fileNames;
            // 调用方多出来的才是面板里新勾的行为 合并后允许补节点
            var merged = new List<string>(fileNames ?? new string[0]);
            foreach (var n in passedNames) {
                var t = n?.Trim();
                if (string.IsNullOrEmpty(t) || merged.Contains(t))
                    continue;
                merged.Add(t);
            }
            return merged.ToArray();
        }

        /// <summary>面板里加回来的行为 从已确认删除集合里移除 允许再次删除时重新弹窗</summary>
        static void PruneConfirmedRemoves(string entitySign, string[] effectiveNames) {
            if (string.IsNullOrEmpty(entitySign) || effectiveNames == null)
                return;
            var nameToSign = new Dictionary<string, string>();
            foreach (var sign in BehaviourConfig.GetKeys()) {
                var cfg = BehaviourConfig.Get(sign);
                if (cfg != null && !string.IsNullOrEmpty(cfg.Name))
                    nameToSign[cfg.Name] = sign;
            }
            foreach (var name in effectiveNames) {
                if (nameToSign.TryGetValue(name, out var sign))
                    confirmedRemoves.Remove(entitySign + "|" + sign);
            }
        }

        /// <summary>按 BehaviourSign 反查 BehaviourConfig 的中文名(ObjConfig 行为列存的是中文名)</summary>
        static string BehaviourNameOf(string behaviourSign) {
            foreach (var sign in BehaviourConfig.GetKeys()) {
                var cfg = BehaviourConfig.Get(sign);
                if (cfg != null && cfg.Sign == behaviourSign)
                    return cfg.Name;
            }
            return null;
        }

        /// <summary>从 ObjConfig.csv 该实体的行为列去掉指定中文名(反写清单 否则下次打开又补回)</summary>
        static void RemoveBehaviourFromObjConfig(string entitySign, string behaviourName) {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, "Csv", "ObjConfig.csv");
            string[] lines;
            try {
                lines = System.IO.File.ReadAllLines(path, System.Text.Encoding.UTF8);
            } catch {
                return;
            }
            bool changed = false;
            for (int i = 3; i < lines.Length; i++) {
                var cols = lines[i].Split(',');
                if (cols.Length < 6 || cols[0].Trim() != entitySign)
                    continue;
                var kept = cols[5].Split('|').Select(b => b.Trim())
                    .Where(b => !string.IsNullOrEmpty(b) && b != behaviourName).ToArray();
                cols[5] = string.Join("|", kept);
                lines[i] = string.Join(",", cols);
                changed = true;
                break;
            }
            if (changed) {
                System.IO.File.WriteAllLines(path, lines, new System.Text.UTF8Encoding(false));
            }
        }

        /// <summary>删除 Setting 资产中该实体的那条数据(删节点后残留会导致运行时误读)</summary>
        static void RemoveSettingEntry(string assetName, string entitySign) {
            if (string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(entitySign))
                return;
            var setting = LoadSetting<Setting>(assetName);
            var listField = setting?.GetType().GetField("Datas");
            var list = listField?.GetValue(setting) as System.Collections.IList;
            if (list == null)
                return;
            for (int i = list.Count - 1; i >= 0; i--) {
                var item = list[i];
                if (item == null)
                    continue;
                var signField = item.GetType().GetField("SourceSign");
                if (signField == null)
                    continue;
                if ((signField.GetValue(item) as string) == entitySign) {
                    list.RemoveAt(i);
                    EditorUtility.SetDirty(setting);
                }
            }
        }

        /// <summary>
        /// 实时存储: 图资产有脏标记后延迟片刻自动落盘 覆盖字段编辑/加删节点/连线等所有改动路径
        /// </summary>
        protected override void Update() {
            base.Update();

            if (graph == null || !EditorUtility.IsDirty(graph)) {
                lastDirtyTime = 0;
                return;
            }

            if (lastDirtyTime == 0) {
                lastDirtyTime = EditorApplication.timeSinceStartup;
                return;
            }

            if (EditorApplication.timeSinceStartup - lastDirtyTime > autoSaveDelay) {
                lastDirtyTime = 0;
                SaveGraph(graph);
            }
        }

        protected override void OnDestroy() {
            SaveGraph(graph);
            graphView?.Dispose();
        }

        protected override void InitializeWindow(BaseGraph graph) {
            if (graphView == null)
                graphView = new LazyPanGraphView(this);
            rootView.Add(graphView);
            BuildEntityBanner(graph);
        }

        /// <summary>窗口顶部实体横幅 中文名+Sign 打开哪个实体一目了然</summary>
        void BuildEntityBanner(BaseGraph graph) {
            var old = rootView.Q<VisualElement>("entity-banner");
            if (old != null)
                rootView.Remove(old);
            string entitySign = graph != null ? graph.name : "";
            string entityName = GetEntityName(entitySign);
            var banner = new VisualElement { name = "entity-banner" };
            banner.style.flexDirection = UnityEngine.UIElements.FlexDirection.Row;
            banner.style.paddingLeft = 8;
            banner.style.paddingRight = 8;
            banner.style.paddingTop = 4;
            banner.style.paddingBottom = 4;
            var label = new UnityEngine.UIElements.Label(
                string.IsNullOrEmpty(entityName) ? entitySign : $"{entityName} ({entitySign})");
            label.style.fontSize = 14;
            label.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            banner.Add(label);
            rootView.Add(banner);
            titleContent = new UnityEngine.GUIContent(string.IsNullOrEmpty(entityName) ? "实体设置" : $"实体设置-{entityName}");
        }

        /// <summary>按实体 Sign 从 ObjConfig.csv 取中文名</summary>
        static string GetEntityName(string entitySign) {
            if (string.IsNullOrEmpty(entitySign))
                return "";
            var path = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "Csv", "ObjConfig.csv");
            string[] lines;
            try {
                lines = System.IO.File.ReadAllLines(path);
            } catch {
                return "";
            }
            foreach (var line in lines.Skip(3)) {
                var cols = line.Split(',');
                if (cols.Length < 4 || cols[0].Trim() != entitySign)
                    continue;
                return cols[3].Trim();
            }
            return "";
        }
    }
}