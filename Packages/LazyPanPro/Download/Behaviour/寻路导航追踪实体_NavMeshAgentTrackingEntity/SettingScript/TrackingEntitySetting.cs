using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>追踪 — 驱动 NavMeshAgent 追目标。读 MovementStop 可暂停。</summary>
    [CreateAssetMenu(fileName = "TrackingEntitySetting", menuName = "LazyPan/TrackingEntitySetting")]
    public class TrackingEntitySetting : Setting {
        public List<TrackingEntitySettingData> Datas = new List<TrackingEntitySettingData>();

        public bool TryGet(string sourceSign, out TrackingEntitySettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("TrackingEntitySetting 缺少 SourceType:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public struct TrackingEntitySettingData {
        [Header("发起追踪的实体类型")]
        [Tooltip("发起追踪的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Enemy_Enemy1")]
        public string SourceSign;

        [Header("追踪速度")]
        [Tooltip("追踪速度，直接写到 NavMeshAgent.speed")]
        public float TrackingSpeed;

        [Header("追踪是否停止")]
        [Tooltip("为 true 则原地停并清空路径；同实体 Data 上 MovementStop(Bool)为 true 也会暂停")]
        public bool TrackingStop;

        [Header("被追踪的实体类型")]
        [Tooltip("被追踪的实体类型(ObjConfig.Type 列)，如 Player，运行时随机取该类型的一个实体")]
        public string TargetType;
    }
}
