using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 子弹飞行配置占位 飞行参数全在子弹自己的 Data 里(开火时写进去) 这里只做图节点与 Setting 的挂钩
    /// </summary>
    [CreateAssetMenu(fileName = "BulletFlySetting", menuName = "LazyPan/BulletFlySetting")]
    public class BulletFlySetting : Setting {
        public List<BulletFlySettingData> Datas = new List<BulletFlySettingData>();

        public bool TryGet(string sourceSign, out BulletFlySettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("BulletFlySetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class BulletFlySettingData {
        [EntitySign]
        [Header("子弹实体 SourceSign")]
        [Tooltip("子弹实体，必须与 ObjConfig.Sign 一致，如 Obj_Bullet_SceneC_SMG")]
        public string SourceSign;
    }
}
