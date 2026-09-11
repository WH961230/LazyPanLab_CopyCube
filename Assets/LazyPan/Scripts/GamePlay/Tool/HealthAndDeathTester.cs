using UnityEngine;
using UnityEngine.InputSystem;

namespace LazyPan {
    /// <summary>
    /// 测试驱动 - 生命与死亡
    /// 仅用于验证行为本身 不含业务 挂到场景任意物体即可
    /// 调用链与框架一致 按类型找实体 EntityRegister 再按实体找行为 BehaviourRegister
    /// 按键 1扣血 2回血 3处决 4复活 5无敌 0打印状态
    /// </summary>
    public class HealthAndDeathTester : MonoBehaviour {
        private const string TARGET_TYPE = "Enemy";
        private const float DAMAGE_AMOUNT = 10f;
        private const float HEAL_AMOUNT = 10f;

        private bool hasHooked;

        private void Update() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || Game.instance == null) {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame) {
                Damage();
            }

            if (keyboard.digit2Key.wasPressedThisFrame) {
                Heal();
            }

            if (keyboard.digit0Key.wasPressedThisFrame) {
                PrintStatus();
            }
        }

        /// <summary>
        /// 框架方式的定位入口 调用方只依赖实体类型与行为契约 不感知业务
        /// </summary>
        private bool TryGetTarget(out Entity targetEntity, out Behaviour_Event_HealthAndDeath healthBehaviour) {
            targetEntity = null;
            healthBehaviour = null;

            if (!EntityRegister.TryGetRandEntityByType(TARGET_TYPE, out targetEntity)) {
                LogUtil.LogErrorFormat("[测试] 未找到类型:{0} 的实体", TARGET_TYPE);
                return false;
            }

            if (!BehaviourRegister.GetBehaviour(targetEntity, out healthBehaviour)) {
                LogUtil.LogErrorFormat("[测试] 实体:{0} 未注册行为:生命与死亡", targetEntity.ObjConfig.Sign);
                return false;
            }

            return true;
        }

        private void Damage() {
            if (!TryGetTarget(out _, out var hp)) {
                return;
            }

            hp.TakeDamage(DAMAGE_AMOUNT);
        }

        private void Heal() {
            if (!TryGetTarget(out _, out var hp)) {
                return;
            }

            bool applied = hp.Heal(HEAL_AMOUNT);
            LogUtil.LogFormat("[测试] Heal({0}) => {1}", HEAL_AMOUNT, applied);
        }

        /// <summary>
        /// 状态打印走实体 Data 展示零行为依赖的读取方式
        /// </summary>
        private void PrintStatus() {
            if (!EntityRegister.TryGetRandEntityByType(TARGET_TYPE, out Entity targetEntity)) {
                return;
            }

            bool hasHealth = Cond.Instance.TryGetData(targetEntity, Behaviour_Event_HealthAndDeath.HEALTH_LABEL, out FloatData health);
            bool hasMaxHealth = Cond.Instance.TryGetData(targetEntity, Behaviour_Event_HealthAndDeath.MAXHEALTH_LABEL, out FloatData maxHealth);
            bool hasDead = Cond.Instance.TryGetData(targetEntity, Behaviour_Event_HealthAndDeath.DEAD_LABEL, out BoolData dead);

            if (hasHealth && hasMaxHealth && hasDead) {
                LogUtil.LogFormat("[测试] 实体:{0} Data读取 血量:{1}/{2} 死亡:{3}",
                    targetEntity.ObjConfig.Sign, health.Float, maxHealth.Float, dead.Bool);
            } else {
                LogUtil.LogErrorFormat("[测试] 实体:{0} 未初始化生命与死亡数据", targetEntity.ObjConfig.Sign);
            }
        }
    }
}
