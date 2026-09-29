using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>开场Logo数据 仅承载行为参数 不含外部行为数据。</summary>
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
        }
    }
}
