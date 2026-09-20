using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>死亡 — 只做生命归零后的死亡处理与延迟销毁，不管血量数值。数值参数 Health/MaxHealth/Dead 归实体参数值(ParamValue)配置初始化，本行为只读写。</summary>
    [CreateAssetMenu(fileName = "DeathSetting", menuName = "LazyPan/DeathSetting")]
    public class DeathSetting : Setting {
        public List<DeathSettingData> Datas = new List<DeathSettingData>();

        public bool TryGet(string sourceSign, out DeathSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("DeathSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public struct DeathSettingData {
        [EntitySign]
        [Header("发起实体的类型标识 SourceSign")]
        [Tooltip("发起实体的类型标识，必须与 ObjConfig.Sign 一致，如 Obj_Enemy_SceneC_Enemy")]
        public string SourceSign;

        [Header("死亡延迟销毁时长 0=立即执行")]
        [Tooltip("死亡后延迟几秒再执行 DeathAction，0 立即执行")]
        public float DeathDelay;

        [Header("死亡后处理")]
        [Tooltip("死亡后处理：None=仅置 Dead 标记，DestroyEntity=销毁实体，DisableEntity=失活预制体")]
        public DeathAction DeathAction;

        [Header("死亡瞬间要改的参数列表")]
        [Tooltip("死亡瞬间(置 Dead 标记时)执行一次，一条=改一个实体的一个 Data，如敌人死后给玩家 Score 加分，或把自己 MovementStop 置真。为空=不改任何参数，老配置不受影响")]
        public List<DeathParamItem> OnDeathParams;
    }

    [Serializable]
    public class DeathParamItem {
        [EntitySign]
        [Header("被修改实体 必填 Self=自己")]
        [Tooltip("要改参数的目标实体 Sign，Self=死掉的自己。跨实体联动时填对方 Sign，如 Obj_Player_SceneC_Player。不允许为空")]
        public string TargetEntitySign = BehaviourSigns.Self;

        [Header("参数标签 必填")]
        [Tooltip("目标实体上的 Data 标签名，如 Score / Energy / MovementStop。不允许为空，不存在时自动创建")]
        public string ParamSign;

        [Header("参数类型")]
        [Tooltip("参数类型，决定读写哪一种 Data")]
        public ParamValueType ValueType;

        [Header("修改方式")]
        [Tooltip("Set=直接赋值，Add=在原值上累加增量(只对 Int/Float/Vector3 有意义)")]
        public DeathModifyType Modify;

        [Header("布尔值")]
        [Tooltip("ValueType=Bool 时写入的值(Set)")]
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

    public enum DeathModifyType {
        Set = 0,
        Add = 1,
    }

    public enum DeathAction {
        None = 0,
        DestroyEntity = 1,
        DisableEntity = 2,
    }
}
