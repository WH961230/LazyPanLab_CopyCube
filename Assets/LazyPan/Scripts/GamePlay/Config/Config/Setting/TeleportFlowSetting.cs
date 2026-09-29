using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 传送流程 — 到前置条件满足才切场景 不感知触发源业务
    /// 前置条件 null=无条件走内部请求(RequestTeleport) 非空则左右参数比较通过才放行
    /// </summary>
    [CreateAssetMenu(fileName = "TeleportFlowSetting", menuName = "LazyPan/TeleportFlowSetting")]
    public class TeleportFlowSetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("传送流程节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【传送流程】管条件一到跳场景。\n" +
            "— 配置参数（TeleportFlowSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>TargetSceneSign</color>：跳去哪个场景\n" +
            "- <color=#FFD54F>Once</color>：true=只跳一次\n" +
            "- <color=#FFD54F>Condition</color>：前置条件，空=谁喊跳谁跳\n" +
            "— 条件怎么填 —\n" +
            "- <color=#FFD54F>LeftEntitySign</color>+<color=#FFD54F>LeftParamSign</color>：看谁的哪个数，Self=自己\n" +
            "- <color=#FFD54F>Compare</color>：比大小的方式\n" +
            "- <color=#FFD54F>RightIsEntityParam</color>：true=右边也是数，false=右边是常量\n" +
            "- 右边是常量时，对着类型填下面的值";
        public List<TeleportFlowSettingData> Datas = new List<TeleportFlowSettingData>();

        public bool TryGet(string sourceSign, out TeleportFlowSettingData data) {
            foreach (TeleportFlowSettingData tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("TeleportFlowSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class TeleportFlowSettingData {
        [EntitySign]
        [Header("发起传送的实体类型 SourceSign")]
        [Tooltip("发起传送的实体类型 必须与 ObjConfig.Sign 一致 如 Obj_Logo_SceneA_BeginLogo")]
        public string SourceSign;

        [Header("目标场景标识")]
        [Tooltip("要跳转的场景标识 传给 Flow.Next 如 SceneB")]
        public string TargetSceneSign;

        [Header("仅触发一次")]
        [Tooltip("勾上则传送一次后锁住 同一实体生命周期内不再重复传送")]
        public bool Once = true;

        [Header("前置条件 null=无条件走内部请求")]
        [Tooltip("前置条件 null=无条件走内部请求(RequestTeleport)，非空则左右参数比较通过才放行。不允许空字符串")]
        public TeleportCondition Condition = new TeleportCondition();
    }

    [Serializable]
    public class TeleportCondition {
        [EntitySign]
        [Header("左边实体 必填 Self=自己")]
        [Tooltip("左边数据源实体 Sign Self=读自己实体的 Data 如塔B填自己。不允许为空")]
        public string LeftEntitySign;

        [Header("左边参数标签 必填")]
        [Tooltip("左边 Data 标签名 如 Energy 不允许为空")]
        public string LeftParamSign;

        [Header("左边参数类型")]
        [Tooltip("左边参数类型 决定读取哪一种 Data")]
        public ParamValueType LeftValueType = ParamValueType.Float;

        [Header("比较方式")]
        [Tooltip("左右值的比较方式 非数值类型仅支持 相等/不等")]
        public TeleportCompare Compare = TeleportCompare.GreaterEqual;

        [Header("右边是实体参数")]
        [Tooltip("勾上=右边读实体参数 不勾=右边用下面的常量值")]
        public bool RightIsEntityParam;

        [EntitySign]
        [Header("右边实体 右边是参数时有效 必填")]
        [Tooltip("右边数据源实体 Sign 仅 RightIsEntityParam 勾上时有效 Self=读自己。不允许为空")]
        public string RightEntitySign;

        [Header("右边参数标签 右边是参数时有效 必填")]
        [Tooltip("右边 Data 标签名 如 MaxEnergy 仅 RightIsEntityParam 勾上时有效。不允许为空")]
        public string RightParamSign;

        [Header("右边参数类型 右边是参数时有效")]
        [Tooltip("右边参数类型 决定读取哪一种 Data 仅 RightIsEntityParam 勾上时有效")]
        public ParamValueType RightValueType = ParamValueType.Float;

        [Header("右边布尔常量")]
        [Tooltip("右边常量类型=布尔时有效")]
        public bool RightBoolValue;

        [Header("右边整数常量")]
        [Tooltip("右边常量类型=整数时有效")]
        public int RightIntValue;

        [Header("右边浮点常量")]
        [Tooltip("右边常量类型=浮点时有效")]
        public float RightConstValue;

        [Header("右边字符串常量")]
        [Tooltip("右边常量类型=字符串时有效")]
        public string RightStringValue;

        [Header("右边向量常量")]
        [Tooltip("右边常量类型=向量时有效")]
        public Vector3 RightVector3Value;

        [Header("右边常量类型 右边不用参数时有效")]
        [Tooltip("右边常量类型 决定用上面哪个常量值参与比较")]
        public ParamValueType RightConstType = ParamValueType.Float;
    }

    public enum TeleportCompare {
        Greater = 0,
        GreaterEqual = 1,
        Equal = 2,
        LessEqual = 3,
        Less = 4,
        NotEqual = 5,
    }
}
