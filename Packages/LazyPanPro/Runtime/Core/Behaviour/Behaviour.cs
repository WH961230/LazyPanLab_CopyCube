using UnityEngine;

namespace LazyPan {
    public abstract class Behaviour {
        public string BehaviourSign;
        public string BehaviourName => BehaviourConfig.Get(BehaviourSign)?.Name;
        public Data BehaviourData;
        public Entity entity;

        protected Behaviour(Entity entity, string behaviourSign) {
            this.entity = entity;
            BehaviourSign = behaviourSign;
            ConsoleEx.Instance.ContentSave("behaviour", $"ID:{entity.ID} 注册行为:{BehaviourConfig.Get(BehaviourSign).Name}");
        }

        public void SetBehaviourData(Data data) {
            BehaviourData = data;
        }

        protected T AttachBehaviourData<T>() where T : Data {
            if (entity == null || entity.Prefab == null) {
                return null;
            }

            if (entity.GetBehaviourData<T>(out T cached) && cached != null) {
                cached.EntityID = entity.ID;
                return cached;
            }

            T data = entity.Prefab.GetComponent<T>();
            if (data == null) {
                data = entity.Prefab.AddComponent<T>();
            }

            data.EntityID = entity.ID;
            entity.SetBehaviourDataCache(data);
            return data;
        }

        protected void DetachBehaviourData<T>() where T : Data {
            if (entity == null || entity.Prefab == null) {
                return;
            }

            entity.GetBehaviourData<T>(out T cached);
            entity.RemoveBehaviourDataCache<T>();
            if (cached != null) {
                Object.Destroy(cached);
                return;
            }

            T data = entity.Prefab.GetComponent<T>();
            if (data != null) {
                Object.Destroy(data);
            }
        }

        public abstract void DelayedExecute();

        public virtual void Clear() {
            ConsoleEx.Instance.ContentSave("behaviour", $"ID:{entity.ID} 注销行为:{BehaviourConfig.Get(BehaviourSign).Name}");
        }
    }
}
