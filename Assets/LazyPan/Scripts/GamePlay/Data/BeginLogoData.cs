using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>装备挂载数据 仅承载行为参数 不含外部行为数据。</summary>
    public class BeginLogoData : Data {
        [Header("行为参数")] public BeginLogoConfig Config = new BeginLogoConfig();

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(BeginLogoConfig)) {
                t = (T) Convert.ChangeType(Config, typeof(T));
                return true;
            }

            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class BeginLogoConfig {
            [Header("播放完成后跳转场景")] public string EndJumpToScene;
        }
    }
}