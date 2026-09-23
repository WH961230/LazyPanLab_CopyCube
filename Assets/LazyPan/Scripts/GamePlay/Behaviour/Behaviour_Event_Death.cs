using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 死亡
    /// 只做一件事: 维护 Dead 标记 死亡瞬间按配置改一批参数 再执行死亡处理与延迟销毁
    /// 有血条(人/怪/塔): Health<=0 自动死 Health 回正自动活 不感知伤害来源 不调用其他行为
    /// 无血条(子弹/特效等一次性东西): 没有 Health 纯靠外部把 Dead 置 true 喊死 置 false 复活
    /// 配置来源 Setting/DeathSetting 运行时状态写入实体 Data(Dead)
    /// </summary>
    public class Behaviour_Event_Death : Behaviour {
        /// <summary>死亡节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【死亡】管一个实体的死活，有血条按血量自动死，无血条靠外部置 Dead。\n" +
            "本行为数据自带，不用去 ParamValue 配任何东西。\n" +
            "— 配置参数（DeathSetting 里按 SourceSign 配）—\n" +
            "- Health：初始血量，有血条(人/怪/塔)填正数，无血条(子弹/特效)填0\n" +
            "- MaxHealth：最大血量，0=无血条模式；>0=有血条，Health<=0 自动死，Health 回正自动活\n" +
            "- DeathDelay：死后延迟几秒再执行 DeathAction，0=立即执行\n" +
            "- DeathAction：None=只置 Dead 标记，DestroyEntity=销毁实体，DisableEntity=失活预制体\n" +
            "- OnDeathParams：死的一瞬间改一批参数，一条=改一个实体的一个数（如敌人死后给玩家加分），为空=啥也不改\n" +
            "— 运行时数据（DeathData，自己管）—\n" +
            "- Health：当前血量，别人扣血改这个字段就行，有血条时<=0 自动死、回正自动活\n" +
            "- Dead：死亡标记，无血条时外部置 true=喊死，置 false=复活\n" +
            "— 数据交流（都读写 DeathData，不直接调别的行为）—\n" +
            "- 扣血：改 Health，有血条看 HasHealthBar（MaxHealth>0）\n" +
            "- 喊死/复活：改 Dead，有血条复活前先把 Health 回正";
        private const string settingPath = "Setting/DeathSetting";
        public const string HEALTH_LABEL = "Health";
        public const string MAXHEALTH_LABEL = "MaxHealth";
        public const string DEAD_LABEL = "Dead";

        //config
        private DeathData _deathData;
        private DeathData.DeathConfig _config;
        private HealthAttr _health;

        //runtime
        private float deathDelayRemainTime;
        private bool hasHealthBar;
        private bool prevDead;

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

            _deathData.Health = settingData.Health;
            _deathData.MaxHealth = settingData.MaxHealth;
            _deathData.Dead = false;
            BindRuntimeData();
            //实体级注册：死亡行为是 Health 唯一生产者，其余行为只消费
            _health = new HealthAttr() { Current = _deathData.Health, Max = _deathData.MaxHealth };
            EntityAttrRegistry.RegisterHealth(entity, _health);

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

            foreach (ParamModifyItem item in settingData.OnDeathParams) {
                if (item == null) {
                    continue;
                }

                if (!BehaviourSigns.Require(item.TargetEntitySign, BehaviourSign, entity.ObjConfig?.Sign, nameof(ParamModifyItem.TargetEntitySign))) {
                    continue;
                }

                if (!BehaviourSigns.Require(item.ParamSign, BehaviourSign, item.TargetEntitySign, nameof(ParamModifyItem.ParamSign))) {
                    continue;
                }

                _config.OnDeathParams.Add(new DeathData.ParamModifyConfig() {
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
        /// Health/Dead 优先走对方 DeathData 强类型(字典缓存无GetComponent) 其他标签走旧通用兜底
        /// </summary>
        private void ApplyOnDeathParams() {
            foreach (DeathData.ParamModifyConfig config in _config.OnDeathParams) {
                if (!BehaviourSigns.ResolveEntity(entity, BehaviourSign, nameof(ParamModifyItem.TargetEntitySign), config.TargetEntitySign, out Entity target)) {
                    continue;
                }

                if (TryApplyTyped(target, config)) {
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
                            intData.Int = config.Modify == ParamModifyType.Add ? intData.Int + config.IntValue : config.IntValue;
                        }

                        break;
                    case ParamValueType.Float:
                        if (Cond.Instance.TryGetData(target, config.ParamSign, out FloatData floatData)) {
                            floatData.Float = config.Modify == ParamModifyType.Add ? floatData.Float + config.FloatValue : config.FloatValue;
                        }

                        break;
                    case ParamValueType.String:
                        if (Cond.Instance.TryGetData(target, config.ParamSign, out StringData stringData)) {
                            stringData.String = config.StringValue;
                        }

                        break;
                    case ParamValueType.Vector3:
                        if (Cond.Instance.TryGetData(target, config.ParamSign, out Vector3Data vector3Data)) {
                            vector3Data.Vector3 = config.Modify == ParamModifyType.Add ? vector3Data.Vector3 + config.Vector3Value : config.Vector3Value;
                        }

                        break;
                    default:
                        LogUtil.LogErrorFormat("行为:{0} 不支持的参数类型:{1}", BehaviourSign, config.ValueType);
                        break;
                }
            }
        }
        /// <summary>
        /// 强类型快路 Health/Dead 直接写对方 DeathData 不经过通用Data
        /// </summary>
        private bool TryApplyTyped(Entity target, DeathData.ParamModifyConfig config) {
            if (!target.GetBehaviourData<DeathData>(out DeathData death)) {
                return false;
            }

            if (config.ParamSign == HEALTH_LABEL && config.ValueType == ParamValueType.Float) {
                death.Health = config.Modify == ParamModifyType.Add ? death.Health + config.FloatValue : config.FloatValue;
                return true;
            }

            if (config.ParamSign == DEAD_LABEL && config.ValueType == ParamValueType.Bool) {
                death.Dead = config.BoolValue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 绑定运行时数据 血量自带不再依赖ParamValue配Dead/Health
        /// </summary>
        private void BindRuntimeData() {
            hasHealthBar = _deathData.MaxHealth > 0f;
            if (hasHealthBar && _deathData.Health <= 0f) {
                _deathData.Health = _deathData.MaxHealth;
            }

            prevDead = _deathData.Dead;
        }

        private void OnUpdate() {
            //行为私有参数不同步到实体前，先把外部对实体的改动收拢：实体的血以注册表为准
            if (_health != null) {
                _deathData.Health = _health.Current;
                _deathData.MaxHealth = _health.Max;
                hasHealthBar = _health.HasBar;
            }
            if (hasHealthBar) {
                if (!_deathData.Dead && _deathData.Health <= 0f) {
                    SetDead();
                }

                //复活由外部直接把 Health 改回正数后自动恢复存活状态
                if (_deathData.Dead && _deathData.Health > 0f) {
                    _deathData.Dead = false;
                    prevDead = false;
                    deathDelayRemainTime = 0f;
                }
            } else if (_deathData.Dead && !prevDead) {
                //无血条模式 外部把 Dead 置 true 就是喊死 在这里统一走死亡结算
                prevDead = true;
                Die();
            } else if (!_deathData.Dead) {
                prevDead = false;
            }

            //死亡标记有效时进入延迟销毁计时
            if (_deathData.Dead && _config.DeathAction == DeathAction.DestroyEntity && _config.DeathDelay > 0f) {
                deathDelayRemainTime -= Time.deltaTime;
                if (deathDelayRemainTime <= 0f) {
                    Obj.Instance.UnLoadEntity(entity);
                }
            }
        }

        /// <summary>
        /// 复活本实体（只改自己的 DeathData；别的实体要复活它，直接改它的 Dead/Health 字段，别调这个）。
        /// </summary>
        public void Revive() {
            if (!_deathData.Dead) {
                return;
            }

            if (hasHealthBar && _deathData.Health <= 0f) {
                return;
            }

            _deathData.Dead = false;
            prevDead = false;
            deathDelayRemainTime = 0f;
        }

        /// <summary>
        /// Health 小于等于 0 时统一进入死亡状态。
        /// </summary>
        private void SetDead() {
            if (_deathData.Dead) {
                return;
            }

            _deathData.Dead = true;
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
            EntityAttrRegistry.Unregister(entity);
            DetachBehaviourData<DeathData>();
            base.Clear();
        }
    }
}
