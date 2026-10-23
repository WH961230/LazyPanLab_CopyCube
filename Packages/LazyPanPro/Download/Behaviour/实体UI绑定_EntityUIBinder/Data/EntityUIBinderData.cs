using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 实体UI绑定数据 仅承载行为参数 不含运行时状态与业务依赖
    /// </summary>
    public class EntityUIBinderData : Data {
        [Header("实体UI绑定参数")] public EntityUIBinderConfig Config = new EntityUIBinderConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(EntityUIBinderConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class EntityUIBinderConfig {
            [Header("绑定UI列表")] public List<EntityUIBindItem> Items = new List<EntityUIBindItem>();
        }
    }
}
