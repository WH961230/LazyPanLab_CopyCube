using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 死亡数据 仅承载行为参数 不含运行时状态与业务依赖
    /// </summary>
    public class DeathData : Data {
        [Header("死亡行为参数")] public DeathConfig Config = new DeathConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(DeathConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class DeathConfig {
            [Header("死亡延迟销毁时长")] public float DeathDelay;
            [Header("死亡后处理")] public DeathAction DeathAction;
        }

        /// <summary>
        /// 参数改写项运行时形态 与 ParamModifyItem 同字段 升阶事件与死亡结算共用一个语义
        /// </summary>
        [Serializable]
        public class ParamModifyConfig {
            [Header("目标实体")] public string TargetEntitySign = BehaviourSigns.Self;
            [Header("改哪个数")] public string ParamSign;
            [Header("值的类型")] public DataValueType ValueType;
            [Header("改法")] public ParamModifyType Modify;
            [Header("布尔值")] public bool BoolValue;
            [Header("整数值")] public int IntValue;
            [Header("小数值")] public float FloatValue;
            [Header("字符串值")] public string StringValue;
            [Header("三维向量值")] public Vector3 Vector3Value;
        }
    }
}
