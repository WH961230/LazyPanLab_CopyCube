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
        [Header("节点便签说明 自由修改")]
        [Tooltip("三选一节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【三选一】管开奖面板，摆三张卡给人点一张，点后效果逐条生效。\n" +
            "— 配置参数（UIPickOneOfThreeSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>PanelPrefabSign</color>：面板用哪个\n" +
            "- <color=#FFD54F>MountSign</color>：挂在哪个点上，Root=主界面根\n" +
            "- <color=#FFD54F>WatchSign</color>：触发旗标签，空=不监听\n" +
            "- <color=#FFD54F>EnableTestAutoOpen</color>+<color=#FFD54F>AutoOpenDelay</color>：测试自动开奖，几秒后自动开\n" +
            "- <color=#FFD54F>Pool</color>：奖池，一张卡一行\n" +
            "— 每张卡怎么填 —\n" +
            "- <color=#FFD54F>Title</color>/<color=#FFD54F>Description</color>/<color=#FFD54F>IconName</color>：标题描述图标，图标可空\n" +
            "- <color=#FFD54F>Effects</color>：点卡后逐条生效，TargetEntitySign=给谁（Self=自己），ParamSign=改哪个数，对着类型填值";
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

        [Header("触发旗标签 空=不监听")]
        [Tooltip("触发旗 Bool 标签名，别的实体把它置 true 就开奖，开奖前自动重置 false。默认 WantPick，升级事件立的就是这面旗。留空=不监听，只走手动 Open")]
        public string WatchSign = DataLabels.WantPick;

        [Header("测试自动开奖开关")]
        [Tooltip("打开后进场景自动弹一次，验证面板用。正式接升级调用后关掉")]
        public bool EnableTestAutoOpen;

        [Tooltip("进场景多少秒后自动弹一次。正式接升级调用后把开关关掉")]
        [ShowIf("EnableTestAutoOpen", true, "测试自动开奖秒数")]
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
        public ParamModifyType Modify;

        [Tooltip("ValueType=Bool 时写入的值")]
        [ShowIf("ValueType", ParamValueType.Bool, "布尔值")]
        public bool BoolValue;

        [Tooltip("ValueType=Int 时的值(Set)或增量(Add)")]
        [ShowIf("ValueType", ParamValueType.Int, "整数值")]
        public int IntValue;

        [Tooltip("ValueType=Float 时的值(Set)或增量(Add)")]
        [ShowIf("ValueType", ParamValueType.Float, "浮点值")]
        public float FloatValue;

        [Tooltip("ValueType=String 时写入的值(只支持 Set)")]
        [ShowIf("ValueType", ParamValueType.String, "字符串值")]
        public string StringValue;

        [Tooltip("ValueType=Vector3 时的值(Set)或增量(Add)")]
        [ShowIf("ValueType", ParamValueType.Vector3, "向量值")]
        public Vector3 Vector3Value;
    }
}
