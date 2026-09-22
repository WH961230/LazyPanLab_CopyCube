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
        private const string settingPath = "Setting/WeaponSetting";
        private const string currentWeaponSign = DataLabels.CurrentWeapon;

        //config
        private WeaponFireData _fireData;
        private WeaponFireData.WeaponFireConfig _config;
        private List<WeaponItem> _arsenal = new List<WeaponItem>();

        //runtime
        private bool isConfigValid;
        private string _orbitWeaponID = "";
        private List<int> _orbitBallIDs = new List<int>();

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
                ClearOrbitBalls();
                return;
            }

            //换枪了 旧环绕球散场(只认 ID 不调球的方法)
            if (weapon.WeaponID != _orbitWeaponID) {
                ClearOrbitBalls();
            }

            if (weapon.Kind == WeaponKind.Orbit) {
                UpdateOrbit(weapon);
                return;
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
        /// 环绕 球不够补 球多了(配置改少了)散多余的 球认 HolderID 自己围转 开火只管数
        /// </summary>
        private void UpdateOrbit(WeaponItem weapon) {
            if (!BehaviourSigns.Require(weapon.SpawnSign, BehaviourSign, entity.ObjConfig?.Sign, nameof(WeaponItem.SpawnSign))) {
                return;
            }

            for (int i = _orbitBallIDs.Count - 1; i >= 0; i--) {
                if (!EntityRegister.TryGetEntityByID(_orbitBallIDs[i], out Entity ball) || ball == null) {
                    _orbitBallIDs.RemoveAt(i);
                }
            }

            int want = 1;
            if (weapon.Payload != null) {
                foreach (WeaponPayloadItem item in weapon.Payload) {
                    if (item != null && item.ParamSign == "OrbitCount" && item.ValueType == ParamValueType.Int) {
                        want = Mathf.Max(item.IntValue, 1);
                        break;
                    }
                }
            }

            while (_orbitBallIDs.Count > want) {
                DismissBall(_orbitBallIDs[_orbitBallIDs.Count - 1]);
                _orbitBallIDs.RemoveAt(_orbitBallIDs.Count - 1);
            }

            while (_orbitBallIDs.Count < want) {
                Entity ball = Obj.Instance.LoadEntity(weapon.SpawnSign);
                if (ball == null) {
                    LogUtil.LogErrorFormat("行为:{0} 环绕球生成失败:{1}", BehaviourSign, weapon.SpawnSign);
                    return;
                }

                _orbitBallIDs.Add(ball.ID);
                _orbitWeaponID = weapon.WeaponID;

                if (Cond.Instance.TryGetData(ball, DataLabels.HolderID, out IntData holderID)) {
                    holderID.Int = entity.ID;
                }

                if (Cond.Instance.TryGetData(ball, DataLabels.TargetType, out StringData targetType)) {
                    targetType.String = weapon.TargetType;
                }

                if (Cond.Instance.TryGetData(ball, "OrbitAngle", out FloatData angle)) {
                    angle.Float = 360f * _orbitBallIDs.Count / Mathf.Max(want, 1);
                }

                if (weapon.Payload != null) {
                    foreach (WeaponPayloadItem item in weapon.Payload) {
                        if (item == null || string.IsNullOrEmpty(item.ParamSign)) {
                            continue;
                        }

                        WritePayload(ball, item);
                    }
                }
            }

            _orbitWeaponID = weapon.WeaponID;
        }

        /// <summary>
        /// 散场 只改球的 Dead 不调球的方法 球的环绕行为自己会停
        /// </summary>
        private void ClearOrbitBalls() {
            foreach (int id in _orbitBallIDs) {
                DismissBall(id);
            }

            _orbitBallIDs.Clear();
            _orbitWeaponID = "";
        }

        private void DismissBall(int id) {
            if (EntityRegister.TryGetEntityByID(id, out Entity ball) && ball != null) {
                if (Cond.Instance.TryGetData(ball, DataLabels.Dead, out BoolData dead)) {
                    dead.Bool = true;
                }
            }
        }

        /// <summary>
        /// 当前武器 Data 里 CurrentWeapon 优先 没有才用默认枪 换枪=改一个字符串 开火原地不动
        /// </summary>
        private WeaponItem CurrentWeapon() {
            string weaponID = _config.DefaultWeaponID;
            if (Cond.Instance.GetData<StringData>(entity, currentWeaponSign, out StringData current)
                && !string.IsNullOrEmpty(current.String)) {
                weaponID = current.String;
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
                    WriteBool(spawned, item.ParamSign, item.BoolValue);
                    break;
                case ParamValueType.Int:
                    WriteInt(spawned, item.ParamSign, item.IntValue);
                    break;
                case ParamValueType.Float:
                    WriteFloat(spawned, item.ParamSign, item.FloatValue);
                    break;
                case ParamValueType.String:
                    WriteString(spawned, item.ParamSign, item.StringValue);
                    break;
                case ParamValueType.Vector3:
                    if (Cond.Instance.TryGetData(spawned, item.ParamSign, out Vector3Data vector3Data)) {
                        vector3Data.Vector3 = item.Vector3Value;
                    } else {
                        LogUtil.LogErrorFormat("行为:{0} 生成物:{1} 缺少 Vector3 参数:{2}!", BehaviourSign, spawned.ObjConfig.Sign, item.ParamSign);
                    }

                    break;
                default:
                    LogUtil.LogErrorFormat("行为:{0} 不支持的参数类型:{1}", BehaviourSign, item.ValueType);
                    break;
            }
        }

        private void WriteInt(Entity spawned, string sign, int value) {
            if (Cond.Instance.TryGetData(spawned, sign, out IntData intData)) {
                intData.Int = value;
                return;
            }

            LogUtil.LogErrorFormat("行为:{0} 生成物:{1} 缺少 Int 参数:{2}!", BehaviourSign, spawned.ObjConfig.Sign, sign);
        }

        private void WriteBool(Entity spawned, string sign, bool value) {
            if (Cond.Instance.TryGetData(spawned, sign, out BoolData boolData)) {
                boolData.Bool = value;
                return;
            }

            LogUtil.LogErrorFormat("行为:{0} 生成物:{1} 缺少 Bool 参数:{2}!", BehaviourSign, spawned.ObjConfig.Sign, sign);
        }

        private void WriteFloat(Entity spawned, string sign, float value) {
            if (Cond.Instance.TryGetData(spawned, sign, out FloatData floatData)) {
                floatData.Float = value;
                return;
            }

            LogUtil.LogErrorFormat("行为:{0} 生成物:{1} 缺少 Float 参数:{2}!", BehaviourSign, spawned.ObjConfig.Sign, sign);
        }

        private void WriteString(Entity spawned, string sign, string value) {
            if (Cond.Instance.TryGetData(spawned, sign, out StringData stringData)) {
                stringData.String = value;
                return;
            }

            LogUtil.LogErrorFormat("行为:{0} 生成物:{1} 缺少 String 参数:{2}!", BehaviourSign, spawned.ObjConfig.Sign, sign);
        }

        public override void Clear() {
            ClearOrbitBalls();
            if (Game.instance != null) {
                Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            }
            DetachBehaviourData<WeaponFireData>();
            base.Clear();
        }
    }
}
