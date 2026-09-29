using System;
using UnityEngine;

namespace LazyPan {
    public class TeleportationData : Data {
        public TeleportationConfig Config = new TeleportationConfig();
        public bool IsTeleporting;
        public float Elapsed;
        public float CooldownLeft;
        public Vector3 Direction;

        public override bool Get<T>(string sign, out T t) {
            if (typeof(T) == typeof(TeleportationConfig)) {
                t = (T)Convert.ChangeType(Config, typeof(T));
                return true;
            }
            t = default;
            return base.Get(sign, out t);
        }

        [Serializable]
        public class TeleportationConfig {
            public string TeleportInputSign = "Global/Space";
            public float Distance = 6f;
            public float Duration = 0.25f;
            public AnimationCurve SpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
            public float Cooldown = 1f;
            public int TeleportPriority = 10;
        }
    }
}
