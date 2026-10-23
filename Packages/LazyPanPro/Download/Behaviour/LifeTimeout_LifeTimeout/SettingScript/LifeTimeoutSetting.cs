using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 寿命配置占位 参数全在自己 Data 里 这里只做图节点与 Setting 的挂钩
    /// </summary>
    [CreateAssetMenu(fileName = "LifeTimeoutSetting", menuName = "LazyPan/LifeTimeoutSetting")]
    public class LifeTimeoutSetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("寿命节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【寿命】管一个东西活几秒，时间到自动喊死。\n" +
            "— 配置参数（LifeTimeoutSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>LifeTime</color>：活几秒，建议 1~10，0=一直活";
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

        [Header("存活秒数")]
        [Tooltip("0=一直活；Data 有 LifeTime 时以 Data（传话包）为准")]
        public float LifeTime = 3f;
    }
}
