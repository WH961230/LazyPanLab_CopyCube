using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>延时生成 — 只管产怪，不认识波次。写 LivingCount。</summary>
    [CreateAssetMenu(fileName = "DelayGenerateSetting", menuName = "LazyPan/DelayGenerateSetting")]
    public class DelayGenerateEntitySetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("延时生成节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【延时生成】管一个产怪点按节奏产怪，产谁产几个全在这里定。\n" +
            "— 配置参数（DelayGenerateEntitySetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>GenerateType</color>：没触发时 Once=只产一次，Loop=循环产\n" +
            "- <color=#FFD54F>IntervalTime</color>：没触发时隔几秒产一个\n" +
            "- <color=#FFD54F>GenerateEntitySign</color>：没触发时产谁\n" +
            "- <color=#FFD54F>Profiles</color>：触发档案，按顺序谁先满足用谁；空着就只按默认节奏产\n" +
            "— 档案里每条怎么填 —\n" +
            "- <color=#FFD54F>WatchSign</color>+<color=#FFD54F>WatchEntitySign</color>：盯着谁的哪个数看，如波次实体的 WaveIndex\n" +
            "- <color=#FFD54F>Compare</color>+<color=#FFD54F>WatchValue</color>：数到几开产，如 >=1\n" +
            "- <color=#FFD54F>Count</color>：这条产几只，0=一直产到被下一条顶掉";
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
        [EntitySign]
        [Header("发起生成的实体类型")]
        [Tooltip("发起生成的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Camera_SceneC_EnemyGenerate")]
        public string SourceSign;

        [Header("无触发时的默认行为 Once/Loop")]
        [Tooltip("无触发档案满足时的默认行为：Once=只产一次，Loop=循环产")]
        public GenerateType GenerateType;

        [Header("无触发时的默认间隔 秒")]
        [Tooltip("无触发档案满足时的默认产间隔（秒）")]
        public float IntervalTime;

        [EntitySign]
        [Header("无触发时的默认实体 Sign")]
        [Tooltip("无触发档案满足时要产的实体 Sign，如 Obj_Enemy_SceneC_Enemy")]
        public string GenerateEntitySign;

        [Header("触发档案 按序匹配首个满足条件的档案")]
        [Tooltip("触发档案按序匹配首个满足条件的档案，空则仅按默认节奏产。每帧从第0条查起，谁先满足就用谁")]
        public List<DelayGenerateProfile> Profiles = new List<DelayGenerateProfile>();
    }

    [Serializable]
    public class DelayGenerateProfile {
        [Header("监听的 Data 标签 必填 比如 WaveIndex")]
        [Tooltip("监听的 Data 标签名，如 WaveIndex。不允许为空。数据源由 WatchEntitySign 指定")]
        public string WatchSign;

        [EntitySign]
        [Header("监听的数据源实体 必填 Self=自己")]
        [Tooltip("监听的数据源实体 Sign，Self=读自己实体的 Data。波次在别的实体上时填波次实体的 Sign。不允许为空")]
        public string WatchEntitySign;

        [Header("比较方式")]
        [Tooltip("与 WatchValue 的比较方式")]
        public WatchCompare Compare = WatchCompare.GreaterEqual;

        [Header("目标值")]
        [Tooltip("目标值，与 Compare 配合。监听 WaveIndex 时默认从 1 起：第1波=1、第2波=2")]
        public int WatchValue;

        [EntitySign]
        [Header("要产的实体 Sign 必填")]
        [Tooltip("要产的实体 Sign，如 Obj_Enemy_SceneC_Enemy。不允许为空")]
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
