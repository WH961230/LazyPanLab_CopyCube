using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GraphProcessor;

namespace LazyPan {
    /// <summary>
    /// 全局实体行为总览入口 只读聚合 ObjConfig.csv 的 Flow-Entity-Behaviour 三层树
    /// 按列布局: 场景 | 实体 | 行为 双击实体节点跳转单实体图
    /// </summary>
    public class GlobalEntityMapWindow : BaseGraphWindow {
        const string graphFolder = "Assets/LazyPan/Bundles/Configs/Graph";
        static BaseGraphView lastView;

        [MenuItem("Tools/LazyPan/全局总览 _F2", priority = 0)]
        public static void Open() {
            var window = CreateWindow<GlobalEntityMapWindow>();
            window.titleContent = new GUIContent("全局总览");
            window.Rebuild();
            window.Show();
        }

        protected override void InitializeWindow(BaseGraph graph) {
            titleContent = new GUIContent("全局总览");
            if (graphView == null)
                graphView = new GlobalMapGraphView(this);
            lastView = graphView as BaseGraphView;
            rootView.Add(graphView);
            BuildToolbar();
            RefreshGraph();
        }

        void BuildToolbar() {
            var old = rootView.Q<VisualElement>("global-map-toolbar");
            if (old != null)
                rootView.Remove(old);
            var toolbar = new VisualElement { name = "global-map-toolbar" };
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.paddingLeft = 8;
            toolbar.style.paddingRight = 8;
            toolbar.style.paddingTop = 4;
            toolbar.style.paddingBottom = 4;
            var layout = new Button(() => AutoLayout()) { text = "自动复原" };
            toolbar.Add(layout);
            rootView.Add(toolbar);
        }

        /// <summary>
        /// 重建内存图(不存资产) 按场景-实体-行为三列排布
        /// </summary>
        void Rebuild() {
            var memGraph = ScriptableObject.CreateInstance<BaseGraph>();
            InitializeGraph(memGraph);
        }

        void RefreshGraph() {
            if (graph == null)
                return;
            foreach (var n in graph.nodes.ToList())
                graph.RemoveNode(n);

            var rows = LoadRows();
            var filtered = rows;

            // Start → Launch → Flow... 全局唯一入口链
            var startNode = BaseNode.CreateFromType<MapStartNode>(Vector2.zero) as MapStartNode;
            startNode.Title = "Start";
            startNode.Subtitle = "入口";
            graph.AddNode(startNode);

            var launchNode = BaseNode.CreateFromType<MapLaunchNode>(Vector2.zero) as MapLaunchNode;
            launchNode.Title = "Launch";
            launchNode.Subtitle = "启动器";
            graph.AddNode(launchNode);
            graph.Connect(launchNode.GetPort("Start", null), startNode.GetPort("Launches", null));

            var flows = filtered.Select(r => r.Flow).Distinct().ToList();
            var flowNodes = new Dictionary<string, MapFlowNode>();
            foreach (var flow in flows) {
                var node = BaseNode.CreateFromType<MapFlowNode>(Vector2.zero) as MapFlowNode;
                node.Title = flow;
                node.Subtitle = "场景";
                node.FlowGroup = flow;
                graph.AddNode(node);
                flowNodes[flow] = node;
                graph.Connect(node.GetPort("Launch", null), launchNode.GetPort("Flows", null));
            }

            string lastFlow = null;
            var entityNodes = new Dictionary<string, MapEntityNode>();
            foreach (var row in filtered) {
                if (row.Flow != lastFlow)
                    lastFlow = row.Flow;
                var node = BaseNode.CreateFromType<MapEntityNode>(Vector2.zero) as MapEntityNode;
                node.Title = row.Name;
                node.Subtitle = $"{row.Sign} [{row.Type}]";
                node.FlowGroup = row.Flow;
                node.EntitySign = row.Sign;
                node.EntityType = row.Type;
                graph.AddNode(node);
                entityNodes[row.Flow + "|" + row.Sign] = node;

                if (flowNodes.TryGetValue(row.Flow, out var flowNode)) {
                    graph.Connect(node.GetPort("Flow", null), flowNode.GetPort("Entities", null));
                }
            }

            // 行为不合并: 一实体一挂载一条 实体居中到自己的行为块 连线扇形散开不交叉
            // 先统计每个行为被几个实体复用 独占的强制默认色 仅复用的用配置色
            var behaviourUseCount = new Dictionary<string, int>();
            foreach (var row in filtered) {
                foreach (var bName in row.Behaviours.Distinct()) {
                    if (behaviourUseCount.ContainsKey(bName))
                        behaviourUseCount[bName]++;
                    else
                        behaviourUseCount[bName] = 1;
                }
            }
            foreach (var row in filtered) {
                string key = row.Flow + "|" + row.Sign;
                if (!entityNodes.TryGetValue(key, out var entityNode))
                    continue;
                foreach (var bName in row.Behaviours.Distinct()) {
                    var bNode = BaseNode.CreateFromType<MapBehaviourNode>(Vector2.zero) as MapBehaviourNode;
                    bNode.Title = bName;
                    bNode.Subtitle = "行为";
                    bNode.FlowGroup = row.Flow;
                    bNode.OwnerEntitySign = row.Sign;
                    bNode.OwnerBehaviourNames = new List<string>(row.Behaviours);
                    bNode.Shared = behaviourUseCount.TryGetValue(bName, out int count) && count > 1;
                    graph.AddNode(bNode);

                    graph.Connect(bNode.GetPort("Entity", null), entityNode.GetPort("Behaviours", null));
                }
            }

            // 数据层建完后重建视图层 否则只改数据不动画面(删除后复原无反应的根因)
            graphView?.Initialize(graph);

            AutoLayout();
        }

