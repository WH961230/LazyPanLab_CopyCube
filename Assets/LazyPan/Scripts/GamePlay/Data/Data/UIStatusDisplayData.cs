using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 屏幕状态展示数据 仅承载行为参数 不含运行时状态与业务依赖
    /// </summary>
    public class UIStatusDisplayData : Data {
        [Header("屏幕状态展示参数")] public UIStatusDisplayConfig Config = new UIStatusDisplayConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(UIStatusDisplayConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class UIStatusDisplayConfig {
            [Header("屏幕UI块列表")] public List<UIStatusDisplayItem> Displays = new List<UIStatusDisplayItem>();
        }
    }
}
