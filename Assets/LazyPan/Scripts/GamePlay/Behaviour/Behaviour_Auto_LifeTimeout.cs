using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 零件 - 寿命
    /// 只干一件事: LifeTime 秒到 Dead 不认识谁 全经过 Data
    /// LifeTime=0 睡觉(一直活) 散场走死亡 无血条分支
    /// </summary>
    public class Behaviour_Auto_LifeTimeout : Behaviour {
        /// <summary>
        /// 对外契约
        /// </summary>
        public static readonly PayloadContractDef[] RequiredPayload = {
            new PayloadContractDef() { Sign = "LifeTime", ValueType = ParamValueType.Float, FloatDefault = 3f },
        };

        /// <summary>
        /// 模块依赖 到点散场走死亡 无血条分支
        /// </summary>
        public static readonly string[] RequiredModules = { "死亡" };

        //runtime
        private float remain;
        private bool isConfigValid;

        public Behaviour_Auto_LifeTimeout(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            float settingLife = 0f;
            LifeTimeoutSetting setting = Loader.LoadAsset<LifeTimeoutSetting>(AssetType.ASSET, "Setting/LifeTimeoutSetting");
            if (setting != null && setting.TryGet(entity.ObjConfig.Sign, out LifeTimeoutSettingData data)) {
                settingLife = data.LifeTime;
            }

            Cond.Instance.TryGetData(entity, "LifeTime", out FloatData lifeTime);
            remain = lifeTime != null ? lifeTime.Float : settingLife;
            if (remain <= 0f) {
                return;
            }

            isConfigValid = true;
            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {
        }

        private void OnUpdate() {
            if (!isConfigValid) {
                return;
            }

            remain -= Time.deltaTime;
            if (remain > 0f) {
                return;
            }

            if (Cond.Instance.TryGetData(entity, DataLabels.Dead, out BoolData dead)) {
                dead.Bool = true;
            }

            if (Game.instance != null) {
                Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            }
            isConfigValid = false;
        }

        public override void Clear() {
            if (Game.instance != null) {
                Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            }
            base.Clear();
        }
    }
}
