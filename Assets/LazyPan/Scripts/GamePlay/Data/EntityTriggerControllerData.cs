using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 实体触发控制器数据 仅承载运行时状态 不含业务依赖
    /// </summary>
    public class EntityTriggerControllerData : Data {
        [Header("实体触发控制器参数")] public EntityTriggerControllerConfig Config = new EntityTriggerControllerConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(EntityTriggerControllerConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class EntityTriggerControllerConfig {
            [Header("是否触发中")] public bool IsInTrigger = false;
            [Header("当前在范围内的触发者数量")] public int InsideCount = 0;
        }
    }
}
