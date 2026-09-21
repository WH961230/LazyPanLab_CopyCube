using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 肉鸽三选一 — 内容池，只管有啥，不管长啥样。
    /// UI 壳只认"纸条"(标题/描述/图标)，内容层只管奖池和效果包，中间拿纸条传话，互相看不见对方。
    /// 效果包复用老积木语义：目标实体+参数+Set/Add，跟 Death 的 OnDeathParams 一个模子，不写新逻辑。
    /// 配置来源 Setting/UIPickOneOfThreeSetting，一个实体一条，里面配一个奖池。
    /// </summary>
    [CreateAssetMenu(fileName = "UIPickOneOfThreeSetting", menuName = "LazyPan/UIPickOneOfThreeSetting")]
    public class UIPickOneOfThreeSetting : Setting {
        public List<UIPickOneOfThreeSettingData> Datas = new List<UIPickOneOfThreeSettingData>();

        public bool TryGet(string sourceSign, out UIPickOneOfThreeSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("UIPickOneOfThreeSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class UIPickOneOfThreeSettingData {
        [EntitySign]
        [Header("挂这个行为的实体类型 SourceSign")]
        [Tooltip("挂这个行为的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Player_SceneC_Player")]
        public string SourceSign;

        [Header("面板预制体标识 Bundles/Prefabs/UI下")]
        [Tooltip("三选一面板预制体，Bundles/Prefabs 下相对路径，如 UI/UI_PickOneOfThree。实例化到主界面挂点下。预制体契约:根上挂Comp；3个按钮命名Card0/1/2；每按钮下2个TMP命名CardX_Title/CardX_Desc；根不要加Canvas；Comp里Buttons/TextMeshProUGUIs把引用拖进去")]
        public string PanelPrefabSign;

        [Header("挂点标签 Root=主界面根")]
        [Tooltip("面板挂到主界面哪个Transform下。Root=主界面根节点，其他填主界面Comp里配置的Transform Sign")]
        public string MountSign;

        [Header("测试自动开奖秒数 0=关")]
        [Tooltip("测试开关，进场景多少秒后自动弹一次，验证面板用。正式接升级调用后填0关闭")]
        public float AutoOpenDelay;

        [Header("奖池 一张卡=一行")]
        [Tooltip("奖池列表，开奖时随机摸3张不同的。加新卡只加行，不用改代码")]
        public List<PickCardItem> Pool = new List<PickCardItem>();
    }

    [Serializable]
    public class PickCardItem {
        [Header("卡标题 如火球+1")]
        [Tooltip("卡标题，贴到面板按钮上，UI 不认字只管贴")]
        public string Title;

        [Header("卡描述 如伤害+50")]
        [Tooltip("卡描述，贴到面板上给玩家看")]
        public string Description;

        [Header("卡图标名 可空")]
        [Tooltip("卡图标名，预留，当前版本可空，以后贴图用了再填")]
        public string IconName;

        [Header("效果包 点卡后逐条生效")]
        [Tooltip("效果包，点卡后逐条改数。目标实体+参数+改法，跟死亡结算一个语义")]
        public List<PickCardEffect> Effects = new List<PickCardEffect>();
    }

    [Serializable]
    public class PickCardEffect {
        [EntitySign]
        [Header("被修改实体 必填 Self=自己")]
        [Tooltip("要改参数的目标实体 Sign，Self=挂行为的自己，如 Obj_Player_SceneC_Player。不允许为空")]
        public string TargetEntitySign = BehaviourSigns.Self;

        [Header("参数标签 必填")]
        [Tooltip("目标实体上的 Data 标签名，如 MaxHealth / MovementSpeed。不允许为空")]
        public string ParamSign;

        [Header("参数类型")]
        [Tooltip("参数类型，决定读写哪一种 Data")]
        public ParamValueType ValueType;

        [Header("修改方式")]
        [Tooltip("Set=直接赋值，Add=在原值上累加增量(只对 Int/Float/Vector3 有意义)")]
        public DeathModifyType Modify;

        [Header("布尔值")]
        [Tooltip("ValueType=Bool 时写入的值")]
        public bool BoolValue;

        [Header("整数值")]
        [Tooltip("ValueType=Int 时的值(Set)或增量(Add)")]
        public int IntValue;

        [Header("浮点值")]
        [Tooltip("ValueType=Float 时的值(Set)或增量(Add)")]
        public float FloatValue;

        [Header("字符串值")]
        [Tooltip("ValueType=String 时写入的值(只支持 Set)")]
        public string StringValue;

        [Header("向量值")]
        [Tooltip("ValueType=Vector3 时的值(Set)或增量(Add)")]
        public Vector3 Vector3Value;
    }
}
