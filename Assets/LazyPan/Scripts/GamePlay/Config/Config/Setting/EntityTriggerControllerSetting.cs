using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>实体触发控制器 — 源实体触发器命中目标实体时 按进入/停留/离开/范围外 四个相位给自己或其他实体增减参数 不读业务。典型: 玩家进塔范围 Energy 每秒涨 离开每秒降。</summary>
    [CreateAssetMenu(fileName = "EntityTriggerControllerSetting", menuName = "LazyPan/EntityTriggerControllerSetting")]
    public class EntityTriggerControllerSetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("触发器节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【触发器】管一块地盘，谁进来出去干什么全在这里定。\n" +
            "— 配置参数（EntityTriggerControllerSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>CompTriggerSign</color>：用地盘上哪个触发器，Root=实体根\n" +
            "- <color=#FFD54F>Rules</color>：触发规则，一条规则管一类人\n" +
            "— 每条规则怎么填 —\n" +
            "- <color=#FFD54F>TriggerEntitySign</color>：谁算数，Any=谁都算\n" +
            "- <color=#FFD54F>EnterActions</color>：进来瞬间触发一次\n" +
            "- <color=#FFD54F>StayActions</color>：待着不动每帧都触发\n" +
            "- <color=#FFD54F>ExitActions</color>：离开瞬间触发一次\n" +
            "- <color=#FFD54F>OutsideActions</color>：在范围外每帧触发\n" +
            "— 每个动作怎么填 —\n" +
            "- <color=#FFD54F>TargetEntitySign</color>：改谁，Self=自己，Triggerer=触发的人\n" +
            "- <color=#FFD54F>ParamSign</color>：改哪个数\n" +
            "- <color=#FFD54F>Modify</color>：Set=直接给，Add=累加\n" +
            "- <color=#FFD54F>Min</color>/<color=#FFD54F>Max</color>：改完夹在范围内，只对整数小数有效";
        public List<EntityTriggerControllerSettingData> Datas = new List<EntityTriggerControllerSettingData>();

        public bool TryGet(string sourceSign, out EntityTriggerControllerSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("EntityTriggerControllerSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class EntityTriggerControllerSettingData {
        [EntitySign]
        [Header("触发源实体类型 SourceSign")]
        [Tooltip("持有触发器碰撞体的实体，必须与 ObjConfig.Sign 一致，如 Obj_Tower_SceneB_Tower")]
        public string SourceSign;

        [Header("组件触发器标识 Root=实体根")]
        [Tooltip("实体上带触发碰撞体的 Comp 标签，如 Trigger。Root=用实体自身根 Comp。不允许为空")]
        public string CompTriggerSign;

        [Header("触发规则")]
        [Tooltip("一条规则=一种进入者对应一组相位参数操作，可配多条以响应不同实体")]
        public List<TriggerRule> Rules = new List<TriggerRule>();
    }

    [Serializable]
    public class TriggerRule {
        [EntitySign]
        [Header("触发者实体标识 Any=任意实体")]
        [Tooltip("允许触发此规则的实体 Sign，如 Obj_Player_SceneB_Player。Any=任意实体进入都算。不允许为空")]
        public string TriggerEntitySign;

        [Header("进入瞬间 触发一次")]
        [Tooltip("TriggerEnter 瞬间执行一次。如把 InRange 置真")]
        public List<TriggerAction> EnterActions = new List<TriggerAction>();

        [Header("停留期间 每帧执行")]
        [Tooltip("TriggerStay 每帧执行。AddPerSecond 即按 deltaTime 累加，如范围内 Energy 每秒+1")]
        public List<TriggerAction> StayActions = new List<TriggerAction>();

        [Header("离开瞬间 触发一次")]
        [Tooltip("TriggerExit 瞬间执行一次。如把 InRange 置假")]
        public List<TriggerAction> ExitActions = new List<TriggerAction>();

        [Header("范围外 每帧执行")]
        [Tooltip("无任何触发者在范围内时每帧执行(行为 Update 驱动)。AddPerSecond 填负值=离开后 Energy 每秒衰减")]
        public List<TriggerAction> OutsideActions = new List<TriggerAction>();
    }

    [Serializable]
    public class TriggerAction {
        [EntitySign(true)]
        [Header("被修改实体 必填 Self=自己 Triggerer=触发者")]
        [Tooltip("要增减参数的目标实体 Sign，Self=触发源实体自己(塔)。Triggerer=带起本次规则的那只实体(谁碰撞就改谁)。填对方 Sign 即改其他实体参数。不允许为空")]
        public string TargetEntitySign;

        [Header("参数标签 必填")]
        [Tooltip("目标实体上的 Data 标签名，如 Energy。不允许为空，不存在时自动创建")]
        public string ParamSign;

        [Header("参数类型")]
        [Tooltip("参数类型，决定读写哪一种 Data")]
        public ParamValueType ValueType;

        [Header("修改方式")]
        [Tooltip("Set=直接赋值 Add=累加增量 AddPerSecond=按 deltaTime 累加(仅停留/范围外每帧相位有意义)")]
        public TriggerModifyType Modify;

        [Header("布尔值")]
        [Tooltip("ValueType=Bool 时的值")]
        public bool BoolValue;

        [Header("整数值")]
        [Tooltip("ValueType=Int 时的值或增量")]
        public int IntValue;

        [Header("浮点值")]
        [Tooltip("ValueType=Float 时的值或增量，AddPerSecond 填每秒速率(正增负减)，如 1 或 -1")]
        public float FloatValue;

        [Header("字符串值")]
        [Tooltip("ValueType=String 时的值")]
        public string StringValue;

        [Header("向量值")]
        [Tooltip("ValueType=Vector3 时的值或增量")]
        public Vector3 Vector3Value;

        [Header("数值下限 仅Int/Float生效")]
        [Tooltip("Set/Add/AddPerSecond 后的最小钳制值，仅数值类型生效。如 Energy 不低于 0")]
        public float Min = float.MinValue;

        [Header("数值上限 仅Int/Float生效")]
        [Tooltip("Set/Add/AddPerSecond 后的最大钳制值，仅数值类型生效。如 Energy 不超过 100")]
        public float Max = float.MaxValue;
    }

    public enum TriggerModifyType {
        Set = 0,
        Add = 1,
        AddPerSecond = 2,
    }
}
