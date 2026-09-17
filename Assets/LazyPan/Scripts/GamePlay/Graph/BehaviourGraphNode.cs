using System;
using UnityEngine;
using GraphProcessor;

namespace LazyPan {
    /// <summary>
    /// 行为图节点基类 子类各自携带该行为在原 Setting 资产中的那条数据(即 XxxSettingData)
    /// 节点即配置 运行时由图直读合成 不再依赖 Setting 资产
    /// </summary>
    [Serializable]
    public abstract class BehaviourGraphNode : BaseNode {
        /// <summary>对应 BehaviourConfig.csv 的 Sign 运行时按此把配置分发给对应行为</summary>
        public abstract string BehaviourSign { get; }

        /// <summary>配置是否已初始化 已初始化后不再从Setting回填</summary>
        [HideInInspector]
        public bool NodeInitialized;
    }

    /// <summary>
    /// 死亡行为节点 嵌入 DeathSettingData 一条 对应原 DeathSetting.Datas 中的一项
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/死亡")]
    public class BehaviourNode_Death : BehaviourGraphNode {
        public DeathSettingData Config;

        public override string name => "死亡";
        public override string BehaviourSign => nameof(Behaviour_Event_Death);
    }
}
