using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 控制器WASD移动数据 仅承载行为参数 不含运行时状态与业务依赖
    /// </summary>
    public class InputWASDMoveData : Data {
        [Header("控制器WASD移动参数")] public InputWASDMoveConfig Config = new InputWASDMoveConfig();

        [Header("运行时：输入向量，回调里实时更新，外部不用管")]
        public Vector2 InputVec;

        [Header("运行时：下坠速度，内部累计，落地自动回压")]
        public float YSpeed;

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(InputWASDMoveConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class InputWASDMoveConfig {
            [Header("输入控制标识")] public string InputControlSign;
            [Header("移动速度")] public float MoveSpeed;
            [Header("转向速度")] public float RotateSpeed;
            [Header("重力")] public float Gravity;
        }
    }
}
