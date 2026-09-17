using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LazyPan {
    /// <summary>
    /// 死亡数据 仅承载行为参数 不含运行时状态与业务依赖
    /// </summary>
    public class DeathData : Data {
        [Header("死亡行为参数")] public DeathConfig Config = new DeathConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(DeathConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class DeathConfig {
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
                    if (BehaviourRegister.GetBehaviour(target, out Behaviour_Event_Death behaviour)) {
                        behaviour.Kill();
                    }
                }
            }
        }

#endif
    }
}
