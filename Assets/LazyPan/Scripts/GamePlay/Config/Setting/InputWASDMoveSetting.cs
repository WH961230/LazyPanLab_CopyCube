using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>控制器WASD移动 — 读输入按 CharacterController 移动，带重力与转向。依赖 inputactions 与 CharacterController。</summary>
    [CreateAssetMenu(fileName = "InputWASDMoveSetting", menuName = "LazyPan/InputWASDMoveSetting")]
    public class InputWASDMoveSetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("控制器移动节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【控制器移动】管一个实体的 WASD 走路，读输入、推 CharacterController，不调别的移动行为。\n" +
            "停走命令看实体级 MoveAttr，谁置停都停。\n" +
            "— 配置参数（InputWASDMoveSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>InputControlSign</color>：输入方案名，填 InputRegister 里的那一个，错了就没反应\n" +
            "- <color=#FFD54F>MoveSpeed</color>：移动速度，0=不动，建议 3~8\n" +
            "- <color=#FFD54F>RotateSpeed</color>：转向速度，0=不转身，建议 5~15\n" +
            "- <color=#FFD54F>Gravity</color>：重力加速度，一般填负数（如 -20），落地后自动压住\n" +
            "— 数据交流（读写实体级 MoveAttr，不直接调别的行为）—\n" +
            "- <color=#FFD54F>停走</color>：MoveAttr.Stopped=true 全体移动行为一起停，false=恢复\n" +
            "- <color=#FFD54F>位置</color>：推 CharacterController，身体朝向跟着输入转";
        public List<InputWASDMoveSettingData> Datas = new List<InputWASDMoveSettingData>();

        public bool TryGet(string sourceSign, out InputWASDMoveSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("InputWASDMoveSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public struct InputWASDMoveSettingData {
        [EntitySign]
        [Header("SourceSign 发起移动的实体类型")]
        [Tooltip("必须与 ObjConfig.Sign 一致，如 Obj_Player_SceneC_Player。对不上这条配置就不会被这个实体用到")]
        public string SourceSign;

        [Header("InputControlSign 输入方案名")]
        [Tooltip("填 InputRegister 里的那一个，如 Player/Motion。填错就没反应，先去 inputactions 里确认名字")]
        public string InputControlSign;

        [Header("MoveSpeed 移动速度")]
        [Tooltip("移动速度，0=不动，建议 3~8。填负数会被钳到 0")]
        public float MoveSpeed;

        [Header("RotateSpeed 转向速度")]
        [Tooltip("转向速度，0=不转身，建议 5~15。越大转身越快")]
        public float RotateSpeed;

        [Header("Gravity 重力")]
        [Tooltip("重力加速度，一般填负数（如 -20）。落地后自动压住，填正数会往天上飘")]
        public float Gravity;
    }
}