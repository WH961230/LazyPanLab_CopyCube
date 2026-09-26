using System;
using UnityEngine;

namespace LazyPan {
    public class KnockbackData : Data {
        public KnockbackConfig Config = new KnockbackConfig();
        public bool IsKnockingBack;
        public float Elapsed;
        public float Duration;
        public float Distance;
        public Vector3 Direction = Vector3.zero;

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(KnockbackConfig)) {
                t = (T)Convert.ChangeType(Config, typeof(T));
                return true;
            }
            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class KnockbackConfig {
            public float Duration = 0.3f;
            public AnimationCurve DecayCurve;
            public int KnockbackPriority = 20;
        }
    }
}
