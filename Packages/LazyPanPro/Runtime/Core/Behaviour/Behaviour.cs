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

            T data = entity.Prefab.GetComponent<T>();
            if (data == null) {
                data = entity.Prefab.AddComponent<T>();
            }

            data.EntityID = entity.ID;
            return data;
        }

        protected void DetachBehaviourData<T>() where T : Data {
            if (entity == null || entity.Prefab == null) {
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
