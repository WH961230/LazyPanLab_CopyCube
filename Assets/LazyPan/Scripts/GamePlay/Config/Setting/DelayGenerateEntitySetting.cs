using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>延时生成 — 只管产怪，不认识波次。写 LivingCount。</summary>
    [CreateAssetMenu(fileName = "DelayGenerateSetting", menuName = "LazyPan/DelayGenerateSetting")]
    public class DelayGenerateEntitySetting : Setting {
        public List<DelayGenerateEntitySettingData> Datas = new List<DelayGenerateEntitySettingData>();

        public bool TryGet(string sourceSign, out DelayGenerateEntitySettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("DelayGenerateSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class DelayGenerateEntitySettingData {
        [Header("发起生成的实体类型")]
        [Tooltip("发起生成的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Generate_EnemyGenerate1")]
        public string SourceSign;

        [Header("无触发时的默认行为 Once/Loop")]
        [Tooltip("无触发档案满足时的默认行为：Once=只产一次，Loop=循环产")]
        public GenerateType GenerateType;

        [Header("无触发时的默认间隔 秒")]
        [Tooltip("无触发档案满足时的默认产间隔（秒）")]
        public float IntervalTime;

        [Header("无触发时的默认实体 Sign")]
        [Tooltip("无触发档案满足时要产的实体 Sign，如 Obj_Enemy_Enemy1")]
        public string GenerateEntitySign;

        [Header("触发档案 按序匹配首个满足条件的档案")]
        [Tooltip("触发档案按序匹配首个满足条件的档案，空则仅按默认节奏产。每帧从第0条查起，谁先满足就用谁")]
        public List<DelayGenerateProfile> Profiles = new List<DelayGenerateProfile>();
    }

    [Serializable]
    public class DelayGenerateProfile {
        [Header("监听的 Data 标签 为空则无条件命中 比如 WaveIndex")]
        [Tooltip("监听的 Data 标签名，如 WaveIndex。为空=无条件命中（装上就产）。数据源由 WatchEntitySign 指定")]
        public string WatchSign;

        [Header("监听的数据源实体 留空读自己")]
        [Tooltip("监听的数据源实体 Sign，留空=读自己实体的 Data。波次在别的实体上时填波次实体的 Sign")]
        public string WatchEntitySign;

        [Header("比较方式")]
        [Tooltip("与 WatchValue 的比较方式")]
        public WatchCompare Compare = WatchCompare.GreaterEqual;

        [Header("目标值")]
        [Tooltip("目标值，与 Compare 配合。监听 WaveIndex 时默认从 1 起：第1波=1、第2波=2")]
        public int WatchValue;

        [Header("要产的实体 Sign")]
        [Tooltip("要产的实体 Sign，如 Obj_Enemy_Enemy1")]
        public string GenerateEntitySign;

        [Header("本档案产几只 0则无限循环产")]
        [Tooltip("本档案产几只，0=无限循环产直到被新档案覆盖")]
        public int Count;

        [Header("产间隔 秒")]
        [Tooltip("档案内产间隔（秒）")]
        public float Interval = 1f;
    }

    public enum GenerateType {
        Once = 0,
        Loop = 1,
    }
}
