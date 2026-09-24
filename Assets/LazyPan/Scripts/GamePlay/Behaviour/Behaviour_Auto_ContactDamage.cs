using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 零件 - 接触伤害
    /// 只干一件事: 半径内同类敌人扣血 同目标按 HitCooldown 间隔再伤(-1=只伤一次) 命中数到 MaxHits 自己死(0=不限)
    /// 没 Damage 睡觉 不认识子弹圆环 全经过 Data 扣血走数据层
    /// </summary>
    public class Behaviour_Auto_ContactDamage : Behaviour {
        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is ContactDamageSettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (c.Damage <= 0f) {
                yellow.Add("伤害<=0，碰到也不扣血");
            }

            if (c.DamageRadius < 0f) {
                yellow.Add("半径是负数，会按 0 用");
            }

            if (c.HitCooldown < -1f) {
                yellow.Add("间隔<-1，会按只伤一次用");
            }

            if (c.MaxHits < 0) {
                yellow.Add("次数是负数，会按不限用");
            }

            yellow.Add("跨实体提醒：挨打的一方必须挂死亡行为，不然扣血没地方写");
        }
        /// <summary>接触伤害节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【接触伤害】管一个东西碰到敌人扣血，碰到谁由触发器定，不用你配。\n" +
            "— 配置参数（ContactDamageSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>Damage</color>：碰一下扣多少血，0=睡觉不伤人\n" +
            "- <color=#FFD54F>DamageRadius</color>：多近算碰到，0=跟别人每帧写的那个走\n" +
            "- <color=#FFD54F>HitCooldown</color>：同一个敌人隔几秒才能再伤，-1=只伤一次\n" +
            "- <color=#FFD54F>MaxHits</color>：伤几个人后自己死，0=不限";
        /// <summary>
        /// 对外契约 TargetType 由触发器交接不用配
        /// </summary>
        public static readonly PayloadContractDef[] RequiredPayload = {
            new PayloadContractDef() { Sign = "Damage", ValueType = ParamValueType.Float, FloatDefault = 10f },
            new PayloadContractDef() { Sign = "DamageRadius", ValueType = ParamValueType.Float, FloatDefault = 0.5f },
            new PayloadContractDef() { Sign = "HitCooldown", ValueType = ParamValueType.Float, FloatDefault = -1f },
            new PayloadContractDef() { Sign = "MaxHits", ValueType = ParamValueType.Int },
        };

        /// <summary>
        /// 模块依赖 命中数满散场走死亡 无血条分支
        /// </summary>
        public static readonly string[] RequiredModules = { "死亡" };

        //runtime
        private Dictionary<int, float> hurtTime = new Dictionary<int, float>();
        private int hitCount;
        private float settingDamage;
        private float settingRadius;
        private float settingCooldown = -1f;
        private int settingMaxHits;
        private bool isConfigValid;

        public Behaviour_Auto_ContactDamage(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            ContactDamageSetting setting = Loader.LoadAsset<ContactDamageSetting>(AssetType.ASSET, "Setting/ContactDamageSetting");
            if (setting != null && setting.TryGet(entity.ObjConfig.Sign, out ContactDamageSettingData data)) {
                settingDamage = data.Damage;
                settingRadius = data.DamageRadius;
                settingCooldown = data.HitCooldown;
                settingMaxHits = data.MaxHits;
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

            Cond.Instance.TryGetData(entity, "Damage", out FloatData damage);
            Cond.Instance.TryGetData(entity, "DamageRadius", out FloatData damageRadius);
            Cond.Instance.TryGetData(entity, "HitCooldown", out FloatData hitCooldown);
            Cond.Instance.TryGetData(entity, "MaxHits", out IntData maxHits);
            Cond.Instance.TryGetData(entity, DataLabels.TargetType, out StringData targetType);
            float amount = damage != null ? damage.Float : settingDamage;
            if (amount <= 0f) {
                return;
            }

            string type = targetType != null ? targetType.String : "";
            if (string.IsNullOrEmpty(type)) {
                return;
            }

            float hitR = damageRadius != null ? Mathf.Max(damageRadius.Float, 0.1f)
                : (settingRadius > 0f ? Mathf.Max(settingRadius, 0.1f) : 0.5f);
            float cooldown = hitCooldown != null ? hitCooldown.Float : settingCooldown;
            int max = maxHits != null ? Mathf.Max(maxHits.Int, 0) : Mathf.Max(settingMaxHits, 0);

            Vector3 selfPos = MoveRoot().position;
            if (!EntityRegister.TryGetEntitiesWithinDistance(type, selfPos, hitR, out List<Entity> touched)) {
                return;
            }

            foreach (Entity candidate in touched) {
                if (candidate == null || !isConfigValid) {
                    continue;
                }

                if (cooldown < 0f && hurtTime.ContainsKey(candidate.ID)) {
                    continue;
                }

                if (cooldown >= 0f && hurtTime.TryGetValue(candidate.ID, out float last) && Time.time - last < cooldown) {
                    continue;
                }

                hurtTime[candidate.ID] = Time.time;
                //实体级消费：只找注册表，不自建；找不到说明对方没挂死亡，直接报错跳过
                if (EntityAttrRegistry.TryGetHealth(candidate, out HealthAttr attr)) {
                    attr.Damage(amount);
                } else if (candidate.GetBehaviourData<DeathData>(out DeathData targetDeath)) {
                    targetDeath.Damage(amount);
                } else {
                    LogUtil.LogErrorFormat("行为:{0} 目标:{1} 未注册Health，请给它挂死亡行为", BehaviourSign, candidate.ObjConfig?.Sign);
                }

                hitCount++;
                if (max > 0 && hitCount >= max) {
                    if (entity.GetBehaviourData<DeathData>(out DeathData selfDeath)) {
                        selfDeath.Dead = true;
                    } else if (Cond.Instance.TryGetData(entity, DataLabels.Dead, out BoolData dead)) {
                        dead.Bool = true;
                    }

                    if (Game.instance != null) {
                        Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
                    }
                    isConfigValid = false;
                    return;
                }
            }
        }

        private Transform MoveRoot() {
            Transform body = Cond.Instance.Get<Transform>(entity, Label.BODY);
            if (body != null) {
                return body;
            }

            return entity.Comp.transform;
        }

        public override void Clear() {
            if (Game.instance != null) {
                Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            }
            base.Clear();
        }
    }
}
