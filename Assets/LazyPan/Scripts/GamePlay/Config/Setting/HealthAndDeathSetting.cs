using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>生命与死亡 — 管数值与死亡处理，不做伤害判定。写 Health/MaxHealth/Dead。</summary>
    [CreateAssetMenu(fileName = "HealthAndDeathSetting", menuName = "LazyPan/HealthAndDeathSetting")]
    public class HealthAndDeathSetting : Setting {
        public List<HealthAndDeathSettingData> Datas = new List<HealthAndDeathSettingData>();

        public bool TryGet(string sourceSign, out HealthAndDeathSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("HealthAndDeathSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public struct HealthAndDeathSettingData {
        [Header("发起实体的类型标识 SourceSign")]
        [Tooltip("发起实体的类型标识，必须与 ObjConfig.Sign 一致，如 Obj_Enemy_Enemy1")]
        public string SourceSign;

        [Header("最大生命")]
        [Tooltip("最大生命，最小按 1 处理")]
        public float MaxHealth;

        [Header("初始生命比例 0-1 为1时满血")]
        [Tooltip("初始生命比例 0..1，1 为满血。实际初始值 = MaxHealth × 比例")]
        public float InitHealthRate;

        [Header("死亡延迟销毁时长 0=立即执行")]
        [Tooltip("死亡后延迟几秒再执行 DeathAction，0 立即执行")]
        public float DeathDelay;

        [Header("死亡后处理")]
        [Tooltip("死亡后处理：None=仅触发事件，DestroyEntity=销毁实体，DisableEntity=失活预制体")]
        public DeathAction DeathAction;
    }

    public enum DeathAction {
        None = 0,
        DestroyEntity = 1,
        DisableEntity = 2,
    }
}
