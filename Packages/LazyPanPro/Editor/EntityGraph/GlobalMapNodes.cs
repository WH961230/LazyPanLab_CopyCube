using System;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;

namespace LazyPan {
    /// <summary>
    /// 全局总览图的纯显示节点 基类只带展示信息 不带 Config 不参与运行时与回写
    /// 展示字段全部 HideInInspector 节点只剩标题+端口  Slim 高度布局才算得准
    /// </summary>
    [Serializable]
    public abstract class GlobalMapNode : BaseNode {
        /// <summary>面板标题 中文名</summary>
        [HideInInspector]
        public string Title = "";
        /// <summary>副标题 Sign/状态等</summary>
        [HideInInspector]
        public string Subtitle = "";
        /// <summary>Flow 分组(场景A/B/C) 用于布局分列</summary>
        [HideInInspector]
        public string FlowGroup = "";

        public override string name => Title;
    }

    /// <summary>Start 节点 总览入口 全局唯一</summary>
    [Serializable]
    [NodeMenuItem("LazyPan/总览/开始")]
    public class MapStartNode : GlobalMapNode {
        public override Color color => GlobalMapColors.Load().Start;

        [Output("启动器", allowMultiple = true)]
        public string Launches;
    }

    /// <summary>Launch 节点 游戏启动器 全局唯一</summary>
    [Serializable]
    [NodeMenuItem("LazyPan/总览/启动器")]
    public class MapLaunchNode : GlobalMapNode {
        public override Color color => GlobalMapColors.Load().Launch;

        [Input("开始", allowMultiple = false)]
        public string Start;

        [Output("场景", allowMultiple = true)]
        public string Flows;
    }

    /// <summary>Flow 节点 一场景一个</summary>
    [Serializable]
    [NodeMenuItem("LazyPan/总览/场景")]
    public class MapFlowNode : GlobalMapNode {
        public override Color color => GlobalMapColors.Load().Flow;

        [Input("启动器", allowMultiple = false)]
        public string Launch;

        [Output("实体", allowMultiple = true)]
        public string Entities;
    }

    /// <summary>实体节点 一实体一条(Flow+Sign 唯一)</summary>
    [Serializable]
    [NodeMenuItem("LazyPan/总览/实体")]
    public class MapEntityNode : GlobalMapNode {
        public override Color color => GlobalMapColors.Load().Entity;

        /// <summary>实体 Sign 双击跳转单实体图用</summary>
        [HideInInspector]
        public string EntitySign = "";
        /// <summary>实体 Type</summary>
        [HideInInspector]
        public string EntityType = "";

        [Input("场景", allowMultiple = false)]
        public string Flow;

        [Output("行为", allowMultiple = true)]
        public string Behaviours;
    }

    /// <summary>行为节点 一实体一挂载一条 不合并 线不交叉 仅被多实体复用的行为用配置色 独占默认色</summary>
    [Serializable]
    [NodeMenuItem("LazyPan/总览/行为")]
    public class MapBehaviourNode : GlobalMapNode {
        public override Color color {
            get {
                var colors = GlobalMapColors.Load();
                if (!Shared || string.IsNullOrEmpty(Title))
                    return colors.BehaviourDefault;
                return colors.GetBehaviourColor(Title);
            }
        }

        /// <summary>是否被多个实体复用 独占时强制默认色</summary>
        [HideInInspector]
        public bool Shared;

        /// <summary>行为 Sign</summary>
        [HideInInspector]
        public string BehaviourSign = "";
        /// <summary>有无图资产</summary>
        [HideInInspector]
        public bool HasGraph;
        /// <summary>归属实体 Sign 打开实体图用</summary>
        [HideInInspector]
        public string OwnerEntitySign = "";
        /// <summary>归属实体的行为中文名集合 打开实体图时传参用</summary>
        [HideInInspector]
        public List<string> OwnerBehaviourNames = new List<string>();

        [Input("实体", allowMultiple = false)]
        public string Entity;
    }
}
