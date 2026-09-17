using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 实体参数赋值数据 仅承载行为参数 不含运行时状态与业务依赖
    /// </summary>
    public class ParamValueData : Data {
        [Header("实体参数赋值行为参数")] public List<ParamValueConfig> Configs = new List<ParamValueConfig>();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(List<ParamValueConfig>)) {
                t = (T) Convert.ChangeType(Configs, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class ParamValueConfig {
            [Header("目标实体 留空写自己")] public string TargetEntitySign;
            [Header("参数标签")] public string ParamSign;
            [Header("参数类型")] public ParamValueType ValueType;
            [Header("布尔值")] public bool BoolValue;
            [Header("整数值")] public int IntValue;
            [Header("浮点值")] public float FloatValue;
            [Header("字符串值")] public string StringValue;
            [Header("向量值")] public Vector3 Vector3Value;
            [Header("装配时立即赋值")] public bool ApplyOnInit = true;
        }
    }
}
