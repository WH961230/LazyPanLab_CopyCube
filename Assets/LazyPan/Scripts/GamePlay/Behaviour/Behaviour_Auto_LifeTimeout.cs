using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 零件 - 寿命
    /// 只干一件事: LifeTime 秒到 Dead 不认识谁 全经过 Data
    /// LifeTime=0 睡觉(一直活) 散场走死亡 无血条分支
    /// </summary>
    public class Behaviour_Auto_LifeTimeout : Behaviour {
        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is LifeTimeoutSettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (c.LifeTime <= 0f) {
                yellow.Add("时长<=0，一直活，不会喊死");
            }
        }
        /// <summary>寿命节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【寿命】管一个东西活几秒，时间到自动喊死。\n" +
            "— 配置参数（LifeTimeoutSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>LifeTime</color>：活几秒，建议 1~10，0=一直活";
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

            //寿命到点自己死：有血条走注册表扣光，无血条写死亡纸条，全程不碰死亡行为的数据类型
            if (EntityAttrRegistry.TryGetHealth(entity, out HealthAttr attr) && attr.HasBar && attr.Current > 0f) {
                attr.Damage(attr.Current);
            }

            if (entity != null && entity.Data != null) {
                BoolData deadFlag = null;
                if (!Cond.Instance.PeekData(entity, DataLabels.Dead, out deadFlag) || deadFlag == null) {
                    entity.Data.Add<BoolData>(DataLabels.Dead, DataLabels.Dead);
                    Cond.Instance.PeekData(entity, DataLabels.Dead, out deadFlag);
                }

                if (deadFlag != null) {
                    deadFlag.Bool = true;
                }
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
