using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 传送流程数据 仅承载行为参数 不含运行时状态与业务依赖
    /// </summary>
    public class TeleportFlowData : Data {
        [Header("传送流程行为参数")] public TeleportFlowConfig Config = new TeleportFlowConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(TeleportFlowConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class TeleportFlowConfig {
            [Header("目标场景标识")] public string TargetSceneSign;
            [Header("仅触发一次")] public bool Once = true;
            [Header("无条件走内部请求 Condition为空时有效")] public bool UseRequest;
            [Header("前置条件 留空=无条件")] public TeleportCondition Condition = new TeleportCondition();
        }
    }
}
