using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 死亡
    /// 只做一件事: 监听实体 Data 的 Dead 标记 执行死亡处理与延迟销毁 不管理数值不做伤害判定
    /// 数值参数 Health/MaxHealth/Dead 归实体参数值(ParamValue)初始化与写入 血量增减规则归数值类行为 本行为只消费 Dead
    /// 配置来源 Setting/DeathSetting 运行时状态写自身 Data(Dead 归 ParamValue 初始化)
    /// </summary>
    public class Behaviour_Event_Death : Behaviour {
        private const string settingPath = "Setting/DeathSetting";
        public const string HEALTH_LABEL = "Health";
        public const string MAXHEALTH_LABEL = "MaxHealth";
        public const string DEAD_LABEL = "Dead";

        //config
        private DeathData _deathData;
        private DeathData.DeathConfig _config;

        //runtime
        private float deathDelayRemainTime;

        //data
        private FloatData _healthFloatData;
        private FloatData _maxHealthFloatData;
        private BoolData _deadBoolData;

        public Behaviour_Event_Death(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _deathData = entity.Prefab.AddComponent<DeathData>();
            _deathData.EntityID = entity.ID;

            DeathSetting setting = Loader.LoadAsset<DeathSetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out DeathSettingData settingData)) {
                return;
            }

            _config = _deathData.Config;
            _config.DeathDelay = Mathf.Max(settingData.DeathDelay, 0f);
            _config.DeathAction = settingData.DeathAction;

            if (!BindRuntimeData()) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 缺少 Dead 参数 请在 ParamValueSetting 为该实体配置 Bool 参数 Dead!", BehaviourSign, entity.ObjConfig.Sign);
                return;
            }

            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {
        }

        /// <summary>
        /// 绑定实体 Data 标签 Health/MaxHealth 只读用于展示 Dead 必须存在 归 ParamValue 初始化
        /// </summary>
        private bool BindRuntimeData() {
            bool hasHealth = Cond.Instance.TryGetData(entity, HEALTH_LABEL, out _healthFloatData);
            bool hasMaxHealth = Cond.Instance.TryGetData(entity, MAXHEALTH_LABEL, out _maxHealthFloatData);
            bool hasDead = Cond.Instance.TryGetData(entity, DEAD_LABEL, out _deadBoolData);
            return hasDead && hasHealth && hasMaxHealth;
        }

        private void OnUpdate() {
            //死亡标记有效时进入延迟销毁计时
            if (_deadBoolData.Bool && _config.DeathAction == DeathAction.DestroyEntity && _config.DeathDelay > 0f) {
                deathDelayRemainTime -= Time.deltaTime;
                if (deathDelayRemainTime <= 0f) {
                    Obj.Instance.UnLoadEntity(entity);
                }
            }
        }

        /// <summary>
        /// 处决 置 Dead 标记 触发死亡处理 外部可用作即死入口
        /// </summary>
        public void Kill() {
            SetDead(true);
        }

        /// <summary>
        /// 复活 清除 Dead 标记并复位计时
        /// </summary>
        public void Revive() {
            SetDead(false);
        }

        /// <summary>
        /// 死亡标记统一入口 去重后触发死亡处理
        /// </summary>
        private void SetDead(bool dead) {
            if (_deadBoolData.Bool == dead) {
                return;
            }

            _deadBoolData.Bool = dead;
            if (dead) {
                Die();
            } else {
                deathDelayRemainTime = 0f;
            }
        }

        /// <summary>
        /// 死亡处理 同步状态与事件 延迟销毁交由 OnUpdate 计时
        /// </summary>
        private void Die() {
            switch (_config.DeathAction) {
                case DeathAction.DestroyEntity:
                    if (_config.DeathDelay > 0f) {
                        deathDelayRemainTime = _config.DeathDelay;
                    } else {
                        Obj.Instance.UnLoadEntity(entity);
                    }

                    break;
                case DeathAction.DisableEntity:
                    entity.Prefab.SetActive(false);
                    break;
                case DeathAction.None:
                default:
                    break;
            }
        }

        public override void Clear() {
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            base.Clear();
        }
    }
}
