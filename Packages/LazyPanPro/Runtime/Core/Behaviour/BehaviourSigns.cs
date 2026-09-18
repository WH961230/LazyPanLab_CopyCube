using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 Sign 显式配置约定：所有 Sign 一律不允许留空。
    /// Self=自己，Root=实体根，Any=任意实体。留空一律视为配置错误，直接报错并停用该行为。
    /// </summary>
    public static class BehaviourSigns {
        public const string Self = "Self";
        public const string Root = "Root";
        public const string Any = "Any";
        public const string None = "None";
        public const string Virtual = "Virtual";
        public const string Triggerer = "Triggerer";

        public static bool Require(string sign, string behaviourSign, string entitySign, string fieldName) {
            if (!string.IsNullOrEmpty(sign)) {
                return true;
            }

            LogUtil.LogErrorFormat("行为:{0} 实体:{1} 字段:{2} 的 Sign 不允许为空，请显式配置！", behaviourSign, entitySign, fieldName);
            return false;
        }

        /// <summary>
        /// 解析目标实体：Self=自己，其他填实体 Sign，留空或找不到一律报错返回 false。
        /// </summary>
        public static bool ResolveEntity(Entity self, string behaviourSign, string fieldName, string sign, out Entity target) {
            target = null;
            if (self == null) {
                return false;
            }

            if (string.IsNullOrEmpty(sign)) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 字段:{2} 的实体 Sign 不允许为空，写自己请填 Self！", behaviourSign, self.ObjConfig?.Sign, fieldName);
                return false;
            }

            if (sign == Self) {
                target = self;
                return true;
            }

            if (self.ObjConfig != null && (sign == self.ObjConfig.Sign || sign == self.Sign)) {
                target = self;
                return true;
            }

            if (!EntityRegister.TryGetEntityBySign(sign, out target)) {
                LogUtil.LogErrorFormat("行为:{0} 未找到目标实体:{1}", behaviourSign, sign);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 解析挂点：Root=实体根，其他填 Comp 标签，留空或找不到一律报错返回 null。
        /// </summary>
        public static Transform ResolveMountPoint(Entity self, string behaviourSign, string mountPointLabel) {
            if (self == null || self.Prefab == null) {
                return null;
            }

            if (string.IsNullOrEmpty(mountPointLabel)) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 挂点标签不允许为空，挂根节点请填 Root！", behaviourSign, self.ObjConfig?.Sign);
                return null;
            }

            if (mountPointLabel == Root) {
                return self.Prefab.transform;
            }

            Transform tran = Cond.Instance.Get<Transform>(self, mountPointLabel);
            if (tran == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到挂点标签:{1}", behaviourSign, mountPointLabel);
            }

            return tran;
        }
    }
}
