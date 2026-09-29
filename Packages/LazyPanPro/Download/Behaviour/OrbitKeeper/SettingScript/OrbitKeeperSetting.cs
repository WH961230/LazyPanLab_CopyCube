using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 环绕养球菜谱 — 只管养哪些球，加新球只加行，不认识谁在养。
    /// 一条=一个持有者的养球库，结构与 WeaponSetting.WeaponItem 复用，只看 Kind=环绕的行。
    /// </summary>
    [CreateAssetMenu(fileName = "OrbitKeeperSetting", menuName = "LazyPan/OrbitKeeperSetting")]
    public class OrbitKeeperSetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("环绕养球节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【环绕养球】管常驻转圈的球，球不够补，球多了散，不掐表不索敌。\n" +
            "— 配置参数（OrbitKeeperSetting 里按 SourceSign 配，只看 Kind=环绕的行）—\n" +
            "- <color=#FFD54F>WeaponID</color>/<color=#FFD54F>WeaponName</color>：养哪把，默认枪是环绕就养默认，否则养第一把环绕\n" +
            "- <color=#FFD54F>TargetType</color>：球打谁，写进球的交接\n" +
            "- <color=#FFD54F>SpawnSign</color>：养什么球，如环绕球\n" +
            "- <color=#FFD54F>UseFireCondition</color>：勾上才看条件，不满足就把球散掉\n" +
            "- <color=#FFD54F>ConditionParam</color>：看自己哪个数，如Energy\n" +
            "- <color=#FFD54F>ConditionCompare</color>：0大于 1大于等于 2等于 3小于等于 4小于 5不等\n" +
            "- <color=#FFD54F>ConditionValue</color>：和多少比，如0\n" +
            "- <color=#FFD54F>Payload</color>：OrbitCount=几个球 OrbitRadius=多远 OrbitSpeed=多快，其余传话包写进球的注册表";
        public List<WeaponSettingData> Datas = new List<WeaponSettingData>();

        public bool TryGet(string sourceSign, out WeaponSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }
            data = default;
            LogUtil.LogErrorFormat("OrbitKeeperSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }
}
