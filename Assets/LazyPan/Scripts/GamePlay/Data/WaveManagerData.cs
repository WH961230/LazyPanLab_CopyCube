using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 波次管理器数据 仅承载行为参数 不含运行时状态与业务依赖
    /// </summary>
    public class WaveManagerData : Data {
        [Header("波次管理器参数")] public WaveManagerConfig Config = new WaveManagerConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(WaveManagerConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class WaveManagerConfig {
            [Header("起始波数 默认1")] public int StartWaveIndex = 1;
            [Header("首波前延迟")] public float InitialDelay;
            [Header("是否循环")] public bool Loop;
            [Header("波次列表")] public List<WaveEntry> Waves = new List<WaveEntry>();
        }
    }
}