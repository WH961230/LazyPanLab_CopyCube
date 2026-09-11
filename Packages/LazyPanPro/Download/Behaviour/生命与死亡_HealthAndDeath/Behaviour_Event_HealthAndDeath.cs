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
        private bool isConfigValid;
        private bool isDead;
        private float invincibleRemainTime;
        private float deathDelayRemainTime;
        private FloatData _healthFloatData;
        private FloatData _maxHealthFloatData;
        private BoolData _deadBoolData;
        private Action<Behaviour_Event_HealthAndDeath> onDeath;
        private Action<Behaviour_Event_HealthAndDeath, float> onHealthChanged;
        private Action<Behaviour_Event_HealthAndDeath> onRevive;

        public Behaviour_Event_HealthAndDeath(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _healthData = entity.Prefab.AddComponent<HealthAndDeathData>();
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
            _config.InvincibleDurationOnHit = Mathf.Max(settingData.InvincibleDurationOnHit, 0f);
            _config.DeathDelay = Mathf.Max(settingData.DeathDelay, 0f);
            _config.CanRevive = settingData.CanRevive;
            _config.DeathAction = settingData.DeathAction;
            _config.ClearInvincibleOnDeath = settingData.ClearInvincibleOnDeath;

            InitRuntimeData();
            isConfigValid = true;

            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {

        }

        /// <summary>
        /// 初始化运行时数值 缺失时自动补挂到实体 Data 上
        /// </summary>
        private void InitRuntimeData() {
            Cond.Instance.TryGetData(entity, HEALTH_LABEL, out _healthFloatData);
            Cond.Instance.TryGetData(entity, MAXHEALTH_LABEL, out _maxHealthFloatData);
            Cond.Instance.TryGetData(entity, DEAD_LABEL, out _deadBoolData);

            _maxHealthFloatData.Float = _config.MaxHealth;
            _healthFloatData.Float = _config.InitHealth;
            _deadBoolData.Bool = false;
        }

        private void OnUpdate() {
            if (!isConfigValid) {
                return;
            }

            if (invincibleRemainTime > 0f) {
                invincibleRemainTime = Mathf.Max(0f, invincibleRemainTime - Time.deltaTime);
            }

            //延迟销毁计时 复活后自动中断
            if (isDead && _config.DeathAction == DeathAction.DestroyEntity && _config.DeathDelay > 0f) {
                deathDelayRemainTime -= Time.deltaTime;
                if (deathDelayRemainTime <= 0f) {
                    Obj.Instance.UnLoadEntity(entity);
                }
            }
        }

        #region 对外接口 调用方不感知配置细节

        /// <summary>
        /// 是否已死亡
        /// </summary>
        public bool IsDead => isDead;

        /// <summary>
        /// 当前生命
        /// </summary>
        public float CurrentHealth => _healthFloatData != null ? _healthFloatData.Float : 0f;

        /// <summary>
        /// 最大生命
        /// </summary>
        public float MaxHealth => _maxHealthFloatData != null ? _maxHealthFloatData.Float : 0f;

        /// <summary>
        /// 是否处于无敌
        /// </summary>
        public bool IsInvincible => invincibleRemainTime > 0f;

        /// <summary>
        /// 剩余无敌时长
        /// </summary>
        public float InvincibleRemainTime => invincibleRemainTime;

        /// <summary>
        /// 注册死亡回调
        /// </summary>
        public void AddDeathListener(Action<Behaviour_Event_HealthAndDeath> listener) {
            onDeath += listener;
        }

        /// <summary>
        /// 注销死亡回调
        /// </summary>
        public void RemoveDeathListener(Action<Behaviour_Event_HealthAndDeath> listener) {
            onDeath -= listener;
        }

        /// <summary>
        /// 注册血量变化回调 参数为实际变化量 正数回复 负数受伤
        /// </summary>
        public void AddHealthChangedListener(Action<Behaviour_Event_HealthAndDeath, float> listener) {
            onHealthChanged += listener;
        }

        /// <summary>
        /// 注销血量变化回调
        /// </summary>
        public void RemoveHealthChangedListener(Action<Behaviour_Event_HealthAndDeath, float> listener) {
            onHealthChanged -= listener;
        }

        /// <summary>
        /// 注册复活回调
        /// </summary>
        public void AddReviveListener(Action<Behaviour_Event_HealthAndDeath> listener) {
            onRevive += listener;
        }

        /// <summary>
        /// 注销复活回调
        /// </summary>
        public void RemoveReviveListener(Action<Behaviour_Event_HealthAndDeath> listener) {
            onRevive -= listener;
        }

        /// <summary>
        /// 扣血 无敌期间不生效 返回是否生效
        /// </summary>
        public bool TakeDamage(float amount) {
            if (!isConfigValid || isDead || amount <= 0f || invincibleRemainTime > 0f) {
                return false;
            }

            ChangeHealth(-amount);
            if (_config.InvincibleDurationOnHit > 0f) {
                invincibleRemainTime = _config.InvincibleDurationOnHit;
            }

            if (_healthFloatData.Float <= 0f) {
                Die();
            }

            return true;
        }

        /// <summary>
        /// 回血 只对未死亡实体生效 返回是否生效
        /// </summary>
        public bool Heal(float amount) {
            if (!isConfigValid || isDead || amount <= 0f) {
                return false;
            }

            ChangeHealth(amount);
            return true;
        }

        /// <summary>
        /// 直接设置生命 不触发死亡判定 常用于初始化或特殊逻辑
        /// </summary>
        public void SetHealth(float value) {
            if (!isConfigValid) {
                return;
            }

            _healthFloatData.Float = Mathf.Clamp(value, 0f, _maxHealthFloatData.Float);
        }

        /// <summary>
        /// 增加最大生命 同时等量回复当前生命
        /// </summary>
        public void AddMaxHealth(float amount) {
            if (!isConfigValid || amount <= 0f) {
                return;
            }

            _config.MaxHealth += amount;
            _maxHealthFloatData.Float = _config.MaxHealth;
            _healthFloatData.Float += amount;
        }

        /// <summary>
        /// 复活 只有配置允许且处于死亡中才生效
        /// </summary>
        public bool Revive() {
            if (!isConfigValid || !isDead || !_config.CanRevive || entity.Prefab == null) {
                return false;
            }

            isDead = false;
            _deadBoolData.Bool = false;
            _healthFloatData.Float = _config.MaxHealth;
            invincibleRemainTime = 0f;
            deathDelayRemainTime = 0f;
            entity.Prefab.SetActive(true);
            onRevive?.Invoke(this);
            return true;
        }

        /// <summary>
        /// 直接进入死亡流程 供外部强制处决
        /// </summary>
        public void Kill() {
            if (!isConfigValid || isDead) {
                return;
            }

            _healthFloatData.Float = 0f;
            Die();
        }

        /// <summary>
        /// 设置无敌时长 传0清除无敌
        /// </summary>
        public void SetInvincible(float duration) {
            if (!isConfigValid) {
                return;
            }

            invincibleRemainTime = Mathf.Max(0f, duration);
        }

        #endregion

        /// <summary>
        /// 血量统一入口 限幅并派发实际变化量
        /// </summary>
        private void ChangeHealth(float delta) {
            float before = _healthFloatData.Float;
            _healthFloatData.Float = Mathf.Clamp(before + delta, 0f, _maxHealthFloatData.Float);
            float actual = _healthFloatData.Float - before;
            if (Mathf.Abs(actual) > float.Epsilon) {
                onHealthChanged?.Invoke(this, actual);
            }
        }

        /// <summary>
        /// 死亡处理 同步状态与事件 延迟销毁交由 OnUpdate 计时
        /// </summary>
        private void Die() {
            if (isDead) {
                return;
            }

            isDead = true;
            _deadBoolData.Bool = true;
            if (_config.ClearInvincibleOnDeath) {
                invincibleRemainTime = 0f;
            }

            onDeath?.Invoke(this);

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
            onDeath = null;
            onHealthChanged = null;
            onRevive = null;
            base.Clear();
        }
    }
}
