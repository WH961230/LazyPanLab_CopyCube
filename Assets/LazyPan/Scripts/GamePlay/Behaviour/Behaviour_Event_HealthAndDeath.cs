using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 生命与死亡
    /// 只负责该实体的血量增减与死亡处理 不引入业务 死亡表现由配置驱动 可移植性高
    /// 配置来源 Setting/HealthAndDeathSetting 运行时数值同步到实体 Data 便于其他行为或 UI 读取
    /// </summary>
    public class Behaviour_Event_HealthAndDeath : Behaviour {
        private const string settingPath = "Setting/HealthAndDeathSetting";
        public const string HEALTH_LABEL = "Health";
        public const string MAXHEALTH_LABEL = "MaxHealth";
        public const string DEAD_LABEL = "Dead";

        //config
        private HealthAndDeathData _healthData;
        private HealthAndDeathData.HealthAndDeathConfig _config;

        //runtime
        private float deathDelayRemainTime;

        //data
        private FloatData _healthFloatData;
        private FloatData _maxHealthFloatData;
        private BoolData _deadBoolData;

        public Behaviour_Event_HealthAndDeath(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _healthData = entity.Prefab.AddComponent<HealthAndDeathData>();
            _healthData.EntityID = entity.ID;
            
            HealthAndDeathSetting setting = Loader.LoadAsset<HealthAndDeathSetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out var settingData)) {
                return;
            }

            _config = _healthData.Config;
            _config.MaxHealth = Mathf.Max(settingData.MaxHealth, 1f);
            _config.InitHealth = Mathf.Clamp(_config.MaxHealth * settingData.InitHealthRate, 0f, _config.MaxHealth);
            _config.DeathDelay = Mathf.Max(settingData.DeathDelay, 0f);
            _config.DeathAction = settingData.DeathAction;

            InitRuntimeData();

            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {
        }

        private void InitRuntimeData() {
            Cond.Instance.TryGetData(entity, HEALTH_LABEL, out _healthFloatData);
            Cond.Instance.TryGetData(entity, MAXHEALTH_LABEL, out _maxHealthFloatData);
            Cond.Instance.TryGetData(entity, DEAD_LABEL, out _deadBoolData);

            _maxHealthFloatData.Float = _config.MaxHealth;
            _healthFloatData.Float = _config.InitHealth;
            _deadBoolData.Bool = false;
        }

        private void OnUpdate() {
            //延迟销毁计时 复活后自动中断
            if (_deadBoolData.Bool && _config.DeathAction == DeathAction.DestroyEntity && _config.DeathDelay > 0f) {
                deathDelayRemainTime -= Time.deltaTime;
                if (deathDelayRemainTime <= 0f) {
                    Obj.Instance.UnLoadEntity(entity);
                }
            }
        }

        /// <summary>
        /// 扣血 返回是否生效
        /// </summary>
        public bool TakeDamage(float amount) {
            if (_deadBoolData.Bool || amount <= 0f) return false;
            ChangeHealth(-amount);
            if (_healthFloatData.Float <= 0f) Die();
            return true;
        }

        /// <summary>
        /// 回血 只对未死亡实体生效 返回是否生效
        /// </summary>
        public bool Heal(float amount) {
            if (_deadBoolData.Bool || amount <= 0f) return false;
            ChangeHealth(amount);
            return true;
        }

        /// <summary>
        /// 直接设置生命 触发死亡判定
        /// </summary>
        public void SetHealth(float value) {
            if (_deadBoolData.Bool) return;
            _healthFloatData.Float = Mathf.Clamp(value, 0f, _maxHealthFloatData.Float);
            if (_healthFloatData.Float == 0f) {
                _deadBoolData.Bool = true;
                Die();
            }
        }

        /// <summary>
        /// 增加最大生命 同时等量回复当前生命
        /// </summary>
        public void AddMaxHealthWithCurrentHealth(float amount) {
            if (amount <= 0f) return;
            _config.MaxHealth += amount;
            _maxHealthFloatData.Float = _config.MaxHealth;
            _healthFloatData.Float += amount;
        }
        
        /// <summary>
        /// 仅增加最大生命
        /// </summary>
        public void AddMaxHealthOnly(float amount) {
            if (amount <= 0f) return;
            _config.MaxHealth += amount;
            _maxHealthFloatData.Float = _config.MaxHealth;
        }

        /// <summary>
        /// 血量统一入口 限幅并派发实际变化量
        /// </summary>
        private void ChangeHealth(float delta) {
            float before = _healthFloatData.Float;
            _healthFloatData.Float = Mathf.Clamp(before + delta, 0f, _maxHealthFloatData.Float);
        }

        /// <summary>
        /// 死亡处理 同步状态与事件 延迟销毁交由 OnUpdate 计时
        /// </summary>
        private void Die() {
            if (_deadBoolData.Bool) return;
            _deadBoolData.Bool = true;
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