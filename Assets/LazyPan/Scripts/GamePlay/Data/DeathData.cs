using System;
using System.Collections.Generic;
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
            [Header("死亡瞬间要改的参数列表")] public List<ParamModifyConfig> OnDeathParams = new List<ParamModifyConfig>();
        }

        [Serializable]
        public class ParamModifyConfig {
            [Header("被修改实体 Self=自己")] public string TargetEntitySign = BehaviourSigns.Self;
            [Header("参数标签")] public string ParamSign;
            [Header("参数类型")] public ParamValueType ValueType;
            [Header("修改方式 Set直接赋值 Add累加")] public ParamModifyType Modify;
            [Header("布尔值")] public bool BoolValue;
            [Header("整数值")] public int IntValue;
            [Header("浮点值")] public float FloatValue;
            [Header("字符串值")] public string StringValue;
            [Header("向量值")] public Vector3 Vector3Value;
        }

#if UNITY_EDITOR

        private void Update() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || Game.instance == null) {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame) {
                if (EntityRegister.TryGetEntityByID(EntityID, out Entity target)) {
                    if (Cond.Instance.TryGetData(target, DataLabels.Health, out FloatData health)) {
                        health.Float = 0f;
                    }
                }
            }
        }

#endif
    }
}
