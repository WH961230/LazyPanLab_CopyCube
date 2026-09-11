using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 波次管理器数据 仅承载行为参数 不含运行时状态与业务依赖
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
            [Header("触发类型")] public TriggerType TriggerType;
        }
    }
}