using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 圆环扩散配置占位 扩散参数全在圆环自己的 Data 里(触发器传话包写进来) 这里只做图节点与 Setting 的挂钩
    /// </summary>
    [CreateAssetMenu(fileName = "RingExpandSetting", menuName = "LazyPan/RingExpandSetting")]
    public class RingExpandSetting : Setting {
        public List<RingExpandSettingData> Datas = new List<RingExpandSettingData>();

        public bool TryGet(string sourceSign, out RingExpandSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("RingExpandSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class RingExpandSettingData {
        [EntitySign]
        [Header("圆环实体 SourceSign")]
        [Tooltip("圆环实体，必须与 ObjConfig.Sign 一致，如 Obj_Ring_SceneC_Ring")]
        public string SourceSign;
    }
}