        /// <summary>
        /// 自上而下树形自动布局 Flow列 | 实体列 | 行为列
        /// 先把缺的/错的连线按初始规则补齐删掉 再排位置 两阶段保证不重叠
        /// </summary>
        void AutoLayout() {
            // 节点被删过则修线修不回来 直接按 CSV 重建整张图
            if (HasDeletedNodes()) {
                RefreshGraph();
                return;
            }
            RepairEdges();
            AutoLayoutPass();
            // 连排三遍: 每遍用上一遍渲染出的真实高度 80ms 间隔让 UI 渲染跟上
            // 最终遍之后再等一遍渲染周期 彻底收敛
            graphView?.schedule.Execute(() => {
                AutoLayoutPass();
                graphView?.schedule.Execute(() => AutoLayoutPass()).ExecuteLater(80);
            }).ExecuteLater(80);
        }

        /// <summary>
        /// 是否有节点被用户删掉: 按 CSV 应有数量 vs 图上实际数量 行为数按去重后算
        /// </summary>
        bool HasDeletedNodes() {
            if (graph == null)
                return false;
            var rows = LoadRows();
            int expectFlow = rows.Select(r => r.Flow).Distinct().Count();
            int expectEntity = rows.Count;
            int expectBehaviour = rows.Sum(r => r.Behaviours.Distinct().Count());
            int actualFlow = graph.nodes.OfType<MapFlowNode>().Count();
            int actualEntity = graph.nodes.OfType<MapEntityNode>().Count();
            int actualBehaviour = graph.nodes.OfType<MapBehaviourNode>().Count();
            int actualStart = graph.nodes.OfType<MapStartNode>().Count();
            int actualLaunch = graph.nodes.OfType<MapLaunchNode>().Count();
            return actualStart < 1 || actualLaunch < 1
                || actualFlow < expectFlow
                || actualEntity < expectEntity
                || actualBehaviour < expectBehaviour;
        }

