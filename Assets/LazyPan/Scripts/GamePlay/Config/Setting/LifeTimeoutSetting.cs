using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 寿命配置占位 参数全在自己 Data 里 这里只做图节点与 Setting 的挂钩
    /// </summary>
    [CreateAssetMenu(fileName = "LifeTimeoutSetting", menuName = "LazyPan/LifeTimeoutSetting")]
    public class LifeTimeoutSetting : Setting {
        public List<LifeTimeoutSettingData> Datas = new List<LifeTimeoutSettingData>();

        public bool TryGet(string sourceSign, out LifeTimeoutSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("LifeTimeoutSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class LifeTimeoutSettingData {
        [EntitySign]
        [Header("实体 SourceSign")]
        [Tooltip("实体 Sign，与 ObjConfig.Sign 一致")]
        public string SourceSign;
    }
}
