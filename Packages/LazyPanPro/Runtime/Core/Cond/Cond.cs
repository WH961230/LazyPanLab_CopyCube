using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace LazyPan {
    public class Cond : Singleton<Cond> {
        #region 全局通用实体

        public Entity GetCameraEntity() {
            if (EntityRegister.TryGetRandEntityByType("Camera", out Entity entity)) {
                return entity;
            } else {
                return null;
            }
        }

        public Entity GetPlayerEntity() {
            if (EntityRegister.TryGetRandEntityByType("Player", out Entity entity)) {
                return entity;
            } else {
                return null;
            }
        }

        public Entity GetGlobalEntity() {
            if (EntityRegister.TryGetRandEntityByType("Global", out Entity entity)) {
                return entity;
            } else {
                return null;
            }
        }

        public bool GetEntityByID(int id, out Entity entity) {
            return EntityRegister.TryGetEntityByID(id, out entity);
        }

        #endregion

        #region 查标签组件
        public T Get<T>(Entity entity, string label) where T : Object {
            if (entity == null) { return default; }
#if UNITY_EDITOR
            if (entity.Comp == null) {
                LogUtil.LogErrorFormat("请检查 entity:{0} 没有挂 Comp 组件!", entity.ObjConfig.Sign);
                EditorApplication.isPaused = true;
            }
#endif
            return entity.Comp.Get<T>(label);
        }
        public T Get<T>(Comp comp, string label) where T : Object {
            if (comp == null) { return default; }
            return comp.Get<T>(label);
        }

        #endregion

        #region 查标签数据

        public bool GetData<T>(Entity entity, string label, out T t) {
            if (entity == null) {
                t = default;
                return false;
            }
#if UNITY_EDITOR
            if (entity.Data == null) {
                LogUtil.LogErrorFormat("请检查 entity:{0} 没有挂 Data 组件!", entity.ObjConfig.Sign);
                EditorApplication.isPaused = true;
            }
#endif
            return entity.Data.Get(label, out t);
        }

        public bool GetData<T1, T2>(Entity entity, string label, out T2 t) where T1 : Data {
            if (entity == null) {
                t = default;
                return false;
            }
#if UNITY_EDITOR
            if (entity.Data == null) {
                LogUtil.LogErrorFormat("请检查 entity:{0} 没有挂 Data 组件!", entity.ObjConfig.Sign);
                EditorApplication.isPaused = true;
            }
#endif
            T1 data = entity.Data as T1;
            return data.Get(label, out t);
        }

        /// <summary>
        /// 尝试获取数据 若不存在则自动添加并返回默认值
        /// </summary>
        public bool TryGetData<T>(Entity entity, string label, out T t) {
            t = default;
            if (entity == null) {
                return false;
            }
#if UNITY_EDITOR
            if (entity.Data == null) {
                LogUtil.LogErrorFormat("请检查 entity:{0} 没有挂 Data 组件!", entity.ObjConfig.Sign);
                EditorApplication.isPaused = true;
                return false;
            }
#endif
            if (entity.Data.Get(label, out t)) {
                return true;
            }

            if (!entity.Data.Add<T>(label, label)) {
                return false;
            }

            return entity.Data.Get(label, out t);
        }

        /// <summary>
        /// 尝试从子类 Data 中获取数据 若不存在则自动添加并返回默认值
        /// </summary>
        public bool TryGetData<T1, T2>(Entity entity, string label, out T2 t) where T1 : Data {
            t = default;
            if (entity == null) {
                return false;
            }
#if UNITY_EDITOR
            if (entity.Data == null) {
                LogUtil.LogErrorFormat("请检查 entity:{0} 没有挂 Data 组件!", entity.ObjConfig.Sign);
                EditorApplication.isPaused = true;
                return false;
            }
#endif
            T1 data = entity.Data as T1;
            if (data.Get(label, out t)) {
                return true;
            }

            if (!data.Add<T2>(label, label)) {
                return false;
            }

            return data.Get(label, out t);
        }

        #endregion

        #region 查标签事件

        public UnityEvent GetEvent(Entity entity, string label) {
            return entity.Comp.GetEvent(label);
        }
        public UnityEvent GetEvent(Comp comp, string label) {
            return comp.GetEvent(label);
        }

        #endregion
    }
}