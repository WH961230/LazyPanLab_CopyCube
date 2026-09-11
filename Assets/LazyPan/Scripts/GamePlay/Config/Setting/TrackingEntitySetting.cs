using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>追踪实体配置：以 SourceSign 为键。行为只消费自身配置。</summary>
    [CreateAssetMenu(fileName = "TrackingEntitySetting", menuName = "LazyPan/TrackingEntitySetting")]
    public class TrackingEntitySetting : Setting {
        public List<TrackingEntitySettingData> Datas = new List<TrackingEntitySettingData>();

        /// <summary>
        /// 按来源类型获取行为配置 条目不存在时输出错误 避免静默失败
        /// </summary>
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

    /// <summary>
    /// 追踪行为配置 以来源类型为键 行为不感知实体业务
    /// </summary>
    [Serializable]
    public struct TrackingEntitySettingData {
        [Tooltip("发起追踪的实体类型，与 ObjConfig.Sign 一致，如 Obj_Enemy_Enemy1")]
        [Header("发起追踪的实体类型")] public string SourceSign;
        [Tooltip("追踪速度，代理速度，如 3.5")]
        [Header("追踪速度")] public float TrackingSpeed;
        [Tooltip("配置级停止，勾上后行为不再追踪")]
        [Header("追踪是否停止")] public bool TrackingStop;
        [Tooltip("被追踪的实体类型，如 Player 或 Obj_Player_Player1")]
        [Header("被追踪的实体类型")] public string TargetType;
    }
}