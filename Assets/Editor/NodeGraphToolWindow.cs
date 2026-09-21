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

            // ObjConfig 为准: 每次打开严格按 ObjConfig.csv 该实体的行为列重建图
            // 图里多余的节点删掉 缺的按 Setting 回填补上 再落盘 保证图与清单一致
            string[] effectiveNames = GetBehaviourNames(entitySign);
            RebuildGraphFromObjConfig(graph, entitySign, effectiveNames);
            SaveGraph(graph);

            window.InitializeGraph(graph);
            window.Show();
        }

        /// <summary>
        /// ObjConfig 为准补节点: 只补清单里缺的 打开时绝不删节点
        /// 删节点只走保存时的弹窗确认(SyncRemovedNodes) 确认后才同步移除 csv 与 Setting
        /// 节点参数从对应 Setting 资产中按 SourceSign == 实体Sign 的那条加载
        /// </summary>
        static void RebuildGraphFromObjConfig(BaseGraph graph, string entitySign, string[] behaviourNames) {
            AddBehaviourNodes(graph, entitySign, behaviourNames);
        }

        /// <summary>行为中文名数组转 BehaviourSign 集合</summary>
        static HashSet<string> BehaviourSignsOf(string[] behaviourNames) {
            var wanted = new HashSet<string>();
            if (behaviourNames == null)
                return wanted;
            var nameToSign = new Dictionary<string, string>();
            foreach (var sign in BehaviourConfig.GetKeys()) {
                var cfg = BehaviourConfig.Get(sign);
                if (cfg != null && !string.IsNullOrEmpty(cfg.Name))
                    nameToSign[cfg.Name] = sign;
            }

            foreach (var rawName in behaviourNames) {
                string name = rawName?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;
                if (nameToSign.TryGetValue(name, out var sign))
                    wanted.Add(sign);
            }

            return wanted;
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
        /// 通用版: 按节点 Config 字段类型反查 Setting 类型 新行为无需加 case
        /// </summary>
        static void LoadNodeConfig(BaseNode node, string entitySign) {
            if (!(node is BehaviourGraphNode)) {
                return;
            }

            var configField = node.GetType().GetField("Config");
            if (configField == null) {
                return;
            }

            Type settingType = FindSettingTypeByDataType(configField.FieldType);
            if (settingType == null) {
                LogUtil.LogErrorFormat("未找到配置数据类型:{0} 对应的 Setting 请检查 Setting.Datas 元素类型!", configField.FieldType.Name);
                return;
            }

            var setting = LoadSettingByType(settingType);
            if (setting == null) {
                return;
            }

            var tryGet = settingType.GetMethod("TryGet");
            if (tryGet == null) {
                return;
            }

            object[] args = new object[] { entitySign, null };
            bool ok = (bool) tryGet.Invoke(setting, args);
            if (ok && args[1] != null) {
                configField.SetValue(node, args[1]);
                return;
            }

            //Setting 里没这条 且 Config 是 class(默认 null) 先 new 一个出来 不然节点上啥都填不了
            //struct 自带默认值不用管 class 必须手动实例化(struct 改 class 后新节点常见)
            if (!configField.FieldType.IsValueType && configField.GetValue(node) == null) {
                try {
                    object fresh = Activator.CreateInstance(configField.FieldType);
                    var signField = configField.FieldType.GetField("SourceSign");
                    if (signField != null && string.IsNullOrEmpty(signField.GetValue(fresh) as string))
                        signField.SetValue(fresh, entitySign);
                    configField.SetValue(node, fresh);
                } catch (Exception e) {
                    LogUtil.LogErrorFormat("节点 Config 自动实例化失败 类型:{0} 原因:{1}", configField.FieldType.Name, e.Message);
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
                lines = CsvEncoding.ReadAllLines(path);
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
                var configField = node.GetType().GetField("Config");
                if (configField == null)
                    continue;
                Type settingType = FindSettingTypeByDataType(configField.FieldType);
                if (settingType == null)
                    continue;
                string assetName = GetSettingAssetFileName(settingType);
                object graphData = configField.GetValue(node);
                //改名搬家: SourceSign 变了就把旧键名下的残留条目清掉 不然 Setting 越攒越多
                string nodeSign = GetSourceSign(graphData);
                if (!string.IsNullOrEmpty(node.LastSyncedSign) && node.LastSyncedSign != nodeSign) {
                    RemoveSettingEntry(assetName, node.LastSyncedSign);
                    changed = true;
                }

                changed |= SyncSettingData(assetName, entitySign, graphData);
                node.LastSyncedSign = nodeSign;
                //struct 类型是拷贝语义 把归一化后的值写回节点 否则节点与 Setting 会分叉
                configField.SetValue(node, graphData);
            }
            return changed;
        }

        static string GetSourceSign(object graphData) {
            if (graphData == null)
                return "";
            var sourceField = graphData.GetType().GetField("SourceSign");
            if (sourceField == null)
                return "";
            return sourceField.GetValue(graphData) as string ?? "";
        }

        /// <summary>
        /// 按节点 Config 字段类型反查 Setting 类型 通用版 新行为无需加映射 保留供外部查询用
        /// </summary>
        static string GetSettingAssetName(BehaviourGraphNode node) {
            var configField = node?.GetType().GetField("Config");
            if (configField == null) {
                return null;
            }

            return GetSettingAssetFileName(FindSettingTypeByDataType(configField.FieldType));
        }

        /// <summary>
        /// 按 Config 数据类型反查 Setting 类型 依据 Setting.Datas 列表元素类型 带缓存
        /// </summary>
        static readonly Dictionary<Type, Type> settingTypeCache = new Dictionary<Type, Type>();

        static Type FindSettingTypeByDataType(Type dataType) {
            if (dataType == null) {
                return null;
            }

            if (settingTypeCache.TryGetValue(dataType, out Type cached)) {
                return cached;
            }

            foreach (Type type in TypeCache.GetTypesDerivedFrom<Setting>()) {
                if (type.IsAbstract) {
                    continue;
                }

                var datasField = type.GetField("Datas");
                if (datasField == null || !datasField.FieldType.IsGenericType) {
                    continue;
                }

                Type[] args = datasField.FieldType.GetGenericArguments();
                if (args.Length == 1 && args[0] == dataType) {
                    settingTypeCache[dataType] = type;
                    return type;
                }
            }

            settingTypeCache[dataType] = null;
            return null;
        }

        /// <summary>
        /// 按 Setting 类型加载资产 找不到就按类型名自动新建 保证图里配完 Setting 一定有地方写
        /// </summary>
        static Setting LoadSettingByType(Type settingType) {
            string assetName = GetSettingAssetFileName(settingType);
            if (string.IsNullOrEmpty(assetName)) {
                return null;
            }

            string path = $"Assets/LazyPan/Bundles/Configs/Setting/{assetName}.asset";
            var setting = AssetDatabase.LoadAssetAtPath<Setting>(path);
            if (setting != null) {
                return setting;
            }

            setting = ScriptableObject.CreateInstance(settingType) as Setting;
            if (setting == null) {
                LogUtil.LogErrorFormat("Setting 资产自动创建失败 类型:{0}", settingType.Name);
                return null;
            }

            AssetDatabase.CreateAsset(setting, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"Setting 资产不存在已自动创建: {path}");
            return setting;
        }

        /// <summary>
        /// 按 Setting 类型反查资产文件名(不带扩展名) 先试类型名直连 找不到再按类型扫描目录 兼容旧资产改名残留
        /// </summary>
        static string GetSettingAssetFileName(Type settingType) {
            if (settingType == null) {
                return null;
            }

            string direct = $"Assets/LazyPan/Bundles/Configs/Setting/{settingType.Name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Setting>(direct) != null) {
                return settingType.Name;
            }

            string[] guids = AssetDatabase.FindAssets($"t:{settingType.Name}", new[] { "Assets/LazyPan/Bundles/Configs/Setting" });
            foreach (string guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset")) {
                    continue;
                }

                var setting = AssetDatabase.LoadAssetAtPath<Setting>(path);
                if (setting != null && setting.GetType() == settingType) {
                    return System.IO.Path.GetFileNameWithoutExtension(path);
                }
            }

            return settingType.Name;
        }

        /// <summary>
        /// 按资产文件名反查 Setting 类型 供资产缺失自动补建用
        /// </summary>
        static Type FindSettingTypeByAssetName(string assetName) {
            if (string.IsNullOrEmpty(assetName))
                return null;
            foreach (Type type in TypeCache.GetTypesDerivedFrom<Setting>()) {
                if (!type.IsAbstract && type.Name == assetName)
                    return type;
            }

            return null;
        }

        /// <summary>
        /// 按 BehaviourSign 反查节点类型 带缓存 供删除同步用
        /// </summary>
        static readonly Dictionary<string, Type> nodeTypeCache = new Dictionary<string, Type>();

        static Type FindNodeTypeByBehaviourSign(string behaviourSign) {
            if (string.IsNullOrEmpty(behaviourSign)) {
                return null;
            }

            if (nodeTypeCache.TryGetValue(behaviourSign, out Type cached)) {
                return cached;
            }

            foreach (Type type in TypeCache.GetTypesDerivedFrom<BehaviourGraphNode>()) {
                if (type.IsAbstract) {
                    continue;
                }

                var probe = Activator.CreateInstance(type) as BehaviourGraphNode;
                if (probe != null && probe.BehaviourSign == behaviourSign) {
                    nodeTypeCache[behaviourSign] = type;
                    return type;
                }
            }

            nodeTypeCache[behaviourSign] = null;
            return null;
        }

        static bool SyncSettingData(string assetName, string entitySign, object graphData) {
            if (graphData == null || string.IsNullOrEmpty(entitySign)) {
                if (graphData == null)
                    LogUtil.LogErrorFormat("Setting 回写跳过 实体:{0} 资产:{1} 节点 Config 为空(struct 改 class 后新节点常见) 请删节点重建", entitySign, assetName);
                return false;
            }
            var setting = LoadSetting<Setting>(assetName);
            if (setting == null) {
                //LoadSetting 按名直连拿不到时 按类型自动建一个 新行为第一次配会走到这里
                setting = LoadSettingByType(FindSettingTypeByAssetName(assetName));
                if (setting == null) {
                    LogUtil.LogErrorFormat("Setting 回写失败 实体:{0} 资产:{1} 加载不到也建不出来", entitySign, assetName);
                    return false;
                }
            }
            var listField = setting?.GetType().GetField("Datas");
            var list = listField?.GetValue(setting) as System.Collections.IList;
            var sourceField = graphData.GetType().GetField("SourceSign");
            if (list == null || sourceField == null || setting == null)
                return false;

            //图为准全量覆盖: 以节点里的 SourceSign 为键 本键旧条目全清后只写当前这一条
            //SourceSign 为空则回落到图的实体名 允许用户显式改归属 不再强制掰回
            string key = sourceField.GetValue(graphData) as string;
            if (string.IsNullOrEmpty(key)) {
                key = entitySign;
                sourceField.SetValue(graphData, key);
            }

            for (int i = list.Count - 1; i >= 0; i--) {
                var item = list[i];
                if (item == null || ReferenceEquals(item, graphData))
                    continue;
                var signField = item.GetType().GetField("SourceSign");
                if (signField == null)
                    continue;
                string itemSign = signField.GetValue(item) as string;
                if (itemSign == key || itemSign == entitySign)
                    list.RemoveAt(i);
            }

            if (ReferenceEquals(list.Count > 0 ? list[list.Count - 1] : null, graphData)) {
                EditorUtility.SetDirty(setting);
                return true;
            }

            bool alreadyIn = false;
            foreach (var item in list) {
                if (ReferenceEquals(item, graphData)) {
                    alreadyIn = true;
                    break;
                }
            }

            if (!alreadyIn) {
                var addMethod = list.GetType().GetMethod("Add");
                if (addMethod == null)
                    return false;
                addMethod.Invoke(list, new[] { graphData });
            }

            EditorUtility.SetDirty(setting);
            return true;
        }

        static void SaveGraph(BaseGraph graph) {
            if (graph == null)
                return;
            // 图为标准: 图上增删都反写 ObjConfig.csv 清单与 Setting 条目
            SyncAddedNodes(graph);
            SyncRemovedNodes(graph);
            // 图内数据回写到对应 Setting 资产 保持一一对应(按图资产名=实体Sign 定位 Setting 条目)
            SyncGraphToSettings(graph, graph.name);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 新增同步: 图上有但 ObjConfig 清单里没有的行为 视为用户在图中新增的
        /// 直接反写清单 不弹窗 下次打开不再重复处理 Setting 条目由 SyncGraphToSettings 回写
        /// </summary>
        static void SyncAddedNodes(BaseGraph graph) {
            string entitySign = graph != null ? graph.name : null;
            if (string.IsNullOrEmpty(entitySign))
                return;
            var names = GetBehaviourNames(entitySign);
            var fileSet = new HashSet<string>(names ?? new string[0]);
            var signToName = new Dictionary<string, string>();
            foreach (var sign in BehaviourConfig.GetKeys()) {
                var cfg = BehaviourConfig.Get(sign);
                if (cfg != null && !string.IsNullOrEmpty(cfg.Name))
                    signToName[cfg.Sign] = cfg.Name;
            }
            var ordered = new List<string>(names ?? new string[0]);
            bool changed = false;
            foreach (var node in graph.nodes.OfType<BehaviourGraphNode>()) {
                if (string.IsNullOrEmpty(node.BehaviourSign))
                    continue;
                if (!signToName.TryGetValue(node.BehaviourSign, out string behaviourName))
                    continue;
                if (fileSet.Contains(behaviourName))
                    continue;
                ordered.Add(behaviourName);
                fileSet.Add(behaviourName);
                confirmedRemoves.Remove(entitySign + "|" + node.BehaviourSign);
                changed = true;
            }

            if (changed) {
                WriteBehaviourNames(entitySign, ordered.ToArray());
            }
        }

        /// <summary>按实体 Sign 把行为中文名数组写回 ObjConfig.csv(保序 去空去重)</summary>
        static void WriteBehaviourNames(string entitySign, string[] behaviourNames) {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, "Csv", "ObjConfig.csv");
            string[] lines;
            try {
                lines = CsvEncoding.ReadAllLines(path);
            } catch {
                return;
            }
            bool changed = false;
            for (int i = 3; i < lines.Length; i++) {
                var cols = lines[i].Split(',');
                if (cols.Length < 6 || cols[0].Trim() != entitySign)
                    continue;
                var cleaned = CleanNames(behaviourNames);
                cols[5] = string.Join("|", cleaned);
                lines[i] = string.Join(",", cols);
                changed = true;
                break;
            }

            if (changed) {
                System.IO.File.WriteAllLines(path, lines, new System.Text.UTF8Encoding(false));
            }
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
            var graphSigns = graph.nodes.OfType<BehaviourGraphNode>()
                .Select(n => n.BehaviourSign).Where(s => !string.IsNullOrEmpty(s)).ToHashSet();
            var nameToSign = new Dictionary<string, string>();
            foreach (var sign in BehaviourConfig.GetKeys()) {
                var cfg = BehaviourConfig.Get(sign);
                if (cfg != null && !string.IsNullOrEmpty(cfg.Name))
                    nameToSign[cfg.Name] = sign;
            }
            //清单里有但图上没了的 弹窗确认后才删 csv 与 Setting 一起移除
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
                        graphSigns.Add(sign);
                        continue;
                    }
                    confirmedRemoves.Add(key);
                }
                RemoveBehaviourFromObjConfig(entitySign, name);
                RemoveSettingEntryBySign(sign, entitySign);
            }
            //清单里没有图上也没有的残留 Setting 条目一起扫掉(图为准 无节点即无条目)
            foreach (var type in TypeCache.GetTypesDerivedFrom<BehaviourGraphNode>()) {
                if (type.IsAbstract)
                    continue;
                var probe = Activator.CreateInstance(type) as BehaviourGraphNode;
                if (probe == null || string.IsNullOrEmpty(probe.BehaviourSign))
                    continue;
                if (graphSigns.Contains(probe.BehaviourSign))
                    continue;
                RemoveSettingEntryBySign(probe.BehaviourSign, entitySign);
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

        /// <summary>按 BehaviourSign 反查节点再定位 Setting 资产 通用版 新行为无需加映射</summary>
        static void RemoveSettingEntryBySign(string behaviourSign, string entitySign) {
            Type nodeType = FindNodeTypeByBehaviourSign(behaviourSign);
            if (nodeType == null) {
                return;
            }

            var configField = nodeType.GetField("Config");
            if (configField == null) {
                return;
            }

            Type settingType = FindSettingTypeByDataType(configField.FieldType);
            if (settingType == null) {
                return;
            }

            RemoveSettingEntry(GetSettingAssetFileName(settingType), entitySign);
        }

        /// <summary>
        /// 生效行为名解析已废弃: 图为标准 打开时不再读 ObjConfig.csv
        /// 保留空壳防外部调用报错 一律返回空 由调用方传入的面板勾选驱动补节点
        /// </summary>
        static string[] ResolveBehaviourNames(string entitySign, string[] passedNames) {
            return CleanNames(passedNames);
        }

        /// <summary>清洗行为中文名: 去空去重保序</summary>
        static string[] CleanNames(string[] names) {
            if (names == null || names.Length == 0) {
                return new string[0];
            }

            var list = new List<string>();
            var seen = new HashSet<string>();
            foreach (var raw in names) {
                string t = raw?.Trim();
                if (string.IsNullOrEmpty(t) || !seen.Add(t)) {
                    continue;
                }

                list.Add(t);
            }

            return list.ToArray();
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
                lines = CsvEncoding.ReadAllLines(path);
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
                CsvEncoding.WriteAllLines(path, lines);
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
        /// 实时存储: 节点 Config 是内嵌对象 改参数经常不触发 graph 脏标记
        /// 所以每帧对全图 Config 做快照比对 变了就落盘 覆盖字段编辑/加删节点/连线等所有改动路径
        /// </summary>
        string lastConfigSnapshot;

        static string BuildConfigSnapshot(BaseGraph graph) {
            if (graph == null)
                return "";
            var sb = new System.Text.StringBuilder();
            sb.Append(graph.nodes.Count);
            sb.Append(';');
            foreach (var node in graph.nodes.OfType<BehaviourGraphNode>()) {
                sb.Append(node.BehaviourSign);
                sb.Append('|');
                var configField = node.GetType().GetField("Config");
                try {
                    object val = configField != null ? configField.GetValue(node) : null;
                    sb.Append(val != null ? JsonUtility.ToJson(val) : "null");
                } catch {
                    sb.Append("err");
                }

                sb.Append(';');
            }

            return sb.ToString();
        }

        protected override void Update() {
            base.Update();

            if (graph == null) {
                lastConfigSnapshot = null;
                return;
            }

            if (EditorUtility.IsDirty(graph)) {
                SaveGraph(graph);
                lastConfigSnapshot = BuildConfigSnapshot(graph);
                return;
            }

            string snapshot = BuildConfigSnapshot(graph);
            if (lastConfigSnapshot != null && snapshot != lastConfigSnapshot)
                SaveGraph(graph);
            lastConfigSnapshot = snapshot;
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