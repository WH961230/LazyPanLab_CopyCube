using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 体型扩散配置占位 参数全在自己 Data 里 这里只做图节点与 Setting 的挂钩
    /// </summary>
    [CreateAssetMenu(fileName = "BodyExpandSetting", menuName = "LazyPan/BodyExpandSetting")]
    public class BodyExpandSetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("体型扩散节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【体型扩散】管一个东西从小变大，长到头自动停下并走死亡。\n" +
            "— 配置参数（BodyExpandSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>MaxRadius</color>：长到这个半径就停，0=沿用节点上配的数\n" +
            "- <color=#FFD54F>ExpandSpeed</color>：每秒长大多少，建议 1~5，0=沿用节点上配的数\n" +
            "— 外部怎么互动 —\n" +
            "- <color=#FFD54F>停下</color>：长满自动停并喊死，不用你管";
        public List<BodyExpandSettingData> Datas = new List<BodyExpandSettingData>();

        public bool TryGet(string sourceSign, out BodyExpandSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("BodyExpandSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class BodyExpandSettingData {
        [EntitySign]
        [Header("实体 SourceSign")]
        [Tooltip("实体 Sign，与 ObjConfig.Sign 一致")]
        public string SourceSign;

        [Header("扩散到多大停（半径）")]
        [Tooltip("长到这个半径就停下并走死亡；<=0 时回退读 Data 的 MaxRadius")]
        public float MaxRadius = 5f;

        [Header("每秒长大多少")]
        [Tooltip("每秒半径增量；<=0 时回退读 Data 的 ExpandSpeed")]
        public float ExpandSpeed = 3f;
    }
}
