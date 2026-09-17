using UnityEngine;
using UnityEngine.InputSystem;

namespace LazyPan {
    /// <summary>
    /// 测试驱动 - 死亡
    /// 仅用于验证行为本身 不含业务 挂到场景任意物体即可
    /// 调用链与框架一致 按类型找实体 EntityRegister 再按实体找行为 BehaviourRegister
    /// 按键 1处决 2复活 0打印状态
    /// </summary>
    public class DeathTester : MonoBehaviour {
        private const string TARGET_TYPE = "Enemy";

        private void Update() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || Game.instance == null) {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame) {
                Kill();
            }

            if (keyboard.digit2Key.wasPressedThisFrame) {
                Revive();
            }

            if (keyboard.digit0Key.wasPressedThisFrame) {
                PrintStatus();
            }
        }

        /// <summary>
        /// 框架方式的定位入口 调用方只依赖实体类型与行为契约 不感知业务
        /// </summary>
        private bool TryGetTarget(out Entity targetEntity, out Behaviour_Event_Death deathBehaviour) {
            targetEntity = null;
            deathBehaviour = null;

            if (!EntityRegister.TryGetRandEntityByType(TARGET_TYPE, out targetEntity)) {
                LogUtil.LogErrorFormat("[测试] 未找到类型:{0} 的实体", TARGET_TYPE);
                return false;
            }

            if (!BehaviourRegister.GetBehaviour(targetEntity, out deathBehaviour)) {
                LogUtil.LogErrorFormat("[测试] 实体:{0} 未注册行为:死亡", targetEntity.ObjConfig.Sign);
                return false;
            }

            return true;
        }

        private void Kill() {
            if (!TryGetTarget(out _, out var death)) {
                return;
            }

            death.Kill();
        }

        private void Revive() {
            if (!TryGetTarget(out _, out var death)) {
                return;
            }

            death.Revive();
        }

        /// <summary>
        /// 状态打印走实体 Data 展示零行为依赖的读取方式
        /// </summary>
        private void PrintStatus() {
            if (!EntityRegister.TryGetRandEntityByType(TARGET_TYPE, out Entity targetEntity)) {
                return;
            }

            bool hasHealth = Cond.Instance.TryGetData(targetEntity, Behaviour_Event_Death.HEALTH_LABEL, out FloatData health);
            bool hasMaxHealth = Cond.Instance.TryGetData(targetEntity, Behaviour_Event_Death.MAXHEALTH_LABEL, out FloatData maxHealth);
            bool hasDead = Cond.Instance.TryGetData(targetEntity, Behaviour_Event_Death.DEAD_LABEL, out BoolData dead);

            if (hasHealth && hasMaxHealth && hasDead) {
                LogUtil.LogFormat("[测试] 实体:{0} Data读取 血量:{1}/{2} 死亡:{3}",
                    targetEntity.ObjConfig.Sign, health.Float, maxHealth.Float, dead.Bool);
            } else {
                LogUtil.LogErrorFormat("[测试] 实体:{0} 未初始化生命参数 请检查 ParamValueSetting", targetEntity.ObjConfig.Sign);
            }
        }
    }
}
