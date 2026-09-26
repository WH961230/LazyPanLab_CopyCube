using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 击退 — 被动行为，只管飞，不管谁喊的。
    /// 别人（比如接触伤害）调 Behaviour_Auto_Knockback.Request 写方向和力度，行为下一帧自动开飞。
    /// 飞全程推 CharacterController，按衰减曲线减速，时间到自动松开 MoveAttr.KnockingBack。
    /// 配置来源 Setting/KnockbackSetting，运行时状态只写自己的 KnockbackData。
    /// </summary>
    [CreateAssetMenu(fileName = "KnockbackSetting", menuName = "LazyPan/KnockbackSetting")]
    public class KnockbackSetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("击退节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【击退】管一个实体被打飞，别人喊（接触伤害命中）才飞，自己不监听按键。\n" +
            "停走命令看实体级 MoveAttr，谁置停都停。\n" +
            "— 配置参数（KnockbackSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>Duration</color>：默认飞多久（秒），喊的人没给时长就用这个\n" +
            "- <color=#FFD54F>DecayCurve</color>：减速曲线，x=进度0~1，y=速度倍率，空=匀速\n" +
            "- <color=#FFD54F>KnockbackPriority</color>：击退优先级，跟 MoveAttr.MovePriority/TeleportPriority 比，默认最大\n" +
            "— 数据交流（读写实体级 MoveAttr，不直接调别的行为）—\n" +
            "- <color=#FFD54F>停走</color>：MoveAttr.Stopped=true 全体移动行为一起停，false=恢复\n" +
            "- <color=#FFD54F>占领</color>：起飞置 MoveAttr.KnockingBack=true，WASD 和瞬移水平让路只留重力，落地=false=恢复\n" +
            "- <color=#FFD54F>位置</color>：推 CharacterController，方向是喊的人给的";
        public List<KnockbackSettingData> Datas = new List<KnockbackSettingData>();

        public bool TryGet(string sourceSign, out KnockbackSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("KnockbackSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class KnockbackSettingData {
        [EntitySign]
        [Header("会被击退的实体类型 SourceSign")]
        [Tooltip("必须与 ObjConfig.Sign 一致，如 Obj_Player_SceneC_Player。对不上这条配置就不会被这个实体用到")]
        public string SourceSign;

        [Header("默认击退时长（秒）")]
        [Tooltip("喊的人没给时长就用这个，建议 0.2~0.4")]
        public float Duration = 0.3f;

        [Header("减速曲线：x=进度0~1, y=速度倍率")]
        [Tooltip("前快后慢就拉成前高后低，空=匀速飞完")]
        public AnimationCurve DecayCurve;

        [Header("击退优先级，默认最大，对比 MoveAttr.MovePriority/TeleportPriority")]
        [Tooltip("默认 20，大于瞬移 10 和走路 0。填小就压不住别人")]
        public int KnockbackPriority = 20;
    }
}
