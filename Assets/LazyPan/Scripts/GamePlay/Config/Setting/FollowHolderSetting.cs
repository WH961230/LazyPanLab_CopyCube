using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 跟随主人配置占位 参数全在自己 Data 里 这里只做图节点与 Setting 的挂钩
    /// </summary>
    [CreateAssetMenu(fileName = "FollowHolderSetting", menuName = "LazyPan/FollowHolderSetting")]
    public class FollowHolderSetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("跟随主人节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【跟随主人】管一个东西围着主人转，主人没了就原地睡觉。\n" +
            "跟谁不用你配，触发器会交过来。\n" +
            "— 配置参数（FollowHolderSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>OrbitRadius</color>：转圈半径，建议 1~4\n" +
            "- <color=#FFD54F>OrbitSpeed</color>：每秒转多少度，建议 90~360\n" +
            "- <color=#FFD54F>OrbitAngle</color>：出生时站在几点钟方向，0~360";
        public List<FollowHolderSettingData> Datas = new List<FollowHolderSettingData>();

        public bool TryGet(string sourceSign, out FollowHolderSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("FollowHolderSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class FollowHolderSettingData {
        [EntitySign]
        [Header("实体 SourceSign")]
        [Tooltip("实体 Sign，与 ObjConfig.Sign 一致")]
        public string SourceSign;

        [Header("环绕半径")]
        [Tooltip("Data 有有效值时以 Data 为准")]
        public float OrbitRadius = 2f;

        [Header("环绕速度（度/秒）")]
        [Tooltip("Data 有有效值时以 Data 为准")]
        public float OrbitSpeed = 180f;

        [Header("初始角度")]
        [Tooltip("Data 有 OrbitAngle 时以 Data 为准，否则用这个")]
        public float OrbitAngle;
    }
}
