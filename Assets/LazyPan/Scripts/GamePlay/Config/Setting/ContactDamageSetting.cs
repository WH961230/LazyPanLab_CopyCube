using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 接触伤害配置占位 参数全在自己 Data 里 这里只做图节点与 Setting 的挂钩
    /// </summary>
    [CreateAssetMenu(fileName = "ContactDamageSetting", menuName = "LazyPan/ContactDamageSetting")]
    public class ContactDamageSetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("接触伤害节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【接触伤害】管一个东西碰到敌人扣血，碰到谁由触发器定，不用你配。\n" +
            "— 配置参数（ContactDamageSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>Damage</color>：碰一下扣多少血，0=睡觉不伤人\n" +
            "- <color=#FFD54F>DamageRadius</color>：多近算碰到，0=跟别人每帧写的那个走\n" +
            "- <color=#FFD54F>HitCooldown</color>：同一个敌人隔几秒才能再伤，-1=只伤一次\n" +
            "- <color=#FFD54F>MaxHits</color>：伤几个人后自己死，0=不限";
        public List<ContactDamageSettingData> Datas = new List<ContactDamageSettingData>();

        public bool TryGet(string sourceSign, out ContactDamageSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("ContactDamageSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class ContactDamageSettingData {
        [EntitySign]
        [Header("实体 SourceSign")]
        [Tooltip("实体 Sign，与 ObjConfig.Sign 一致")]
        public string SourceSign;

        [Header("单次伤害")]
        [Tooltip("<=0 睡觉；Data 有有效值时以 Data（传话包）为准")]
        public float Damage = 10f;

        [Header("伤害半径")]
        [Tooltip("<=0 表示跟 Data 走（比如圆环由体型扩散每帧写 DamageRadius）；Data 缺失才用这个")]
        public float DamageRadius;

        [Header("同目标再伤间隔")]
        [Tooltip("-1=只伤一次；Data 有有效值时以 Data 为准")]
        public float HitCooldown = -1f;

        [Header("命中几次后自己死")]
        [Tooltip("0=不限；Data 有有效值时以 Data 为准")]
        public int MaxHits;
    }
}
