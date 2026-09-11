using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>装备挂载数据 仅承载行为参数 不含外部行为数据。</summary>
    public class EquipmentMountData : Data {
        [Header("装备挂载行为参数")] public EquipmentMountConfig Config = new EquipmentMountConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(EquipmentMountConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class EquipmentMountConfig {
            [Header("初始即挂载")] public bool InitialMount = true;
            [Header("装备槽位列表")] public List<EquipmentMountItem> Mounts = new List<EquipmentMountItem>();
        }
    }
}