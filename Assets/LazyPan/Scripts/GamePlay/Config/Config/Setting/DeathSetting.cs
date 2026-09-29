using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>死亡 — 只做生命归零后的死亡处理与延迟销毁，不管血量数值。数值参数 Health/MaxHealth/Dead 归实体参数值(ParamValue)配置初始化，本行为只读写。</summary>
    [CreateAssetMenu(fileName = "DeathSetting", menuName = "LazyPan/DeathSetting")]
    public class DeathSetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("死亡节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【死亡】管一个实体的死活，有血条按血量自动死，无血条靠外部喊死。\n" +
            "— 配置参数（DeathSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>Health</color>：初始血量，有血条（人/怪/塔）填正数，无血条（子弹/特效）填 0\n" +
            "- <color=#FFD54F>MaxHealth</color>：最大血量，0=无血条模式，只靠外部喊死；>0=有血条，血量<=0 自动死，血量回正自动活\n" +
            "- <color=#FFD54F>DeathDelay</color>：死后延迟几秒再执行 DeathAction，0=立即执行\n" +
            "- <color=#FFD54F>DeathAction</color>：None=只标记不处理，DestroyEntity=销毁实体，DisableEntity=失活预制体\n" +
            "- <color=#FFD54F>OnDeathParams</color>：死的一瞬间顺手改一批参数，一条改一个实体的一个数（比如敌人死后给玩家加 40 分），为空=啥也不改\n" +
            "— 外部怎么互动 —\n" +
            "- <color=#FFD54F>扣血</color>：有血条时把 Health 往下改，<=0 自动死；无血条时扣血没用\n" +
            "- <color=#FFD54F>喊死/复活</color>：把 Dead 置 true=喊死，置 false=复活；有血条复活前先把 Health 回正";
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
        public List<ParamModifyItem> OnDeathParams;

        [Header("初始血量 0=无血条")]
        [Tooltip("有血条(人/怪/塔)填正数，无血条(子弹/特效)填0。替代原来ParamValue里配Health/MaxHealth")]
        public float Health;

        [Header("最大血量 0=无血条")]
        public float MaxHealth;
    }

    public enum DeathAction {
        None = 0,
        DestroyEntity = 1,
        DisableEntity = 2,
    }
}
