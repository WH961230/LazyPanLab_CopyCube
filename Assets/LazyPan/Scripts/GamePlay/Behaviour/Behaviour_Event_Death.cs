using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 死亡
    /// 只做一件事: 监听实体 Health 状态并维护 Dead 标记 死亡瞬间按配置改一批参数 再执行死亡处理与延迟销毁
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
            CopyOnDeathParams(settingData);

            if (!BindRuntimeData()) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 缺少 Dead 参数 请在 ParamValueSetting 为该实体配置 Bool 参数 Dead!", BehaviourSign, entity.ObjConfig.Sign);
                return;
            }

            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {
        }

        /// <summary>
        /// 配置资产的参数项拷贝到实体 Data 与配置资产解耦 空标签项直接丢弃
        /// 老配置 OnDeathParams 为空时直接跳过 不影响原有死亡流程
        /// </summary>
        private void CopyOnDeathParams(DeathSettingData settingData) {
            _config.OnDeathParams.Clear();
            if (settingData.OnDeathParams == null) {
                return;
            }

            foreach (DeathParamItem item in settingData.OnDeathParams) {
                if (item == null) {
                    continue;
                }

                if (!BehaviourSigns.Require(item.TargetEntitySign, BehaviourSign, entity.ObjConfig?.Sign, nameof(DeathParamItem.TargetEntitySign))) {
                    continue;
                }

                if (!BehaviourSigns.Require(item.ParamSign, BehaviourSign, item.TargetEntitySign, nameof(DeathParamItem.ParamSign))) {
                    continue;
                }

                _config.OnDeathParams.Add(new DeathData.DeathParamConfig() {
                    TargetEntitySign = item.TargetEntitySign,
                    ParamSign = item.ParamSign,
                    ValueType = item.ValueType,
                    Modify = item.Modify,
                    BoolValue = item.BoolValue,
                    IntValue = item.IntValue,
                    FloatValue = item.FloatValue,
                    StringValue = item.StringValue,
                    Vector3Value = item.Vector3Value,
                });
            }
        }

        /// <summary>
        /// 死亡瞬间执行一次 按配置改一批参数 单项失败不影响其余项
        /// Set=直接赋值 Add=在原值上累加(只对 Int/Float/Vector3 有意义)
        /// </summary>
        private void ApplyOnDeathParams() {
            foreach (DeathData.DeathParamConfig config in _config.OnDeathParams) {
                if (!BehaviourSigns.ResolveEntity(entity, BehaviourSign, nameof(DeathParamItem.TargetEntitySign), config.TargetEntitySign, out Entity target)) {
                    continue;
                }

                switch (config.ValueType) {
                    case ParamValueType.Bool:
                        if (Cond.Instance.TryGetData(target, config.ParamSign, out BoolData boolData)) {
                            boolData.Bool = config.BoolValue;
                        }

                        break;
                    case ParamValueType.Int:
                        if (Cond.Instance.TryGetData(target, config.ParamSign, out IntData intData)) {
                            intData.Int = config.Modify == DeathModifyType.Add ? intData.Int + config.IntValue : config.IntValue;
                        }

                        break;
                    case ParamValueType.Float:
                        if (Cond.Instance.TryGetData(target, config.ParamSign, out FloatData floatData)) {
                            floatData.Float = config.Modify == DeathModifyType.Add ? floatData.Float + config.FloatValue : config.FloatValue;
                        }

                        break;
                    case ParamValueType.String:
                        if (Cond.Instance.TryGetData(target, config.ParamSign, out StringData stringData)) {
                            stringData.String = config.StringValue;
                        }

                        break;
                    case ParamValueType.Vector3:
                        if (Cond.Instance.TryGetData(target, config.ParamSign, out Vector3Data vector3Data)) {
                            vector3Data.Vector3 = config.Modify == DeathModifyType.Add ? vector3Data.Vector3 + config.Vector3Value : config.Vector3Value;
                        }

                        break;
                    default:
                        LogUtil.LogErrorFormat("行为:{0} 不支持的参数类型:{1}", BehaviourSign, config.ValueType);
                        break;
                }
            }
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
        /// 死亡处理 先按配置改一批参数 再做销毁/失活 延迟销毁交由 OnUpdate 计时
        /// </summary>
        private void Die() {
            ApplyOnDeathParams();
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
