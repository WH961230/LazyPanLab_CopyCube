using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 三选一数据 仅承载行为参数与本轮纸条 不含业务逻辑
    /// </summary>
    public class UIPickOneOfThreeData : Data {
        [Header("三选一行为参数")] public UIPickOneOfThreeConfig Config = new UIPickOneOfThreeConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(UIPickOneOfThreeConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class UIPickOneOfThreeConfig {
            [Header("面板预制体标识")] public string PanelPrefabSign;
            [Header("挂点标签")] public string MountSign;
            [Header("触发旗标签 空=不监听")] public string WatchSign;
            [Header("测试自动开奖开关")] public bool EnableTestAutoOpen;
            [Header("测试自动开奖秒数")] public float AutoOpenDelay;
            [Header("奖池")] public List<PickCardItem> Pool = new List<PickCardItem>();
            [Header("本轮三张卡下标 调试用")] public List<int> CurrentPicks = new List<int>();
            [Header("面板是否打开 调试用")] public bool IsOpen;
        }
    }
}
