using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 追踪实体数据 仅承载行为参数 不含运行时状态与业务依赖
    /// </summary>
    public class TrackingEntityData : Data {
        [Header("追踪行为参数")] public TrackingConfig Config = new TrackingConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(TrackingConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class TrackingConfig {
            [Header("追踪是否停止")] public bool TrackingStop;
            [Header("追踪速度")] public float TrackingSpeed;
            [Header("被追踪实体类型")] public string TargetEntityType;
        }
    }
}