        /// <summary>
        /// 按初始规则修复连线: 删掉跨层/跨归属的错线 补齐缺的正确线
        /// Start→Launch→Flow→Entity→Behaviour 归属按 FlowGroup/OwnerEntitySign 判定
        /// </summary>
        void RepairEdges() {
            if (graph == null)
                return;

            var expected = new HashSet<string>();
            foreach (var n in graph.nodes)
                CollectExpectedEdges(n, expected);

            // 删错线(不在期望集合里)
            foreach (var e in graph.edges.ToList()) {
                if (!expected.Contains(EdgeKey(e.inputNode, e.inputFieldName, e.outputNode, e.outputFieldName)))
                    graph.Disconnect(e);
            }

            // 补缺线(期望有 实际无)
            var actual = new HashSet<string>();
            foreach (var e in graph.edges)
                actual.Add(EdgeKey(e.inputNode, e.inputFieldName, e.outputNode, e.outputFieldName));
            foreach (var n in graph.nodes)
                ConnectMissing(n, actual);
        }

        /// <summary>收集该节点按初始规则应有的连线(输出端视角)</summary>
        void CollectExpectedEdges(BaseNode node, HashSet<string> expected) {
            switch (node) {
                case MapStartNode start: {
                    var launch = graph.nodes.OfType<MapLaunchNode>().FirstOrDefault();
                    if (launch != null)
                        expected.Add(EdgeKey(launch, "Start", start, "Launches"));
                    break;
                }
                case MapFlowNode flow: {
                    var launch = graph.nodes.OfType<MapLaunchNode>().FirstOrDefault();
                    if (launch != null)
                        expected.Add(EdgeKey(flow, "Launch", launch, "Flows"));
                    foreach (var entity in graph.nodes.OfType<MapEntityNode>().Where(e => e.FlowGroup == flow.FlowGroup))
                        expected.Add(EdgeKey(entity, "Flow", flow, "Entities"));
                    break;
                }
                case MapEntityNode entity: {
                    foreach (var b in graph.nodes.OfType<MapBehaviourNode>().Where(b => b.OwnerEntitySign == entity.EntitySign && b.FlowGroup == entity.FlowGroup))
                        expected.Add(EdgeKey(b, "Entity", entity, "Behaviours"));
                    break;
                }
            }
        }

        /// <summary>补上该节点缺失的连线(输出端视角 实际集合同步更新防重复)</summary>
        void ConnectMissing(BaseNode node, HashSet<string> actual) {
            switch (node) {
                case MapStartNode start: {
                    var launch = graph.nodes.OfType<MapLaunchNode>().FirstOrDefault();
                    if (launch != null)
                        TryConnect(launch, "Start", start, "Launches", actual);
                    break;
                }
                case MapFlowNode flow: {
                    var launch = graph.nodes.OfType<MapLaunchNode>().FirstOrDefault();
                    if (launch != null)
                        TryConnect(flow, "Launch", launch, "Flows", actual);
                    foreach (var entity in graph.nodes.OfType<MapEntityNode>().Where(e => e.FlowGroup == flow.FlowGroup))
                        TryConnect(entity, "Flow", flow, "Entities", actual);
                    break;
                }
                case MapEntityNode entity: {
                    foreach (var b in graph.nodes.OfType<MapBehaviourNode>().Where(b => b.OwnerEntitySign == entity.EntitySign && b.FlowGroup == entity.FlowGroup))
                        TryConnect(b, "Entity", entity, "Behaviours", actual);
                    break;
                }
            }
        }

        void TryConnect(BaseNode inputNode, string inputField, BaseNode outputNode, string outputField, HashSet<string> actual) {
            string key = EdgeKey(inputNode, inputField, outputNode, outputField);
            if (actual.Contains(key))
                return;
            var inPort = inputNode?.GetPort(inputField, null);
            var outPort = outputNode?.GetPort(outputField, null);
            if (inPort == null || outPort == null)
                return;
            var newEdge = graph.Connect(inPort, outPort);
            actual.Add(key);
            // 数据层连上后补画视图层 否则只改数据不画线(删线后复原无反应的根因)
            AddEdgeView(newEdge);
        }

