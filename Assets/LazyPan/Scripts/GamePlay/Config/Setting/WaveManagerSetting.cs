using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>波数管理器 — 只报第几波、歇几秒、啥时候报下一波，不产怪不感知其他行为。写 WaveIndex/WaveState/WaveRestRemain。</summary>
    [CreateAssetMenu(fileName = "WaveManagerSetting", menuName = "LazyPan/WaveManagerSetting")]
    public class WaveManagerSetting : Setting {
        public List<WaveManagerSettingData> Datas = new List<WaveManagerSettingData>();

        public bool TryGet(string sourceSign, out WaveManagerSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("WaveManagerSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class WaveManagerSettingData {
        [EntitySign]
        [Header("发起波次的实体类型 SourceSign")]
        [Tooltip("发起波次的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Camera_SceneC_EnemyGenerate")]
        public string SourceSign;

        [Header("起始波数 默认1")]
        [Tooltip("第一波写入 WaveIndex 的数值，默认1。产怪档案按此匹配：第1波=1、第2波=2")]
        public int StartWaveIndex = 1;

        [Header("首波前延迟 0立即开始")]
        [Tooltip("首波前延迟秒数，0 立即开第一波")]
        public float InitialDelay;

        [Header("全部完成后是否循环")]
        [Tooltip("全部波走完后是否回到起始波重来")]
        public bool Loop;

        [Header("波次列表 按序执行")]
        [Tooltip("波次列表按序执行，一条=一波。WaveIndex 从 StartWaveIndex 起递增")]
        public List<WaveEntry> Waves = new List<WaveEntry>();
    }

    [Serializable]
    public class WaveEntry {
        [Header("本波结束后到下一波的等待 0立即下一波")]
        [Tooltip("本波结束后到下一波的等待秒数，0 立即下一波")]
        public float RestDuration = 3f;

        [Header("进入下一波的条件")]
        [Tooltip("Interval=计时到就下一波；WaitValue=等指定 Data 满足条件再计时")]
        public WaveAdvanceMode AdvanceMode = WaveAdvanceMode.Interval;

        [Header("等待的 Data 标签 空则不等待 比如 LivingCount")]
        [Tooltip("等待的 Data 标签名，如 LivingCount。为空则不等待，不感知怪只认数字。数据源由 WaitWatchEntitySign 指定")]
        public string WaitWatchSign;

        [EntitySign]
        [Header("等待的数据源实体 留空读自己")]
        [Tooltip("等待的数据源实体 Sign，留空=读自己实体的 Data。波次与产怪分属两个实体时，填产怪实体 Sign 读它的 LivingCount")]
        public string WaitWatchEntitySign;

        [Header("等待的目标值")]
        [Tooltip("等待的目标值，与 Compare 配合，如 LivingCount Equal 0 表示等怪清光")]
        public int WaitTargetValue;

        [Header("比较方式")]
        [Tooltip("与 WaitTargetValue 的比较方式")]
        public WatchCompare Compare = WatchCompare.Equal;
    }

    public enum WaveAdvanceMode {
        Interval = 0,   // 计时结束即下一波
        WaitValue = 1,  // 等待通用 Data 满足条件再计时
    }

    public enum WatchCompare {
        Equal = 0,
        GreaterEqual = 1,
        LessEqual = 2,
        Greater = 3,
        Less = 4,
    }

    public enum WaveManagerState {
        Idle = 0,
        InitialDelay = 1,
        Rest = 2,
        Waiting = 3,
        Completed = 4,
    }
}