using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>控制器WASD移动 — 读输入按 CharacterController 移动，带重力与转向。依赖 inputactions 与 CharacterController。</summary>
    [CreateAssetMenu(fileName = "InputWASDMoveSetting", menuName = "LazyPan/InputWASDMoveSetting")]
    public class InputWASDMoveSetting : Setting {
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
        [Header("发起移动的实体类型")]
        [Tooltip("发起移动的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Player_Player1")]
        public string SourceSign;

        [Header("输入控制标识")]
        [Tooltip("输入动作标识（InputRegister 路径），如 Player/Motion，需在 LazyPanInputControl.inputactions 中存在")]
        public string InputControlSign;

        [Header("移动速度")]
        [Tooltip("移动速度")]
        public float MoveSpeed;

        [Header("转向速度 每秒转向系数")]
        [Tooltip("转向速度，每秒转向系数，越大越快，0 不转向")]
        public float RotateSpeed;

        [Header("重力 负数向下")]
        [Tooltip("重力，负数向下（保持贴地）")]
        public float Gravity;
    }
}