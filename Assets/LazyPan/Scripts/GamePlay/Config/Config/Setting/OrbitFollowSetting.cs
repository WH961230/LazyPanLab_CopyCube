using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 环绕配置占位 参数全在球自己的 Data 里(开火时写进来) 这里只做图节点与 Setting 的挂钩
    /// </summary>
    [CreateAssetMenu(fileName = "OrbitFollowSetting", menuName = "LazyPan/OrbitFollowSetting")]
    public class OrbitFollowSetting : Setting {
        public List<OrbitFollowSettingData> Datas = new List<OrbitFollowSettingData>();

        public bool TryGet(string sourceSign, out OrbitFollowSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("OrbitFollowSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class OrbitFollowSettingData {
        [EntitySign]
        [Header("环绕球实体 SourceSign")]
        [Tooltip("环绕球实体，必须与 ObjConfig.Sign 一致")]
        public string SourceSign;
    }
}
