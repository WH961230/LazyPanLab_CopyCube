using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 体型扩散配置占位 参数全在自己 Data 里 这里只做图节点与 Setting 的挂钩
    /// </summary>
    [CreateAssetMenu(fileName = "BodyExpandSetting", menuName = "LazyPan/BodyExpandSetting")]
    public class BodyExpandSetting : Setting {
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
