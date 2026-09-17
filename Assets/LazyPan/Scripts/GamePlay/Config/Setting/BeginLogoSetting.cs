using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 开场Logo设置
    /// </summary>
    [CreateAssetMenu(fileName = "BeginLogoSetting", menuName = "LazyPan/BeginLogoSetting")]
    public class BeginLogoSetting : Setting {
        public List<BeginLogoSettingData> Datas = new List<BeginLogoSettingData>();

        public bool TryGet(string sourceSign, out BeginLogoSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("BeginLogoSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class BeginLogoSettingData {
        [EntitySign]
        [Header("发起挂载的实体类型 SourceSign")]
        [Tooltip("发起挂载的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Player_SceneB_Player")]
        public string SourceSign;

        [Header("挂载在哪个UI预制体上面")]
        [Tooltip("")]
        public string UIParentPrefabSign;
        
        [Header("挂载哪个预制体")]
        [Tooltip("")]
        public string UIChildPrefabSign;
        
        [Header("播放时间")]
        [Tooltip("")]
        public float LogoContinueTime;
    }
}