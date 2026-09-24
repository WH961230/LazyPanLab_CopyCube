using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 武器菜谱 — 只管有哪些枪、每把枪什么参数，不认识谁在开火。
    /// 一条=一个持有者的军火库，里面多把枪按 WeaponID 区分，当前用哪把由持有者 Data 的 CurrentWeapon 字符串决定。
    /// 开火行为只读菜谱+读名单+掐表，不认识枪名，加枪只加行。
    /// 配置来源 Setting/WeaponSetting，一个持有者一条。
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponSetting", menuName = "LazyPan/WeaponSetting")]
    public class WeaponSetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("武器开火节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【武器开火】管一把枪怎么打，军火库里一枪一行。\n" +
            "— 配置参数（WeaponSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>DefaultWeaponID</color>：开局拿哪把\n" +
            "- <color=#FFD54F>Weapons</color>：军火库，一枪一行\n" +
            "— 每把枪怎么填 —\n" +
            "- <color=#FFD54F>WeaponID</color>/<color=#FFD54F>WeaponName</color>：枪的编号和名字，编号全局唯一\n" +
            "- <color=#FFD54F>Kind</color>：直射=点射，环绕=围着转，范围=生产范围体\n" +
            "- <color=#FFD54F>TargetType</color>：打谁，索敌类型必填\n" +
            "- <color=#FFD54F>Range</color>/<color=#FFD54F>Interval</color>：射程和开火间隔秒数\n" +
            "- <color=#FFD54F>SpawnSign</color>：打出什么，直射范围填，环绕不填\n" +
            "- <color=#FFD54F>SpawnCount</color>/<color=#FFD54F>SpreadAngle</color>：一次打几个，散布总角度，0=无散布\n" +
            "- <color=#FFD54F>Payload</color>：传话包，写进打出东西的 Data 里，ParamSign=哪个数，对着类型填值";
        public List<WeaponSettingData> Datas = new List<WeaponSettingData>();

        public bool TryGet(string sourceSign, out WeaponSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("WeaponSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class WeaponSettingData {
        [EntitySign]
        [Header("挂枪的持有者 SourceSign")]
        [Tooltip("挂开火行为的持有者，必须与 ObjConfig.Sign 一致，如 Obj_Tower_SceneC_Tower")]
        public string SourceSign;

        [Header("默认武器ID")]
        [Tooltip("持有者 Data 里没有 CurrentWeapon 时用的枪，对应下表某行的 WeaponID，如 smg01")]
        public string DefaultWeaponID;

        [Header("军火库 一行一把枪")]
        [Tooltip("军火库列表，加新枪只加行，不用改代码")]
        public List<WeaponItem> Weapons = new List<WeaponItem>();
    }

    [Serializable]
    public class WeaponItem {
        [Header("武器ID 全局唯一 如smg01")]
        [Tooltip("武器ID，持有者 Data 的 CurrentWeapon 填它就换这把枪")]
        public string WeaponID;

        [Header("武器名 如冲锋枪")]
        [Tooltip("武器名，只做显示，不参与逻辑")]
        public string WeaponName;

        [Header("武器类型 直射=点射 环绕=围着转 范围=生范围体")]
        [Tooltip("Direct=直射(冲锋枪/狙击枪/霰弹枪)，Orbit=环绕(圆环/环绕球/散射卫星)，Area=范围(圆环扩散/手雷/激光)。切类型下面参数跟着换")]
        public WeaponKind Kind;

        [Header("索敌类型 必填 如Enemy")]
        [Tooltip("打谁，按实体 Type 扫，如 Enemy。触发器只管有没有人，不管怎么打。空=不开火，节点检查会标出来")]
        public string TargetType = "";

        [Header("射程米")]
        [Tooltip("多少米内有敌人才触发，环绕类型可填0(不索敌一直转)")]
        public float Range = 8f;

        [Header("触发间隔秒")]
        [Tooltip("多少秒触发一次，冲锋枪填0.2")]
        public float Interval = 0.2f;

        [EntitySign]
        [Header("生成实体 直射/范围填 环绕不填")]
        [Tooltip("触发时生成的实体 Sign，如子弹 Obj_Bullet_SceneC_Bullet、圆环 Obj_Ring_SceneC_Ring。生成物自己管表现，触发器不管。环绕类型不生成，不用填")]
        public string SpawnSign;

        [Header("一次生几个 霰弹填5")]
        [Tooltip("一次触发生成几个，霰弹枪填5配合散布角，默认1。环绕类型=同时转几个球")]
        public int SpawnCount = 1;

        [Header("散布角总度数 0=无散布")]
        [Tooltip("多个生成物扇形分开的总角度，如霰弹5发30度。单个填0")]
        public float SpreadAngle;

        [Header("传话包 写进生成物Data")]
        [Tooltip("传话包，触发器照单全写进新生实体 Data。生成物各认各的词，点开火节点上的一键补齐自动填，不用手打。环绕认 OrbitCount/OrbitRadius/OrbitSpeed/OrbitDamage")]
        public List<WeaponPayloadItem> Payload = new List<WeaponPayloadItem>();
    }

    [Serializable]
    public class WeaponPayloadItem {
        [Header("参数标签 必填")]
        [Tooltip("写进生成物 Data 的标签名，如 Damage / BulletSpeed / MaxRadius。必须与生成物行为认的词一致，写错读不到")]
        public string ParamSign;

        [Header("参数类型")]
        [Tooltip("参数类型，决定写哪一种 Data")]
        public ParamValueType ValueType;

        [Header("布尔值")]
        [Tooltip("ValueType=Bool 时写入的值")]
        [ShowIf("ValueType", ParamValueType.Bool, "布尔值")]
        public bool BoolValue;

        [Header("整数值")]
        [Tooltip("ValueType=Int 时写入的值")]
        [ShowIf("ValueType", ParamValueType.Int, "整数值")]
        public int IntValue;

        [Header("浮点值")]
        [Tooltip("ValueType=Float 时写入的值")]
        [ShowIf("ValueType", ParamValueType.Float, "浮点值")]
        public float FloatValue;

        [Header("字符串值")]
        [Tooltip("ValueType=String 时写入的值")]
        [ShowIf("ValueType", ParamValueType.String, "字符串值")]
        public string StringValue;

        [Header("向量值")]
        [Tooltip("ValueType=Vector3 时写入的值")]
        [ShowIf("ValueType", ParamValueType.Vector3, "向量值")]
        public Vector3 Vector3Value;
    }

    public enum WeaponKind {
        Direct = 0,
        Orbit = 1,
        Area = 2,
    }
}
