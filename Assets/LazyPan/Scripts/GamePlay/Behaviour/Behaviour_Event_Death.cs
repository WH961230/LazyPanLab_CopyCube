using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 死亡
    /// 只做一件事: 监听实体 Health 状态并维护 Dead 标记 执行死亡处理与延迟销毁
    /// Health 由其他数值行为修改 本行为不感知伤害来源 不调用其他行为
    /// 配置来源 Setting/DeathSetting 运行时状态写入实体 Data(Dead)
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
            _deathData = AttachBehaviourData<DeathData>();

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
        /// 绑定实体 Data 标签 Health/MaxHealth/Dead。
        /// Health 与 MaxHealth 用于状态读取，Dead 由本行为维护。
        /// </summary>
        private bool BindRuntimeData() {
            bool hasHealth = Cond.Instance.TryGetData(entity, HEALTH_LABEL, out _healthFloatData);
            bool hasMaxHealth = Cond.Instance.TryGetData(entity, MAXHEALTH_LABEL, out _maxHealthFloatData);
            bool hasDead = Cond.Instance.TryGetData(entity, DEAD_LABEL, out _deadBoolData);
            return hasDead && hasHealth && hasMaxHealth;
        }

        private void OnUpdate() {
            if (!_deadBoolData.Bool && _healthFloatData.Float <= 0f) {
                SetDead();
            }

            //死亡标记有效时进入延迟销毁计时
            if (_deadBoolData.Bool && _config.DeathAction == DeathAction.DestroyEntity && _config.DeathDelay > 0f) {
                deathDelayRemainTime -= Time.deltaTime;
                if (deathDelayRemainTime <= 0f) {
                    Obj.Instance.UnLoadEntity(entity);
                }
            }
        }

        /// <summary>
        /// 复活由外部直接把 Health 改回正数后自动恢复存活状态。
        /// </summary>
        public void Revive() {
            if (_deadBoolData.Bool && _healthFloatData.Float > 0f) {
                _deadBoolData.Bool = false;
                deathDelayRemainTime = 0f;
            }
        }

        /// <summary>
        /// Health 小于等于 0 时统一进入死亡状态。
        /// </summary>
        private void SetDead() {
            if (_deadBoolData.Bool) {
                return;
            }

            _deadBoolData.Bool = true;
            Die();
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
            DetachBehaviourData<DeathData>();
            base.Clear();
        }
    }
}
