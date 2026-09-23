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

        [Header("当前血量")] public float Health;
        [Header("最大血量 0=无血条(子弹/特效靠外部置Dead)")] public float MaxHealth;
        [Header("死亡标记")] public bool Dead;

        public bool HasHealthBar => MaxHealth > 0f;

        public void Damage(float amount) {
            if (!HasHealthBar || Dead) {
                return;
            }
            Health -= amount;
        }

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
        //调试入口统一走 DeathTester，Data 里不再监听按键，避免双入口打架。
#endif
    }
}
