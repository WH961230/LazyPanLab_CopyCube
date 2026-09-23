using System.Collections.Generic;

namespace LazyPan {
    /// <summary>
    /// 实体属性注册表：实体级大账本，只存一份。
    /// Health 唯一生产者是死亡行为（挂了死亡才有血），其余行为只消费，不许自建。
    /// </summary>
    public static class EntityAttrRegistry {
        private static readonly Dictionary<int, HealthAttr> healthMap = new Dictionary<int, HealthAttr>();
        private static readonly Dictionary<int, MoveAttr> moveMap = new Dictionary<int, MoveAttr>();

        public static bool RegisterHealth(Entity entity, HealthAttr attr) {
            if (entity == null || attr == null) return false;
            if (healthMap.ContainsKey(entity.ID)) {
                LogUtil.LogErrorFormat("实体:{0} Health重复注册，第二个生产者拒绝", entity.ObjConfig?.Sign);
                return false;
            }
            healthMap[entity.ID] = attr;
            return true;
        }

        public static bool TryGetHealth(Entity entity, out HealthAttr attr) {
            attr = null;
            if (entity == null) return false;
            return healthMap.TryGetValue(entity.ID, out attr);
        }

        public static MoveAttr RegisterOrGetMove(Entity entity) {
            if (entity == null) return null;
            if (!moveMap.TryGetValue(entity.ID, out MoveAttr attr)) {
                attr = new MoveAttr();
                moveMap[entity.ID] = attr;
            }
            return attr;
        }

        public static void Unregister(Entity entity) {
            if (entity == null) return;
            healthMap.Remove(entity.ID);
            moveMap.Remove(entity.ID);
        }
    }

    /// <summary>实体级移动属性：停止开关，多移动行为共享一份，不许各存各的。</summary>
    public class MoveAttr {
        public bool Stopped;
    }
    /// <summary>实体级健康值对象：当前血/最大血，读写必须走方法，不许直接改数字。</summary>
    public class HealthAttr {
        public float Current;
        public float Max;
        public bool HasBar => Max > 0f;
        public bool IsZero => HasBar && Current <= 0f;

        public void Damage(float amount) {
            if (!HasBar || amount <= 0f) return;
            Current -= amount;
        }

        public void Heal(float amount) {
            if (!HasBar || amount <= 0f) return;
            Current = UnityEngine.Mathf.Min(Current + amount, Max);
        }
    }
}
