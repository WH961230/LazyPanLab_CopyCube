using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 环绕养球数据 只存养球运行时 不含逻辑
    /// </summary>
    public class OrbitKeeperData : Data {
        [Header("养球参数")] public OrbitKeeperConfig Config = new OrbitKeeperConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(OrbitKeeperConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class OrbitKeeperConfig {
            [Header("默认武器ID")] public string DefaultWeaponID;
        }
    }
}
