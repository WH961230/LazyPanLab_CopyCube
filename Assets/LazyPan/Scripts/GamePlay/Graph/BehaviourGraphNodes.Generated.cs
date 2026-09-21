using System;
using GraphProcessor;

namespace LazyPan {
    /// <summary>
    /// 追踪实体行为节点 对应 TrackingEntitySettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/追踪实体")]
    public class BehaviourNode_TrackingEntity : BehaviourGraphNode {
        public TrackingEntitySettingData Config;
        public override string name => "追踪实体";
        public override string BehaviourSign => nameof(Behaviour_Auto_TrackingEntityByNavMeshAgent);
    }

    /// <summary>
    /// 延时生成实体行为节点 对应 DelayGenerateEntitySettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/延时生成实体")]
    public class BehaviourNode_DelayGenerate : BehaviourGraphNode {
        public DelayGenerateEntitySettingData Config;
        public override string name => "延时生成实体";
        public override string BehaviourSign => nameof(Behaviour_Event_DelayGenerateEntity);
    }

    /// <summary>
    /// 实体挂载界面行为节点 对应 EntityUIBindSettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/实体挂载界面")]
    public class BehaviourNode_EntityUIBinder : BehaviourGraphNode {
        public EntityUIBindSettingData Config;
        public override string name => "实体挂载界面";
        public override string BehaviourSign => nameof(Behaviour_Event_EntityUIBinder);
    }

    /// <summary>
    /// 控制器移动行为节点 对应 InputWASDMoveSettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/控制器移动")]
    public class BehaviourNode_InputWASDMove : BehaviourGraphNode {
        public InputWASDMoveSettingData Config;
        public override string name => "控制器移动";
        public override string BehaviourSign => nameof(Behaviour_Auto_InputWASDMove);
    }

    /// <summary>
    /// 波数管理行为节点 对应 WaveManagerSettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/波数管理")]
    public class BehaviourNode_WaveManager : BehaviourGraphNode {
        public WaveManagerSettingData Config;
        public override string name => "波数管理";
        public override string BehaviourSign => nameof(Behaviour_Event_WaveManager);
    }

    /// <summary>
    /// 装备挂载行为节点 对应 EquipmentMountSettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/装备挂载")]
    public class BehaviourNode_EquipmentMount : BehaviourGraphNode {
        public EquipmentMountSettingData Config;
        public override string name => "装备挂载";
        public override string BehaviourSign => nameof(Behaviour_Event_EquipmentMountManager);
    }

    /// <summary>
    /// 开头Logo行为节点 对应 BeginLogoSettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/开头Logo")]
    public class BehaviourNode_BeginLogo : BehaviourGraphNode {
        public BeginLogoSettingData Config;
        public override string name => "开头Logo";
        public override string BehaviourSign => nameof(Behaviour_Event_BeginLogo);
    }

    /// <summary>
    /// 实体触发控制行为节点 对应 EntityTriggerControllerSettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/实体触发控制")]
    public class BehaviourNode_EntityTriggerController : BehaviourGraphNode {
        public EntityTriggerControllerSettingData Config;
        public override string name => "实体触发控制";
        public override string BehaviourSign => nameof(Behaviour_Auto_EntityTriggerController);
    }

    /// <summary>
    /// 实体参数值行为节点 对应 ParamValueSettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/实体参数值")]
    public class BehaviourNode_ParamValue : BehaviourGraphNode {
        public ParamValueSettingData Config;
        public override string name => "实体参数值";
        public override string BehaviourSign => nameof(Behaviour_Event_ParamValue);
    }

    /// <summary>
    /// 传送流程行为节点 对应 TeleportFlowSettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/传送流程")]
    public class BehaviourNode_TeleportFlow : BehaviourGraphNode {
        public TeleportFlowSettingData Config;
        public override string name => "传送流程";
        public override string BehaviourSign => nameof(Behaviour_Event_TeleportFlow);
    }

    /// <summary>
    /// 死亡行为节点 对应 DeathSettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/死亡")]
    public class BehaviourNode_Death : BehaviourGraphNode {
        public DeathSettingData Config;
        public override string name => "死亡";
        public override string BehaviourSign => nameof(Behaviour_Event_Death);
    }

    /// <summary>
    /// 阶段进度行为节点 对应 StageProgressSettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/阶段进度")]
    public class BehaviourNode_StageProgress : BehaviourGraphNode {
        public StageProgressSettingData Config;
        public override string name => "阶段进度";
        public override string BehaviourSign => nameof(Behaviour_Event_StageProgress);
    }

    /// <summary>
    /// 屏幕状态展示行为节点 对应 UIStatusDisplaySettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/屏幕状态展示")]
    public class BehaviourNode_UIStatusDisplay : BehaviourGraphNode {
        public UIStatusDisplaySettingData Config;
        public override string name => "屏幕状态展示";
        public override string BehaviourSign => nameof(Behaviour_Event_UIStatusDisplay);
    }

    /// <summary>
    /// UI三选一行为节点 对应 UIPickOneOfThreeSettingData 一条
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/UI三选一")]
    public class BehaviourNode_UIPickOneOfThree : BehaviourGraphNode {
        public UIPickOneOfThreeSettingData Config;
        public override string name => "UI三选一";
        public override string BehaviourSign => nameof(Behaviour_Event_UIPickOneOfThree);
    }

}
