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
            "【接触伤害】管一个东西碰到敌人扣血，Data 里有人写就用写的，没人写用 Setting 保底，不认识触发器和击退。\n" +
            "— 配置参数（ContactDamageSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>TargetType</color>：打哪类实体，如 Player，空=不伤人\n" +
            "- <color=#FFD54F>Damage</color>：碰一下扣多少血，0=睡觉不伤人\n" +
            "- <color=#FFD54F>DamageRadius</color>：多近算碰到，Data 有有效值先用 Data 的\n" +
            "- <color=#FFD54F>HitCooldown</color>：同一个敌人隔几秒才能再伤，-1=只伤一次\n" +
            "- <color=#FFD54F>MaxHits</color>：伤几个人后自己死，0=不限\n" +
            "- <color=#FFD54F>KnockbackDistance</color>：本次打击自带推人几米（旧名，实为打击属性），0=不推，对方没人看纸条就只扣血\n" +
            "- <color=#FFD54F>KnockbackDuration</color>：推人飞多久（秒），方向固定 B 减 A 压平指向 B";
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

        [Header("目标实体类型 空=不伤人")]
        [Tooltip("打哪类实体，如 Player。Data 里写的优先，这个是保底，两边都空就不伤人")]
        public string TargetType;

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

        [Header("命中推人距离 0=不推")]
        [Tooltip("本次打击自带属性：命中后把对方推出几米，0=只扣血不推。对方没挂击退行为就是纸条没人看，也只扣血")]
        public float KnockbackDistance;

        [Header("推人时长（秒）")]
        [Tooltip("推出去飞多久，方向固定 B 减 A 压平指向 B")]
        public float KnockbackDuration = 0.3f;

        [Header("伤害记在谁头上 空=打谁扣谁")]
        [Tooltip("比如敌人蹭到塔，血想扣玩家的，就填 Player。空=扣被碰到的那个")]
        public string DamageToType;

        [Header("多段打击（可空） 有段=老字段全歇")]
        [Tooltip("一物多打：一段打一类人，各自独立。填了这里，上面老字段全部不参与，只当摆设；空着=老单组模式")]
        public List<ContactHitItem> Hits = new List<ContactHitItem>();
    }

    [Serializable]
    public class ContactHitItem {
        [Header("目标实体类型")]
        [Tooltip("打哪类实体，如 Player，空=这条不伤人")]
        public string TargetType;

        [Header("单次伤害")]
        public float Damage = 10f;

        [Header("伤害半径")]
        public float DamageRadius;

        [Header("同目标再伤间隔 -1=只伤一次")]
        public float HitCooldown = -1f;

        [Header("命中几次后自己死 0=不限")]
        public int MaxHits;

        [Header("命中推人距离 0=不推")]
        public float KnockbackDistance;

        [Header("推人时长（秒）")]
        public float KnockbackDuration = 0.3f;

        [Header("伤害记在谁头上 空=打谁扣谁")]
        [Tooltip("比如敌人蹭到塔，血想扣玩家的，就填 Player。空=扣被碰到的那个")]
        public string DamageToType;
    }
}
