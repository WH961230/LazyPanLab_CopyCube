using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    [CreateAssetMenu(fileName = "TeleportationSetting", menuName = "LazyPan/TeleportationSetting")]
    public class TeleportationSetting : Setting {
        [TextArea(5, 15)]
        public string MemoDoc =
            "【瞬间移动】管一个实体的瞬移，按按键、朝身体朝向、按曲线飞一段，不调别的移动行为。\n" +
            "停走命令看实体级 MoveAttr，谁置停都停。\n" +
            "— 配置参数（TeleportationSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>TeleportInputSign</color>：触发按键，填 InputRegister 里的那一个，如 Global/Space，错了就没反应\n" +
            "- <color=#FFD54F>Distance</color>：瞬移距离，0=原地罚站，建议 4~8\n" +
            "- <color=#FFD54F>Duration</color>：瞬移耗时（秒），走完曲线就停，建议 0.2~0.4\n" +
            "- <color=#FFD54F>SpeedCurve</color>：速度曲线，x=进度0~1，y=速度倍率，前快后慢就拉成前高后低\n" +
            "- <color=#FFD54F>Cooldown</color>：冷却（秒），防连按，建议 0.5~1.5\n" +
            "- <color=#FFD54F>TeleportPriority</color>：瞬移优先级，跟 MoveAttr.MovePriority 比，大就压住 WASD\n" +
            "— 数据交流（读写实体级 MoveAttr，不直接调别的行为）—\n" +
            "- <color=#FFD54F>停走</color>：MoveAttr.Stopped=true 全体移动行为一起停，false=恢复\n" +
            "- <color=#FFD54F>占领</color>：起飞置 MoveAttr.Teleporting=true，WASD 水平让路只留重力，落地=false=恢复\n" +
            "- <color=#FFD54F>位置</color>：推 CharacterController，方向取身体朝向";
        public List<TeleportationSettingData> Datas = new List<TeleportationSettingData>();

        public bool TryGet(string sourceSign, out TeleportationSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }
            data = default;
            LogUtil.LogErrorFormat("TeleportationSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class TeleportationSettingData {
        [EntitySign]
        public string SourceSign;
        [Header("触发按键的 Input 方案名，如 Global/Space")]
        public string TeleportInputSign = "Global/Space";
        [Header("瞬移距离")]
        public float Distance = 6f;
        [Header("瞬移耗时（秒），走完曲线就停")]
        public float Duration = 0.25f;
        [Header("速度曲线：x=进度0~1, y=速度倍率")]
        public AnimationCurve SpeedCurve;
        [Header("冷却（秒），防连按")]
        public float Cooldown = 1f;
        [Header("瞬移优先级，默认大于WASD，对比 MoveAttr.MovePriority")]
        public int TeleportPriority = 10;
    }
}
