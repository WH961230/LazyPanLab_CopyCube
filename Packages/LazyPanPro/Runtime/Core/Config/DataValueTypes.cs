using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 通用值类型五选一：Bool/Int/Float/String/Vector3。
    /// 配置里一个字段只存一种值时，用它标明读写哪一种 Data，不绑任何具体行为。
    /// </summary>
    public enum DataValueType {
        Bool = 0,
        Int = 1,
        Float = 2,
        String = 3,
        Vector3 = 4,
    }

    /// <summary>
    /// 通用改数项：一条=改一个实体的一个 Data，Set=直接赋值，Add=累加（只对 Int/Float/Vector3 有意义）。
    /// 死亡/升级/三选一等行为共用，谁也不像谁的附庸。
    /// </summary>
    [Serializable]
    public class ParamModifyItem {
        [EntitySign]
        [Header("被修改实体 必填 Self=自己")]
        [Tooltip("要改参数的目标实体 Sign，Self=自己。跨实体联动时填对方 Sign，如 Obj_Player_SceneC_Player。不允许为空")]
        public string TargetEntitySign = BehaviourSigns.Self;

        [Header("参数标签 必填")]
        [Tooltip("目标实体上的 Data 标签名，如 Score / Energy / MovementStop。不允许为空，不存在时自动创建")]
        public string ParamSign;

        [Header("参数类型")]
        [Tooltip("参数类型，决定读写哪一种 Data")]
        public DataValueType ValueType;

        [Header("修改方式")]
        [Tooltip("Set=直接赋值，Add=在原值上累加增量(只对 Int/Float/Vector3 有意义)")]
        public ParamModifyType Modify;

        [Header("布尔值")]
        [Tooltip("ValueType=Bool 时写入的值(Set)")]
        public bool BoolValue;

        [Header("整数值")]
        [Tooltip("ValueType=Int 时的值(Set)或增量(Add)")]
        public int IntValue;

        [Header("浮点值")]
        [Tooltip("ValueType=Float 时的值(Set)或增量(Add)")]
        public float FloatValue;

        [Header("字符串值")]
        [Tooltip("ValueType=String 时写入的值(只支持 Set)")]
        public string StringValue;

        [Header("向量值")]
        [Tooltip("ValueType=Vector3 时的值(Set)或增量(Add)")]
        public Vector3 Vector3Value;
    }

    public enum ParamModifyType {
        Set = 0,
        Add = 1,
    }

    /// <summary>
    /// 传话包认词单三件套：词名+类型+默认值三位一体，行为头上贴一张，开火节点照单补齐。
    /// 词的唯一出处永远是行为代码，配置只是投影，配的人只改数不打字。
    /// </summary>
    [Serializable]
    public class PayloadContractDef {
        public string Sign;
        public DataValueType ValueType;
        public bool BoolDefault;
        public int IntDefault;
        public float FloatDefault;
        public string StringDefault;
        public Vector3 Vector3Default;
    }
}
