using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 开场Logo设置
    /// </summary>
    [CreateAssetMenu(fileName = "BeginLogoSetting", menuName = "LazyPan/BeginLogoSetting")]
    public class BeginLogoSetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("开头Logo节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【开头Logo】管开场播几秒 Logo，播完自动跳下一步。\n" +
            "— 配置参数（BeginLogoSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>UIParentPrefabSign</color>：Logo 挂在哪个界面上，如 UI/UI_SceneA\n" +
            "- <color=#FFD54F>UIChildPrefabSign</color>：挂哪个 Logo，如 UI/UI_Logo\n" +
            "- <color=#FFD54F>LogoContinueTime</color>：播几秒，建议 3~8，0=一闪而过";
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
        [Tooltip("Logo 挂在哪个界面上，如 UI/UI_SceneA，填错就不显示")]
        public string UIParentPrefabSign;
        
        [Header("挂载哪个预制体")]
        [Tooltip("挂哪个 Logo，如 UI/UI_Logo")]
        public string UIChildPrefabSign;
        
        [Header("播放时间")]
        [Tooltip("播几秒，建议 3~8，0=一闪而过")]
        public float LogoContinueTime;
    }
}