using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 阶段进度数据 仅承载行为参数 不含运行时状态与业务依赖
    /// </summary>
    public class StageProgressData : Data {
        [Header("阶段进度行为参数")] public StageProgressConfig Config = new StageProgressConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(StageProgressConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class StageProgressConfig {
            [Header("阶段钥匙标签 Int类型")] public string StageParamSign;
            [Header("阶段上限标签 为空=不同步")] public string MaxStageParamSign;
            [Header("进度数值标签 Float类型")] public string ProgressParamSign;
            [Header("进度上限标签 为空=不同步")] public string MaxProgressParamSign;
            [Header("开局阶段")] public int InitialStage = 1;
            [Header("满级阶段 掐顶")] public int MaxStage = 99999;
            [Header("兜底上限")] public float FallbackCap = 100f;
            [Header("阶段上限表")] public List<StageCapConfig> Caps = new List<StageCapConfig>();
            [Header("升阶事件 升1级触发1次")] public List<DeathData.ParamModifyConfig> StageUpEvents = new List<DeathData.ParamModifyConfig>();
        }

        [Serializable]
        public class StageCapConfig {
            [Header("阶段")] public int Stage = 1;
            [Header("该阶段上限")] public float Cap = 100f;
        }
    }
}
