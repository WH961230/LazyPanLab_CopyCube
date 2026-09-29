using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 开火数据 仅承载武器配置快照与冷却沙漏 不含业务逻辑
    /// 当前武器ID优先读持有者 Data 的 CurrentWeapon 字符串(武器管理器换枪就改它) 没有才用配置的默认枪
    /// </summary>
    public class WeaponFireData : Data {
        [Header("开火行为参数")] public WeaponFireConfig Config = new WeaponFireConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(WeaponFireConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class WeaponFireConfig {
            [Header("默认武器ID")] public string DefaultWeaponID;
            [Header("索敌类型")] public string TargetType = "Enemy";
            [Header("冷却剩余秒 调试用")] public float Cooldown;
        }
    }
}