        /// <summary>按数据边补画连线视图(照抄库 InitializeEdgeViews 的画法)</summary>
        void AddEdgeView(SerializableEdge edge) {
            var view = graphView as BaseGraphView;
            if (view == null || edge == null || edge.inputNode == null || edge.outputNode == null)
                return;
            if (!view.nodeViewsPerNode.TryGetValue(edge.inputNode, out var inputNodeView) || inputNodeView == null)
                return;
            if (!view.nodeViewsPerNode.TryGetValue(edge.outputNode, out var outputNodeView) || outputNodeView == null)
                return;
            var edgeView = view.CreateEdgeView();
            edgeView.userData = edge;
            edgeView.input = inputNodeView.GetPortViewFromFieldName(edge.inputFieldName, edge.inputPortIdentifier);
            edgeView.output = outputNodeView.GetPortViewFromFieldName(edge.outputFieldName, edge.outputPortIdentifier);
            if (edgeView.input == null || edgeView.output == null)
                return;
            view.ConnectView(edgeView);
        }

        static string EdgeKey(BaseNode inputNode, string inputField, BaseNode outputNode, string outputField) {
            string inId = inputNode != null ? inputNode.GUID : "?";
            string outId = outputNode != null ? outputNode.GUID : "?";
            return $"{inId}.{inputField}<-{outId}.{outputField}";
        }

        void AutoLayoutPass() {
            if (graph == null || graph.nodes.Count == 0)
                return;

            const float colStartX = 40f;
            const float colLaunchX = 260f;
            const float colFlowX = 480f;
            const float colEntityX = 740f;
            const float colBehaviourX = 1060f;
            const float rowGap = 48f;
            const float colGap = 96f;

            var view = graphView as BaseGraphView;
            var heights = new Dictionary<BaseNode, float>();
            foreach (var n in graph.nodes) {
                float h = 0f;
                if (view != null && view.nodeViewsPerNode.TryGetValue(n, out var nodeView) && nodeView != null)
                    h = nodeView.layout.height;
                if (h < 10f)
                    h = EstimateNodeHeight(n);
                heights[n] = h + 16f; // 留安全边距
            }

            var flows = graph.nodes.OfType<MapFlowNode>().OrderBy(n => n.Title).ToList();
            float cursorY = 60f;

            foreach (var flow in flows) {
                var entities = graph.nodes.OfType<MapEntityNode>()
                    .Where(e => e.FlowGroup == flow.FlowGroup)
                    .OrderBy(e => e.Title)
                    .ToList();

                float flowBlockTop = cursorY;
                foreach (var entity in entities) {
                    var behaviours = graph.nodes.OfType<MapBehaviourNode>()
                        .Where(b => IsChildOf(entity, b))
                        .OrderBy(b => b.Title)
                        .ToList();

                    float entityH = heights[entity];
                    float entityTop;
                    if (behaviours.Count == 0) {
                        entityTop = cursorY;
                        cursorY += entityH + rowGap;
                    } else {
                        // 先排自己这坨行为 再把实体居中到中间 连线扇形不交叉
                        float blockTop = cursorY;
                        foreach (var b in behaviours) {
                            SetNodePos(b, colBehaviourX, cursorY);
                            cursorY += heights[b] + rowGap;
                        }
                        float blockBottom = cursorY - rowGap;
                        float blockCenter = (blockTop + blockBottom) / 2f;
                        entityTop = blockCenter - entityH / 2f;
                        if (entityTop < blockTop)
                            entityTop = blockTop;
                    }
                    SetNodePos(entity, colEntityX, entityTop);
                }

                float flowBlockBottom = cursorY - rowGap;
                float flowH = heights[flow];
                float flowTop = (flowBlockBottom - flowBlockTop) / 2f + flowBlockTop - flowH / 2f;
                if (flowTop < flowBlockTop)
                    flowTop = flowBlockTop;
                SetNodePos(flow, colFlowX, flowTop);

                cursorY = flowBlockBottom + rowGap + colGap;
            }

            // Start / Launch 居中到全部内容中间
            var startNode = graph.nodes.OfType<MapStartNode>().FirstOrDefault();
            var launchNode = graph.nodes.OfType<MapLaunchNode>().FirstOrDefault();
            if (startNode != null && launchNode != null) {
                float totalBottom = cursorY - rowGap - colGap;
                float midY = (60f + totalBottom) / 2f;
                SetNodePos(launchNode, colLaunchX, midY - heights[launchNode] / 2f);
                SetNodePos(startNode, colStartX, midY - heights[startNode] / 2f);
            }
        }

