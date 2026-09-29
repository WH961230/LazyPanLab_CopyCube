using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 开火(通用触发器)
    /// 只干四件事: 掐表 看圈 生实体 传话 不认识子弹圆环 不调任何行为 全经过 Data 传话
    /// 当前武器=持有者 Data 的 CurrentWeapon 字符串(武器管理器换枪就改它) 没有才用默认枪
    /// 名单=框架现成的按类型距离扫 生几个+散布角通用 传话包照单写进新生实体 Data
    /// 配置来源 Setting/WeaponSetting 一个持有者一条 里面是军火库
    /// </summary>
    public class Behaviour_Event_WeaponFire : Behaviour {
        /// <summary>武器开火节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【武器开火】管一把枪怎么打，军火库里一枪一行。\n" +
            "— 配置参数（WeaponSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>DefaultWeaponID</color>：开局拿哪把\n" +
            "- <color=#FFD54F>Weapons</color>：军火库，一枪一行\n" +
            "— 每把枪怎么填 —\n" +
            "- <color=#FFD54F>WeaponID</color>/<color=#FFD54F>WeaponName</color>：枪的编号和名字，编号全局唯一\n" +
            "- <color=#FFD54F>Kind</color>：直射=点射，环绕=围着转，范围=生产范围体\n" +
            "- <color=#FFD54F>TargetType</color>：打谁，索敌类型必填\n" +
            "- <color=#FFD54F>Range</color>/<color=#FFD54F>Interval</color>：射程和开火间隔秒数\n" +
            "- <color=#FFD54F>SpawnSign</color>：打出什么，直射范围填，环绕不填\n" +
            "- <color=#FFD54F>SpawnCount</color>/<color=#FFD54F>SpreadAngle</color>：一次打几个，散布总角度，0=无散布\n" +
            "- <color=#FFD54F>UseFireCondition</color>：勾上才看开火条件，不勾一直打\n" +
            "- <color=#FFD54F>ConditionParam</color>：看自己哪个数，如Energy\n" +
            "- <color=#FFD54F>ConditionCompare</color>：0大于 1大于等于 2等于 3小于等于 4小于 5不等\n" +
            "- <color=#FFD54F>ConditionValue</color>：和多少比，如50\n" +
            "- <color=#FFD54F>Payload</color>：传话包，写进打出东西的注册表里，ParamSign=哪个数，对着类型填值";
        private const string settingPath = "Setting/WeaponSetting";

        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is WeaponSettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (c.Weapons == null || c.Weapons.Count == 0) {
                red.Add("军火库是空的，没枪可拿");
                return;
            }

            if (string.IsNullOrEmpty(c.DefaultWeaponID)) {
                yellow.Add("没配默认枪，开局拿不到枪");
            } else if (!c.Weapons.Exists(w => w != null && w.WeaponID == c.DefaultWeaponID)) {
                red.Add($"默认枪 {c.DefaultWeaponID} 不在军火库里");
            }

            var seen = new HashSet<string>();
            foreach (var w in c.Weapons) {
                if (w == null || string.IsNullOrEmpty(w.WeaponID)) {
                    red.Add("有把枪没填编号");
                    continue;
                }

                if (!seen.Add(w.WeaponID)) {
                    red.Add($"枪 {w.WeaponID} 编号重复");
                }

                if (string.IsNullOrEmpty(w.TargetType)) {
                    red.Add($"枪 {w.WeaponID} 没填打谁，不会开火");
                }

                if (w.Interval <= 0f) {
                    yellow.Add($"枪 {w.WeaponID} 间隔<=0，会每帧打");
                }

                if (w.Kind != WeaponKind.Orbit && string.IsNullOrEmpty(w.SpawnSign)) {
                    yellow.Add($"枪 {w.WeaponID} 没配生成物，打出去没东西（环绕类不用配）");
                }

                if (w.SpawnCount <= 0) {
                    yellow.Add($"枪 {w.WeaponID} 一次生成<=0，打出去没东西");
                }
            }
        }
        private const string currentWeaponSign = DataLabels.CurrentWeapon;

        //config
        private WeaponFireData _fireData;
        private WeaponFireData.WeaponFireConfig _config;
        private List<WeaponItem> _arsenal = new List<WeaponItem>();

        //runtime
        private bool isConfigValid;

        public Behaviour_Event_WeaponFire(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _fireData = AttachBehaviourData<WeaponFireData>();

            WeaponSetting setting = Loader.LoadAsset<WeaponSetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out WeaponSettingData settingData)) {
                return;
            }

            _config = _fireData.Config;
            _config.DefaultWeaponID = settingData.DefaultWeaponID;
            _config.TargetType = "";
            _config.Cooldown = 0f;
            _arsenal.Clear();
            if (settingData.Weapons != null) {
                foreach (WeaponItem weapon in settingData.Weapons) {
                    if (weapon == null || string.IsNullOrEmpty(weapon.WeaponID)) {
                        continue;
                    }

                    _arsenal.Add(weapon);
                }
            }

            if (_arsenal.Count == 0) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 军火库为空!", BehaviourSign, entity.ObjConfig.Sign);
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

            WeaponItem weapon = CurrentWeapon();
            if (weapon == null) {
                return;
            }

            //环绕枪搬去养球行为，这里只打点射和范围
            if (weapon.Kind == WeaponKind.Orbit) {
                return;
            }

            //开火条件：勾了开关才看，看自己注册表里的数，不满足直接歇着
            if (weapon.UseFireCondition && !string.IsNullOrEmpty(weapon.ConditionParam)) {
                EntityAttrRegistry.TryGetNumber(entity, weapon.ConditionParam, out float curVal);
                if (!CompareFireCondition(curVal, weapon.ConditionValue, weapon.ConditionCompare)) {
                    return;
                }
            }

            if (!BehaviourSigns.Require(weapon.TargetType, BehaviourSign, entity.ObjConfig?.Sign, nameof(WeaponItem.TargetType))) {
                return;
            }

            _config.Cooldown -= Time.deltaTime;
            if (_config.Cooldown > 0f) {
                return;
            }

            if (!TryFindTarget(weapon, out Entity target)) {
                return;
            }

            Fire(weapon, target);
            _config.Cooldown = Mathf.Max(weapon.Interval, 0.01f);
        }

        /// <summary>
        /// 开火条件比较 注册表左边数 vs 右边常量
        /// </summary>
        private bool CompareFireCondition(float left, float right, TeleportCompare compare) {
            switch (compare) {
                case TeleportCompare.Greater: return left > right;
                case TeleportCompare.GreaterEqual: return left >= right;
                case TeleportCompare.Equal: return Mathf.Abs(left - right) < 0.001f;
                case TeleportCompare.LessEqual: return left <= right;
                case TeleportCompare.Less: return left < right;
                case TeleportCompare.NotEqual: return Mathf.Abs(left - right) >= 0.001f;
                default: return left >= right;
            }
        }

        /// <summary>
        /// 当前武器 注册表 CurrentWeapon 优先 没有才用默认枪 换枪=改一个字符串 开火原地不动
        /// </summary>
        private WeaponItem CurrentWeapon() {
            string weaponID = _config.DefaultWeaponID;
            if (EntityAttrRegistry.TryGetText(entity, currentWeaponSign, out string currentID)
                && !string.IsNullOrEmpty(currentID)) {
                weaponID = currentID;
            }

            foreach (WeaponItem weapon in _arsenal) {
                if (weapon.WeaponID == weaponID) {
                    return weapon;
                }
            }

            return null;
        }

        /// <summary>
        /// 索敌 框架现成的按类型距离扫 挑最近的 只伤锁定那一个
        /// </summary>
        private bool TryFindTarget(WeaponItem weapon, out Entity target) {
            target = null;
            Vector3 from = HolderPosition();
            if (!EntityRegister.TryGetEntitiesWithinDistance(weapon.TargetType, from, weapon.Range, out List<Entity> candidates)) {
                return false;
            }

            float nearest = float.MaxValue;
            foreach (Entity candidate in candidates) {
                if (candidate == null || candidate == entity) {
                    continue;
                }

                Transform body = Cond.Instance.Get<Transform>(candidate, Label.BODY);
                Vector3 pos = body != null ? body.position : candidate.Comp.transform.position;
                float dist = (pos - from).sqrMagnitude;
                if (dist < nearest) {
                    nearest = dist;
                    target = candidate;
                }
            }

            return target != null;
        }

        private Vector3 HolderPosition() {
            Transform body = Cond.Instance.Get<Transform>(entity, Label.BODY);
            if (body != null) {
                return body.position;
            }

            return entity.Comp.transform.position;
        }

        /// <summary>
        /// 下单 生 N 个实体 扇形散布 定到枪口 写交接(TargetID/TargetType)+传话包 不调生成物的任何方法
        /// </summary>
        private void Fire(WeaponItem weapon, Entity target) {
            if (!BehaviourSigns.Require(weapon.SpawnSign, BehaviourSign, entity.ObjConfig?.Sign, nameof(WeaponItem.SpawnSign))) {
                return;
            }

            int count = Mathf.Max(weapon.SpawnCount, 1);
            for (int i = 0; i < count; i++) {
                float angle = count <= 1 ? 0f : -weapon.SpreadAngle / 2f + weapon.SpreadAngle * i / (count - 1);
                SpawnOne(weapon, target, angle);
            }
        }

        private void SpawnOne(WeaponItem weapon, Entity target, float angleOffset) {
            Entity spawned = Obj.Instance.LoadEntity(weapon.SpawnSign);
            if (spawned == null) {
                LogUtil.LogErrorFormat("行为:{0} 生成实体失败:{1}", BehaviourSign, weapon.SpawnSign);
                return;
            }

            Transform root = Cond.Instance.Get<Transform>(spawned, Label.BODY);
            if (root == null) {
                root = spawned.Comp.transform;
            }
            root.position = HolderPosition();
            if (!Mathf.Approximately(angleOffset, 0f)) {
                root.rotation = Quaternion.Euler(0f, angleOffset, 0f) * root.rotation;
            }

            //交接 触发器只给这两样 打谁+打哪类 其余全看传话包
            WriteInt(spawned, DataLabels.TargetID, target.ID);
            WriteString(spawned, DataLabels.TargetType, weapon.TargetType);

            if (weapon.Payload != null) {
                foreach (WeaponPayloadItem item in weapon.Payload) {
                    if (item == null || string.IsNullOrEmpty(item.ParamSign)) {
                        continue;
                    }

                    WritePayload(spawned, item);
                }
            }
        }

        private void WritePayload(Entity spawned, WeaponPayloadItem item) {
            switch (item.ValueType) {
                case ParamValueType.Bool:
                    EntityAttrRegistry.SetBool(spawned, item.ParamSign, item.BoolValue);
                    break;
                case ParamValueType.Int:
                    EntityAttrRegistry.SetNumber(spawned, item.ParamSign, item.IntValue);
                    break;
                case ParamValueType.Float:
                    EntityAttrRegistry.SetNumber(spawned, item.ParamSign, item.FloatValue);
                    break;
                case ParamValueType.String:
                    EntityAttrRegistry.SetText(spawned, item.ParamSign, item.StringValue ?? "");
                    break;
                case ParamValueType.Vector3:
                    EntityAttrRegistry.SetVector(spawned, item.ParamSign, item.Vector3Value);
                    break;
            }
        }

        private void WriteInt(Entity spawned, string sign, int value) {
            EntityAttrRegistry.SetNumber(spawned, sign, value);
        }

        private void WriteBool(Entity spawned, string sign, bool value) {
            EntityAttrRegistry.SetBool(spawned, sign, value);
        }

        private void WriteFloat(Entity spawned, string sign, float value) {
            EntityAttrRegistry.SetNumber(spawned, sign, value);
        }

        private void WriteString(Entity spawned, string sign, string value) {
            EntityAttrRegistry.SetText(spawned, sign, value ?? "");
        }

        public override void Clear() {
            if (Game.instance != null) {
                Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            }
            DetachBehaviourData<WeaponFireData>();
            base.Clear();
        }
    }
}
