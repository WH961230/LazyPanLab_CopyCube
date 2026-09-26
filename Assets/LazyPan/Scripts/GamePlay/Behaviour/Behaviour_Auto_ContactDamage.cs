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

            if (c.Damage <= 0f && (c.Hits == null || c.Hits.Count == 0)) {
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

            if (string.IsNullOrEmpty(c.TargetType) && (c.Hits == null || c.Hits.Count == 0)) {
                yellow.Add("目标类型没配，Data 里也没人写就不伤人，记得填比如 Player");
            }

            if (c.KnockbackDistance < 0f) {
                yellow.Add("击退距离是负数，会按不击退用");
            }

            if (!string.IsNullOrEmpty(c.TargetType) && string.IsNullOrEmpty(c.DamageToType) && c.TargetType != "Player") {
                yellow.Add("目标和记账不是一类：碰到扣的是被碰的那个的血，塔没血条会报错，想扣玩家的就把记账填 Player");
            }

            if (c.Hits != null && c.Hits.Count > 0) {
                if (!string.IsNullOrEmpty(c.TargetType) || c.Damage > 0f) {
                    yellow.Add("Hits 里有段，老字段就不参与了，上面看着有数其实没用，想用就搬进 Hits");
                }

                for (int i = 0; i < c.Hits.Count; i++) {
                    ContactHitItem item = c.Hits[i];
                    if (item == null) {
                        red.Add($"第{i + 1}段是空行，删掉");
                        continue;
                    }

                    if (item.Damage <= 0f) {
                        yellow.Add($"第{i + 1}段伤害<=0，这段不伤人");
                    }

                    if (string.IsNullOrEmpty(item.TargetType)) {
                        yellow.Add($"第{i + 1}段目标类型没配，这段不伤人");
                    }

                    if (item.HitCooldown < -1f) {
                        yellow.Add($"第{i + 1}段间隔<-1，会按只伤一次用");
                    }

                    if (item.MaxHits < 0) {
                        yellow.Add($"第{i + 1}段次数是负数，会按不限用");
                    }

                    if (item.KnockbackDistance < 0f) {
                        yellow.Add($"第{i + 1}段推人距离是负数，会按不推用");
                    }
                }
            }

            yellow.Add("跨实体提醒：挨打的一方必须有血条（挂死亡行为生产 Health），不然扣血没地方写，只会报错跳过；推人只写纸条，对方没挂击退就是不飞");
        }
        /// <summary>接触伤害节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【接触伤害】管一个东西碰到敌人扣血，Data 里有人写就用写的，没人写用 Setting 保底，不认识触发器和击退。\n" +
            "— 一物多打还是老单组（二选一）—\n" +
            "- <color=#FFD54F>Hits 空着</color>：老单组模式，只看上面老字段，打一类人\n" +
            "- <color=#FFD54F>Hits 有东西</color>：多段模式，老字段全部歇着，一段打一类人，各自独立\n" +
            "- <color=#FFD54F>Data 传话包</color>：写了就全场通用，两边都靠边\n" +
            "— 配置参数（ContactDamageSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>TargetType</color>：打哪类实体，如 Player，空=不伤人\n" +
            "- <color=#FFD54F>Damage</color>：碰一下扣多少血，0=睡觉不伤人\n" +
            "- <color=#FFD54F>DamageRadius</color>：多近算碰到，Data 有有效值先用 Data 的\n" +
            "- <color=#FFD54F>HitCooldown</color>：同一个敌人隔几秒才能再伤，-1=只伤一次\n" +
            "- <color=#FFD54F>MaxHits</color>：伤几个人后自己死，0=不限\n" +
            "- <color=#FFD54F>KnockbackDistance</color>：本次打击自带推人几米（旧名，实为打击属性），0=不推，对方没人看纸条就只扣血\n" +
            "- <color=#FFD54F>KnockbackDuration</color>：推人飞多久（秒），方向固定 B 减 A 压平指向 B\n" +
            "- <color=#FFD54F>Hits</color>：多段打击，一条打一类人，各自独立，空=用上面老单组";
        /// <summary>
        /// 对外契约 打击数值优先读 Data 传话包 缺失回退 Setting 保底
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

        //runtime 一物多打：一条打一类人，各自独立冷却表和计数；Data 传话包是全局覆盖，对所有条目的同名字段生效
        private readonly List<HitRuntime> _hits = new List<HitRuntime>();
        private bool isConfigValid;

        public Behaviour_Auto_ContactDamage(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            ContactDamageSetting setting = Loader.LoadAsset<ContactDamageSetting>(AssetType.ASSET, "Setting/ContactDamageSetting");
            if (setting != null && setting.TryGet(entity.ObjConfig.Sign, out ContactDamageSettingData data)) {
                if (data.Hits != null && data.Hits.Count > 0) {
                    foreach (ContactHitItem item in data.Hits) {
                        if (item == null) {
                            continue;
                        }

                        _hits.Add(HitRuntime.FromItem(item));
                    }
                }

                //老单组兼容：多段为空时用上面老字段拼一条，老存档不用动
                if (_hits.Count == 0) {
                    _hits.Add(new HitRuntime() {
                        TargetType = data.TargetType,
                        Damage = data.Damage,
                        Radius = data.DamageRadius,
                        Cooldown = data.HitCooldown,
                        MaxHits = data.MaxHits,
                        PushDistance = Mathf.Max(data.KnockbackDistance, 0f),
                        PushDuration = Mathf.Max(data.KnockbackDuration, 0.01f),
                        DamageToType = data.DamageToType?.Trim(),
                    });
                }
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

            Cond.Instance.PeekData(entity, "Damage", out FloatData damage);
            Cond.Instance.PeekData(entity, "DamageRadius", out FloatData damageRadius);
            Cond.Instance.PeekData(entity, "HitCooldown", out FloatData hitCooldown);
            Cond.Instance.PeekData(entity, "MaxHits", out IntData maxHits);
            Cond.Instance.PeekData(entity, DataLabels.TargetType, out StringData targetType);
            // 行为自己兜底：Data 缺失或遗留 0 值时回退 Setting，再回退契约默认，不再被自动创建的 0 盖死
            // Data 是全局覆盖：写了就对所有条目的同名字段生效，没写各条目用自己的 Setting
            string dataType = targetType != null ? targetType.String : null;

            Vector3 selfPos = MoveRoot().position;
            foreach (HitRuntime hit in _hits) {
                if (!isConfigValid) {
                    return;
                }

                ApplyHit(hit, selfPos, damage, damageRadius, hitCooldown, maxHits, dataType);
            }
        }

        /// <summary>
        /// 单条打击：解算自己这条的数值（Data 全局覆盖优先），量尺子，扣血写纸条，次数打满自己死
        /// </summary>
        private void ApplyHit(HitRuntime hit, Vector3 selfPos, FloatData damage, FloatData damageRadius, FloatData hitCooldown, IntData maxHits, string dataType) {
            float amount = damage != null ? damage.Float : 0f;
            if (amount <= 0f) {
                amount = hit.Damage > 0f ? hit.Damage : RequiredPayload[0].FloatDefault;
            }
            if (amount <= 0f) {
                return;
            }

            string type = !string.IsNullOrEmpty(dataType) ? dataType : hit.TargetType;
            if (string.IsNullOrEmpty(type)) {
                return;
            }

            float hitR = damageRadius != null && damageRadius.Float > 0f ? Mathf.Max(damageRadius.Float, 0.1f)
                : (hit.Radius > 0f ? Mathf.Max(hit.Radius, 0.1f) : RequiredPayload[1].FloatDefault);
            float cooldown = hitCooldown != null ? hitCooldown.Float : hit.Cooldown;
            int max = maxHits != null ? Mathf.Max(maxHits.Int, 0) : Mathf.Max(hit.MaxHits, 0);

            if (!EntityRegister.TryGetEntitiesWithinDistance(type, selfPos, hitR, out List<Entity> touched)) {
                return;
            }

            foreach (Entity candidate in touched) {
                if (candidate == null || !isConfigValid) {
                    continue;
                }

                if (cooldown < 0f && hit.HurtTime.ContainsKey(candidate.ID)) {
                    continue;
                }

                if (cooldown >= 0f && hit.HurtTime.TryGetValue(candidate.ID, out float last) && Time.time - last < cooldown) {
                    continue;
                }

                hit.HurtTime[candidate.ID] = Time.time;
                //实体级消费：只找注册表，不碰死亡行为的数据类型；找不到说明对方没挂死亡，直接报错跳过
                //记账：DamageToType 填了就碰到算命中、血记在该类型账上（如蹭到塔扣玩家的血），找不到记账方就记被碰的那个
                Entity victim = candidate;
                if (!string.IsNullOrEmpty(hit.DamageToType)
                    && EntityRegister.TryGetEntitiesByType(hit.DamageToType, out List<Entity> owners)
                    && owners != null && owners.Count > 0) {
                    victim = owners[0];
                }

                if (EntityAttrRegistry.TryGetHealth(victim, out HealthAttr attr)) {
                    attr.Damage(amount);
                } else {
                    LogUtil.LogErrorFormat("行为:{0} 记账:{1} 未注册Health，请给它挂死亡行为", BehaviourSign, victim.ObjConfig?.Sign);
                }

                //命中推人：只往对方身上写纸条，不认识击退行为类，对方没挂击退就是纸条没人看，只扣血不飞
                //方向固定为 AB 向量指向 B：B 身体位置减 A 身体位置，压平 y，保证谁撞谁都是把对方往外推
                if (hit.PushDistance > 0f) {
                    Vector3 targetPos = candidate.Comp != null ? candidate.Comp.transform.position : selfPos;
                    Transform targetBody = Cond.Instance.Get<Transform>(candidate, Label.BODY);
                    if (targetBody != null) {
                        targetPos = targetBody.position;
                    }

                    Vector3 pushDir = targetPos - selfPos;
                    pushDir.y = 0f;
                    if (pushDir.sqrMagnitude < 0.0001f) {
                        pushDir = entity.Comp.transform.forward;
                        pushDir.y = 0f;
                    }

                    WriteKnockbackIntent(candidate, pushDir.normalized, hit.PushDistance, hit.PushDuration);
                }

                hit.HitCount++;
                if (max > 0 && hit.HitCount >= max) {
                    KillSelf();

                    if (Game.instance != null) {
                        Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
                    }
                    isConfigValid = false;
                    return;
                }
            }
        }

        /// <summary>
        /// 单条打击的运行时：数值是自己的，冷却表和计数也是自己的，条条独立
        /// </summary>
        private class HitRuntime {
            public string TargetType;
            public float Damage;
            public float Radius;
            public float Cooldown = -1f;
            public int MaxHits;
            public float PushDistance;
            public float PushDuration = 0.3f;
            //伤害记账：空=扣被碰到的那个，填了类型=碰到算命中，血记在该类型账上（如蹭到塔扣玩家的血）
            public string DamageToType;
            public Dictionary<int, float> HurtTime = new Dictionary<int, float>();
            public int HitCount;

            public static HitRuntime FromItem(ContactHitItem item) {
                return new HitRuntime() {
                    TargetType = item.TargetType,
                    Damage = item.Damage,
                    Radius = item.DamageRadius,
                    Cooldown = item.HitCooldown,
                    MaxHits = item.MaxHits,
                    PushDistance = Mathf.Max(item.KnockbackDistance, 0f),
                    PushDuration = Mathf.Max(item.KnockbackDuration, 0.01f),
                    DamageToType = item.DamageToType?.Trim(),
                };
            }
        }

        private Transform MoveRoot() {
            Transform body = Cond.Instance.Get<Transform>(entity, Label.BODY);
            if (body != null) {
                return body;
            }

            return entity.Comp.transform;
        }

        /// <summary>
        /// 次数打满自己死：有血条走注册表扣光，无血条写死亡纸条，全程不碰死亡行为的数据类型
        /// </summary>
        private void KillSelf() {
            if (EntityAttrRegistry.TryGetHealth(entity, out HealthAttr selfAttr) && selfAttr.HasBar && selfAttr.Current > 0f) {
                selfAttr.Damage(selfAttr.Current);
            }

            WriteBool(entity, DataLabels.Dead, true);
        }

        /// <summary>
        /// 往受害者身上写击退纸条：一帧多人命中后写盖先写，击退看到序号变化飞最新的一次
        /// </summary>
        private static void WriteKnockbackIntent(Entity target, Vector3 dir, float distance, float duration) {
            if (target == null || target.Data == null || distance <= 0f) {
                return;
            }

            Vector3 flat = dir;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) {
                return;
            }

            WriteVector3(target, DataLabels.KnockbackDir, flat.normalized);
            WriteFloat(target, DataLabels.KnockbackDistance, distance);
            WriteFloat(target, DataLabels.KnockbackDuration, duration);
            int seq = 0;
            if (Cond.Instance.PeekData(target, DataLabels.KnockbackSeq, out IntData seqData) && seqData != null) {
                seq = seqData.Int;
            }

            WriteInt(target, DataLabels.KnockbackSeq, seq + 1);
        }

        private static void WriteFloat(Entity target, string sign, float value) {
            if (!Cond.Instance.PeekData(target, sign, out FloatData data) || data == null) {
                target.Data.Add<FloatData>(sign, sign);
                Cond.Instance.PeekData(target, sign, out data);
            }

            if (data != null) {
                data.Float = value;
            }
        }

        private static void WriteInt(Entity target, string sign, int value) {
            if (!Cond.Instance.PeekData(target, sign, out IntData data) || data == null) {
                target.Data.Add<IntData>(sign, sign);
                Cond.Instance.PeekData(target, sign, out data);
            }

            if (data != null) {
                data.Int = value;
            }
        }

        private static void WriteVector3(Entity target, string sign, Vector3 value) {
            if (!Cond.Instance.PeekData(target, sign, out Vector3Data data) || data == null) {
                target.Data.Add<Vector3Data>(sign, sign);
                Cond.Instance.PeekData(target, sign, out data);
            }

            if (data != null) {
                data.Vector3 = value;
            }
        }

        private static void WriteBool(Entity target, string sign, bool value) {
            if (!Cond.Instance.PeekData(target, sign, out BoolData data) || data == null) {
                target.Data.Add<BoolData>(sign, sign);
                Cond.Instance.PeekData(target, sign, out data);
            }

            if (data != null) {
                data.Bool = value;
            }
        }

        public override void Clear() {
            if (Game.instance != null) {
                Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            }
            base.Clear();
        }
    }
}
