using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 生命与死亡数据 仅承载行为参数 不含运行时状态与业务依赖
    /// </summary>
    public class HealthAndDeathData : Data {
        [Header("生命与死亡行为参数")] public HealthAndDeathConfig Config = new HealthAndDeathConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(HealthAndDeathConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class HealthAndDeathConfig {
            [Header("最大生命")] public float MaxHealth;
            [Header("初始生命")] public float InitHealth;
            [Header("受击后无敌时长")] public float InvincibleDurationOnHit;
            [Header("死亡延迟销毁时长")] public float DeathDelay;
            [Header("是否可被复活")] public bool CanRevive;
            [Header("死亡后处理")] public DeathAction DeathAction;
            [Header("死亡时是否清空无敌")] public bool ClearInvincibleOnDeath;
        }
    }
}
