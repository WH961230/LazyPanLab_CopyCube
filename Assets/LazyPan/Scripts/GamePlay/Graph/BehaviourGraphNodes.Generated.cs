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
    /// 体型扩散行为节点 对应 BodyExpandSettingData 一条(占位挂钩 参数走 Data)
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/体型扩散")]
    public class BehaviourNode_BodyExpand : BehaviourGraphNode {
        public BodyExpandSettingData Config;
        public override string name => "体型扩散";
        public override string BehaviourSign => nameof(Behaviour_Auto_BodyExpand);
    }

    /// <summary>
    /// 接触伤害行为节点 对应 ContactDamageSettingData 一条(占位挂钩 参数走 Data)
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/接触伤害")]
    public class BehaviourNode_ContactDamage : BehaviourGraphNode {
        public ContactDamageSettingData Config;
        public override string name => "接触伤害";
        public override string BehaviourSign => nameof(Behaviour_Auto_ContactDamage);
    }

    /// <summary>
    /// 寿命行为节点 对应 LifeTimeoutSettingData 一条(占位挂钩 参数走 Data)
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/寿命")]
    public class BehaviourNode_LifeTimeout : BehaviourGraphNode {
        public LifeTimeoutSettingData Config;
        public override string name => "寿命";
        public override string BehaviourSign => nameof(Behaviour_Auto_LifeTimeout);
    }

    /// <summary>
    /// 跟随主人行为节点 对应 FollowHolderSettingData 一条(占位挂钩 参数走 Data)
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/跟随主人")]
    public class BehaviourNode_FollowHolder : BehaviourGraphNode {
        public FollowHolderSettingData Config;
        public override string name => "跟随主人";
        public override string BehaviourSign => nameof(Behaviour_Auto_FollowHolder);
    }

    /// <summary>
    /// 飞行追踪行为节点 对应 FlyTrackSettingData 一条(占位挂钩 参数走 Data)
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/飞行追踪")]
    public class BehaviourNode_FlyTrack : BehaviourGraphNode {
        public FlyTrackSettingData Config;
        public override string name => "飞行追踪";
        public override string BehaviourSign => nameof(Behaviour_Auto_FlyTrack);
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

    /// <summary>
    /// 开火行为节点 对应 WeaponSettingData 一条(持有者的军火库)
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/开火")]
    public class BehaviourNode_WeaponFire : BehaviourGraphNode {
        public WeaponSettingData Config;
        public override string name => "开火";
        public override string BehaviourSign => nameof(Behaviour_Event_WeaponFire);
    }

    /// <summary>
    /// 瞬间移动行为节点 对应 TeleportationSettingData 一条
    /// 参数便签见 BehaviourPayloadDoc.Get(nameof(Behaviour_Auto_Teleportation))，节点身上只读显示
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/瞬间移动")]
    public class BehaviourNode_Teleportation : BehaviourGraphNode {
        public TeleportationSettingData Config;
        public override string name => "瞬间移动";
        public override string BehaviourSign => nameof(Behaviour_Auto_Teleportation);
    }

    /// <summary>
    /// 击退行为节点 对应 KnockbackSettingData 一条
    /// 参数便签见 BehaviourPayloadDoc.Get(nameof(Behaviour_Auto_Knockback))，节点身上只读显示
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/击退")]
    public class BehaviourNode_Knockback : BehaviourGraphNode {
        public KnockbackSettingData Config;
        public override string name => "击退";
        public override string BehaviourSign => nameof(Behaviour_Auto_Knockback);
    }

}