        /// <summary>行为节点是否连着指定实体节点(按连线判定 不按名字猜)</summary>
        static bool IsChildOf(MapEntityNode entity, MapBehaviourNode behaviour) {
            foreach (var edge in behaviour.GetAllEdges()) {
                if (edge.inputNode == behaviour && edge.outputNode == entity)
                    return true;
            }
            return false;
        }

        static void SetNodePos(BaseNode node, float x, float y) {
            var rect = node.position;
            rect.x = x;
            rect.y = y;
            node.position = rect;
            // 视图缓存的位置也要同步 否则只改数据不动画面
            var view = GetView(node);
            view?.SetPosition(rect);
        }

        static BaseNodeView GetView(BaseNode node) {
            var gv = lastView;
            if (gv != null && gv.nodeViewsPerNode.TryGetValue(node, out var nodeView))
                return nodeView;
            return null;
        }

        /// <summary>
        /// 节点高度估算: Slim 节点只有标题行+端口行
        /// </summary>
        static float EstimateNodeHeight(BaseNode node) {
            if (node is MapEntityNode)
                return 120f;
            return 96f;
        }

        struct EntityRow {
            public string Flow;
            public string Sign;
            public string Type;
            public string Name;
            public List<string> Behaviours;
        }

        static List<EntityRow> LoadRows() {
            var result = new List<EntityRow>();
            var path = Path.Combine(Application.streamingAssetsPath, "Csv", "ObjConfig.csv");
            string[] lines;
            try {
                lines = File.ReadAllLines(path);
            } catch {
                return result;
            }
            for (int i = 3; i < lines.Length; i++) {
                var cols = lines[i].Split(',');
                if (cols.Length < 6)
                    continue;
                var sign = cols[0].Trim();
                if (string.IsNullOrEmpty(sign))
                    continue;
                var behaviours = new List<string>();
                foreach (var b in cols[5].Split('|')) {
                    var name = b.Trim();
                    if (!string.IsNullOrEmpty(name))
                        behaviours.Add(name);
                }
                result.Add(new EntityRow {
                    Flow = cols[1].Trim(),
                    Sign = sign,
                    Type = cols[2].Trim(),
                    Name = cols[3].Trim(),
                    Behaviours = behaviours,
                });
            }
            return result;
        }
    }

    /// <summary>总览图视图 实体/行为节点右键打开对应实体图</summary>
    public class GlobalMapGraphView : BaseGraphView {
        public GlobalMapGraphView(EditorWindow window) : base(window) {
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
            base.BuildContextualMenu(evt);
            if (!(evt.target is BaseNodeView view))
                return;
            if (view.nodeTarget is MapEntityNode entity) {
                evt.menu.AppendAction("打开实体图", _ => {
                    var names = NodeGraphToolWindow.GetBehaviourNames(entity.EntitySign);
                    NodeGraphToolWindow.OpenForEntity(entity.EntitySign, names);
                });
            } else if (view.nodeTarget is MapBehaviourNode behaviour) {
                evt.menu.AppendAction("打开实体图", _ => {
                    var names = behaviour.OwnerBehaviourNames.Count > 0
                        ? behaviour.OwnerBehaviourNames.ToArray()
                        : NodeGraphToolWindow.GetBehaviourNames(behaviour.OwnerEntitySign);
                    NodeGraphToolWindow.OpenForEntity(behaviour.OwnerEntitySign, names);
                });
            }
        }
    }
}
