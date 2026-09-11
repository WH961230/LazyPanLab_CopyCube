using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>波数管理器 — 只报第几波、歇几秒、啥时候报下一波，不产怪不感知其他行为。写 WaveIndex/WaveState/WaveRestRemain。</summary>
    [CreateAssetMenu(fileName = "EntityTriggerControllerSetting", menuName = "LazyPan/EntityTriggerControllerSetting")]
    public class EntityTriggerControllerSetting : Setting {
        public List<EntityTriggerControllerSettingData> Datas = new List<EntityTriggerControllerSettingData>();

        public bool TryGet(string sourceSign, out EntityTriggerControllerSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("EntityTriggerControllerSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class EntityTriggerControllerSettingData {
        [Header("实体类型 SourceSign")]
        [Tooltip("实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Generate_EnemyGenerate1")]
        public string SourceSign;

        [Header("组件触发器标识")]
        [Tooltip("")]
        public string CompTriggerSign;

        [Header("触发数据")]
        [Tooltip("")]
        public List<TriggerData> Waves = new List<TriggerData>();
    }

    [Serializable]
    public class TriggerData {
        [Header("触发的目标实体标识")]
        [Tooltip("")]
        public string TargetTriggerEntitySign;

        [Header("触发类型")]
        [Tooltip("")]
        public TriggerType TriggerType;

        [Header("行为")]
        [Tooltip("")]
        public string BehaviourSign;
        
        [Header("行为数据")]
        [Tooltip("")]
        public string BehaviourDataParamSign;
    }

    [Serializable]
    public enum TriggerType {
        EnterExit = 0,
        Stay = 1,
    }
}