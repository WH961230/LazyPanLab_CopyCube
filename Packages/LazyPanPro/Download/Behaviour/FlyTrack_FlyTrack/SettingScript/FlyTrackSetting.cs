using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 飞行追踪配置占位 参数全在自己 Data 里 这里只做图节点与 Setting 的挂钩
    /// </summary>
    [CreateAssetMenu(fileName = "FlyTrackSetting", menuName = "LazyPan/FlyTrackSetting")]
    public class FlyTrackSetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("飞行追踪节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【飞行追踪】管一个东西往前飞，有目标就追着飞，没目标就直着飞。\n" +
            "追谁不用你配，触发器会交过来。\n" +
            "— 配置参数（FlyTrackSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>Speed</color>：飞多快，建议 5~15，0=睡觉不动";
        public List<FlyTrackSettingData> Datas = new List<FlyTrackSettingData>();

        public bool TryGet(string sourceSign, out FlyTrackSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("FlyTrackSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class FlyTrackSettingData {
        [EntitySign]
        [Header("实体 SourceSign")]
        [Tooltip("实体 Sign，与 ObjConfig.Sign 一致")]
        public string SourceSign;

        [Header("飞行速度")]
        [Tooltip("<=0 睡觉；Data 有 Speed 时以 Data（传话包）为准")]
        public float Speed = 10f;
    }
}
