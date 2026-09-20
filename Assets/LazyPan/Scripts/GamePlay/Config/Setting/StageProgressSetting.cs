using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>阶段进度 — 通用阶段与上限关系，不管业务词。只认阶段钥匙(Int)与进度值(Float)及一张阶段对照上限表。</summary>
    [CreateAssetMenu(fileName = "StageProgressSetting", menuName = "LazyPan/StageProgressSetting")]
    public class StageProgressSetting : Setting {
        public List<StageProgressSettingData> Datas = new List<StageProgressSettingData>();

        public bool TryGet(string sourceSign, out StageProgressSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("StageProgressSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class StageProgressSettingData {
        [EntitySign]
        [Header("使用该行为的实体类型 SourceSign")]
        [Tooltip("使用该行为的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Player_SceneC_Player")]
        public string SourceSign;

        [Header("阶段钥匙标签 必填 Int类型")]
        [Tooltip("阶段钥匙的 Data 标签名，如 Level。必须是 IntData。不允许为空")]
        public string StageParamSign;

        [Header("阶段上限标签 选填 Int类型 为空=不同步")]
        [Tooltip("阶段上限的 Data 标签名，如 MaxLevel。必须是 IntData。每一帧同步为满级阶段，供界面与其他行为读取用。为留给老存档可置空，为置可空则不同步")]
        public string MaxStageParamSign;

        [Header("进度数值标签 必填 Float类型")]
        [Tooltip("进度数值的 Data 标签名，如 Exp。必须是 FloatData。不允许为空")]
        public string ProgressParamSign;

        [Header("进度上限标签 选填 Float类型 为空=不同步")]
        [Tooltip("进度上限的 Data 标签名，如 MaxExp。必须是 FloatData。为每一帧同步为当前阶段的上限，供血条/滑条显示用。为解决为什么升级了条还按100算的问题。为留给老存档可置空，为置可空则不同步")]
        public string MaxProgressParamSign;

        [Header("开局阶段")]
        [Tooltip("开局阶段钥匙，默认 1。如 Level=1 Exp=0 初始化")]
        public int InitialStage = 1;

        [Header("满级阶段 掐顶")]
        [Tooltip("满级阶段，到顶后进度卡在上限不动。假无限直接填 99999")]
        public int MaxStage = 99999;

        [Header("兜底上限 表里没写的阶段统一按这个算")]
        [Tooltip("阶段表里没写到的阶段统一按这个上限算，如只配 1~10 级手工曲线，11 级以后全按这个值。不配默认 100")]
        public float FallbackCap = 100f;

        [Header("阶段上限表")]
        [Tooltip("阶段对照上限表，一行=一个阶段装满是多少。如 1->100，2->150。表里没写的阶段用兜底上限")]
        public List<StageCapItem> Caps = new List<StageCapItem>();
    }

    [Serializable]
    public class StageCapItem {
        [Header("阶段")]
        [Tooltip("阶段钥匙，如 1 级")]
        public int Stage = 1;

        [Header("该阶段上限")]
        [Tooltip("该阶段进度装满是多少，如 100。必须大于 0")]
        public float Cap = 100f;
    }
}
