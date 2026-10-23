using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 环绕养球
    /// 只干一件事: 把军火库里 Kind=环绕的那把枪养起来 球不够补 球多了散 不掐表不索敌
    /// 配置复用 Setting/WeaponSetting 一个持有者一条 只看环绕行 点射范围不管
    /// 球的交接走注册表 不调球的任何方法
    /// </summary>
    public class Behaviour_Event_OrbitKeeper : Behaviour {
        public static readonly string MemoDoc =
            "【环绕养球】管常驻转圈的球，球不够补，球多了散，不掐表不索敌。\n" +
            "— 配置参数（复用 WeaponSetting 里按 SourceSign 配，只看 Kind=环绕的行）—\n" +
            "- <color=#FFD54F>WeaponID</color>/<color=#FFD54F>WeaponName</color>：养哪把，默认枪是环绕就养默认，否则养第一把环绕\n" +
            "- <color=#FFD54F>TargetType</color>：球打谁，写进球的交接\n" +
            "- <color=#FFD54F>SpawnSign</color>：养什么球，如环绕球\n" +
            "- <color=#FFD54F>UseFireCondition</color>：勾上才看条件，不满足就把球散掉\n" +
            "- <color=#FFD54F>ConditionParam</color>：看自己哪个数，如Energy\n" +
            "- <color=#FFD54F>ConditionCompare</color>：0大于 1大于等于 2等于 3小于等于 4小于 5不等\n" +
            "- <color=#FFD54F>ConditionValue</color>：和多少比，如0\n" +
            "- <color=#FFD54F>Payload</color>：OrbitCount=几个球 OrbitRadius=多远 OrbitSpeed=多快 OrbitDamage";
        private const string settingPath = "Setting/OrbitKeeperSetting";

        /// <summary>
        /// 对外契约 与开火一致 球认的词一样（一键补齐照这份填）
        /// </summary>
        public static readonly PayloadContractDef[] RequiredPayload = {
            new PayloadContractDef() { Sign = "Damage", ValueType = DataValueType.Float, FloatDefault = 10f },
            new PayloadContractDef() { Sign = "DamageRadius", ValueType = DataValueType.Float, FloatDefault = 0.5f },
            new PayloadContractDef() { Sign = "HitCooldown", ValueType = DataValueType.Float, FloatDefault = -1f },
            new PayloadContractDef() { Sign = "MaxHits", ValueType = DataValueType.Int },
        };

        /// <summary>
        /// 模块依赖 与开火一致 命中数满散场走死亡
        /// </summary>
        public static readonly string[] RequiredModules = { "死亡" };

        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is WeaponSettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }
            bool hasOrbit = false;
            if (c.Weapons != null) {
                foreach (var w in c.Weapons) {
                    if (w == null) { red.Add("有把枪是空行，删掉"); continue; }
                    if (w.Kind != WeaponKind.Orbit) continue;
                    hasOrbit = true;
                    if (string.IsNullOrEmpty(w.WeaponID)) red.Add("有把环绕枪没填编号");
                    if (string.IsNullOrEmpty(w.SpawnSign)) red.Add($"枪 {w.WeaponID} 没配养什么球");
                    if (w.UseFireCondition && string.IsNullOrEmpty(w.ConditionParam)) yellow.Add($"枪 {w.WeaponID} 勾了条件但没填看哪个数");
                }
            }
            if (!hasOrbit) yellow.Add("军火库里没有环绕枪，挂了也白挂");
        }

        private OrbitKeeperData _data;
        private List<WeaponItem> _orbits = new List<WeaponItem>();
        private string _orbitWeaponID = "";
        private List<int> _orbitBallIDs = new List<int>();

        public Behaviour_Event_OrbitKeeper(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _data = AttachBehaviourData<OrbitKeeperData>();
            OrbitKeeperSetting setting = Loader.LoadAsset<OrbitKeeperSetting>(AssetType.ASSET, settingPath);
            if (setting == null) { LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath); return; }
            if (!setting.TryGet(entity.ObjConfig.Sign, out WeaponSettingData settingData)) return;
            _data.Config.DefaultWeaponID = settingData.DefaultWeaponID;
            if (settingData.Weapons != null) {
                foreach (WeaponItem w in settingData.Weapons) {
                    if (w != null && w.Kind == WeaponKind.Orbit) _orbits.Add(w);
                }
            }
            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() { }

        private void OnUpdate() {
            if (_orbits.Count == 0) return;
            WeaponItem weapon = PickOrbit();
            if (weapon == null) { ClearBalls(); return; }
            if (weapon.UseFireCondition && !string.IsNullOrEmpty(weapon.ConditionParam)) {
                EntityAttrRegistry.TryGetNumber(entity, weapon.ConditionParam, out float cur);
                if (!Compare(cur, weapon.ConditionValue, weapon.ConditionCompare)) { ClearBalls(); return; }
            }
            if (weapon.WeaponID != _orbitWeaponID) ClearBalls();
            KeepBalls(weapon);
        }

        private WeaponItem PickOrbit() {
            string curID = _data.Config.DefaultWeaponID;
            if (EntityAttrRegistry.TryGetText(entity, DataLabels.CurrentWeapon, out string cid) && !string.IsNullOrEmpty(cid)) curID = cid;
            foreach (var w in _orbits) if (w.WeaponID == curID) return w;
            return _orbits[0];
        }

        private bool Compare(float left, float right, TeleportCompare c) {
            switch (c) {
                case TeleportCompare.Greater: return left > right;
                case TeleportCompare.GreaterEqual: return left >= right;
                case TeleportCompare.Equal: return Mathf.Abs(left - right) < 0.001f;
                case TeleportCompare.LessEqual: return left <= right;
                case TeleportCompare.Less: return left < right;
                case TeleportCompare.NotEqual: return Mathf.Abs(left - right) >= 0.001f;
                default: return left >= right;
            }
        }

        private void KeepBalls(WeaponItem weapon) {
            for (int i = _orbitBallIDs.Count - 1; i >= 0; i--) {
                if (!EntityRegister.TryGetEntityByID(_orbitBallIDs[i], out Entity ball) || ball == null) _orbitBallIDs.RemoveAt(i);
            }
            int want = 1;
            if (weapon.Payload != null) {
                foreach (var item in weapon.Payload) {
                    if (item != null && item.ParamSign == "OrbitCount") {
                        if (item.ValueType == DataValueType.Int) want = Mathf.Max(item.IntValue, 1);
                        else if (item.ValueType == DataValueType.Float) want = Mathf.Max(Mathf.RoundToInt(item.FloatValue), 1);
                        break;
                    }
                }
            }
            while (_orbitBallIDs.Count > want) { DismissBall(_orbitBallIDs[_orbitBallIDs.Count - 1]); _orbitBallIDs.RemoveAt(_orbitBallIDs.Count - 1); }
            while (_orbitBallIDs.Count < want) {
                Entity ball = Obj.Instance.LoadEntity(weapon.SpawnSign);
                if (ball == null) { LogUtil.LogErrorFormat("行为:{0} 环绕球生成失败:{1}", BehaviourSign, weapon.SpawnSign); return; }
                _orbitBallIDs.Add(ball.ID);
                _orbitWeaponID = weapon.WeaponID;
                EntityAttrRegistry.SetNumber(ball, DataLabels.HolderID, entity.ID);
                EntityAttrRegistry.SetText(ball, DataLabels.TargetType, weapon.TargetType ?? "");
                EntityAttrRegistry.SetNumber(ball, "OrbitAngle", 360f * _orbitBallIDs.Count / Mathf.Max(want, 1));
                if (weapon.Payload != null) {
                    foreach (var item in weapon.Payload) {
                        if (item == null || string.IsNullOrEmpty(item.ParamSign)) continue;
                        WritePayload(ball, item);
                    }
                }
            }
            _orbitWeaponID = weapon.WeaponID;
        }

        private void WritePayload(Entity ball, WeaponPayloadItem item) {
            switch (item.ValueType) {
                case DataValueType.Bool: EntityAttrRegistry.SetBool(ball, item.ParamSign, item.BoolValue); break;
                case DataValueType.Int: EntityAttrRegistry.SetNumber(ball, item.ParamSign, item.IntValue); break;
                case DataValueType.Float: EntityAttrRegistry.SetNumber(ball, item.ParamSign, item.FloatValue); break;
                case DataValueType.String: EntityAttrRegistry.SetText(ball, item.ParamSign, item.StringValue ?? ""); break;
                case DataValueType.Vector3: EntityAttrRegistry.SetVector(ball, item.ParamSign, item.Vector3Value); break;
            }
        }

        private void ClearBalls() {
            foreach (int id in _orbitBallIDs) DismissBall(id);
            _orbitBallIDs.Clear();
            _orbitWeaponID = "";
        }

        private void DismissBall(int id) {
            if (EntityRegister.TryGetEntityByID(id, out Entity ball) && ball != null) {
                EntityAttrRegistry.SetBool(ball, DataLabels.Dead, true);
            }
        }

        public override void Clear() {
            ClearBalls();
            if (Game.instance != null) Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            DetachBehaviourData<OrbitKeeperData>();
            base.Clear();
        }
    }
}
