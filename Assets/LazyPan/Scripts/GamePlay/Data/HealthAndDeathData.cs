using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
            [Header("死亡延迟销毁时长")] public float DeathDelay;
            [Header("死亡后处理")] public DeathAction DeathAction;
        }
        
#if UNITY_EDITOR
        
        private void Update() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || Game.instance == null) {
                return;
            }
            
            if (keyboard.digit1Key.wasPressedThisFrame) {
                if (EntityRegister.TryGetEntityByID(EntityID, out Entity target)) {
                    if (BehaviourRegister.GetBehaviour(target, out Behaviour_Event_HealthAndDeath behaviour)) {
                        behaviour.TakeDamage(10);
                    }
                }
            }
            
            if (keyboard.digit2Key.wasPressedThisFrame) {
                if (EntityRegister.TryGetEntityByID(EntityID, out Entity target)) {
                    if (BehaviourRegister.GetBehaviour(target, out Behaviour_Event_HealthAndDeath behaviour)) {
                        behaviour.Heal(10);
                    }
                }
            }
            
            if (keyboard.digit3Key.wasPressedThisFrame) {
                if (EntityRegister.TryGetEntityByID(EntityID, out Entity target)) {
                    if (BehaviourRegister.GetBehaviour(target, out Behaviour_Event_HealthAndDeath behaviour)) {
                        behaviour.AddMaxHealthOnly(10);
                    }
                }
            }
        }

#endif
    }
}
