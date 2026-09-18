using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>实体参数赋值 — 给自己或配置的目标实体写 Data 属性，不读业务不做判定。一个实体一条配置，内挂多条参数项。</summary>
    [CreateAssetMenu(fileName = "ParamValueSetting", menuName = "LazyPan/ParamValueSetting")]
    public class ParamValueSetting : Setting {
        public List<ParamValueSettingData> Datas = new List<ParamValueSettingData>();

        public bool TryGet(string sourceSign, out ParamValueSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("ParamValueSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class ParamValueSettingData {
        [EntitySign]
        [Header("发起赋值的实体类型 SourceSign")]
        [Tooltip("发起赋值的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Enemy_SceneC_Enemy")]
        public string SourceSign;

        [Header("参数项列表")]
        [Tooltip("参数项列表，一个实体可配多个参数，如 Energy + MaxEnergy")]
        public List<ParamValueItem> Items = new List<ParamValueItem>();
    }

    [Serializable]
    public class ParamValueItem {
        [EntitySign]
        [Header("目标实体 必填 Self=自己")]
        [Tooltip("要写值的目标实体 Sign，Self=自己实体的 Data。跨实体联动时填对方 Sign，行为不感知对方类型。不允许为空")]
        public string TargetEntitySign;

        [Header("参数标签 必填")]
        [Tooltip("要写的 Data 标签名，如 MovementStop。不允许为空，目标上不存在时自动创建")]
        public string ParamSign;

        [Header("参数类型")]
        [Tooltip("参数类型，决定写入哪一种 Data 并读取对应的值字段")]
        public ParamValueType ValueType;

        [Header("布尔值")]
        [Tooltip("ValueType=Bool 时写入的值")]
        public bool BoolValue;

        [Header("整数值")]
        [Tooltip("ValueType=Int 时写入的值")]
        public int IntValue;

        [Header("浮点值")]
        [Tooltip("ValueType=Float 时写入的值")]
        public float FloatValue;

        [Header("字符串值")]
        [Tooltip("ValueType=String 时写入的值")]
        public string StringValue;

        [Header("向量值")]
        [Tooltip("ValueType=Vector3 时写入的值")]
        public Vector3 Vector3Value;

        [Header("装配时立即赋值")]
        [Tooltip("勾上则行为装配完成立即写一次；不勾则只等外部调用 Apply()（如触发器联动时按需赋值）")]
        public bool ApplyOnInit = true;
    }

    public enum ParamValueType {
        Bool = 0,
        Int = 1,
        Float = 2,
        String = 3,
        Vector3 = 4,
    }
}
