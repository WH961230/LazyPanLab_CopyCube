using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 零件 - 跟随主人
    /// 只干一件事: 围着 HolderID 转 主人没了自己散 不认识环绕球 全经过 Data
    /// 没主人睡觉 常驻不死 散场只靠外部置 Dead
    /// </summary>
    public class Behaviour_Auto_FollowHolder : Behaviour {
        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is FollowHolderSettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (c.OrbitRadius <= 0f) {
                yellow.Add("半径<=0，会按 0.5 算");
            }

            if (c.OrbitSpeed == 0f) {
                yellow.Add("速度=0，挂着不动");
            }

            yellow.Add("跨实体提醒：主人由触发器交过来，没主人就原地睡觉");
        }
        /// <summary>跟随主人节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【跟随主人】管一个东西围着主人转，主人没了就原地睡觉。\n" +
            "跟谁不用你配，触发器会交过来。\n" +
            "— 配置参数（FollowHolderSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>OrbitRadius</color>：转圈半径，建议 1~4\n" +
            "- <color=#FFD54F>OrbitSpeed</color>：每秒转多少度，建议 90~360\n" +
            "- <color=#FFD54F>OrbitAngle</color>：出生时站在几点钟方向，0~360";
        /// <summary>
        /// 对外契约 HolderID 由触发器交接不用配
        /// </summary>
        public static readonly PayloadContractDef[] RequiredPayload = {
            new PayloadContractDef() { Sign = "OrbitRadius", ValueType = ParamValueType.Float, FloatDefault = 2f },
            new PayloadContractDef() { Sign = "OrbitSpeed", ValueType = ParamValueType.Float, FloatDefault = 180f },
            new PayloadContractDef() { Sign = "OrbitAngle", ValueType = ParamValueType.Float },
        };

        //runtime
        private float angle;
        private float settingRadius = 2f;
        private float settingSpeed = 180f;
        private bool isConfigValid;

        public Behaviour_Auto_FollowHolder(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            FollowHolderSetting setting = Loader.LoadAsset<FollowHolderSetting>(AssetType.ASSET, "Setting/FollowHolderSetting");
            if (setting != null && setting.TryGet(entity.ObjConfig.Sign, out FollowHolderSettingData data)) {
                settingRadius = data.OrbitRadius;
                settingSpeed = data.OrbitSpeed;
                angle = data.OrbitAngle;
            }

            if (Cond.Instance.TryGetData(entity, "OrbitAngle", out FloatData angleData)) {
                angle = angleData.Float;
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

            if (!Cond.Instance.TryGetData(entity, DataLabels.HolderID, out IntData holderID)
                || !EntityRegister.TryGetEntityByID(holderID.Int, out Entity holder)
                || holder == null) {
                return;
            }

            Cond.Instance.TryGetData(entity, "OrbitRadius", out FloatData radius);
            Cond.Instance.TryGetData(entity, "OrbitSpeed", out FloatData speed);
            float r = Mathf.Max(radius != null ? radius.Float : (settingRadius > 0f ? settingRadius : 2f), 0.5f);
            float deg = speed != null ? speed.Float : settingSpeed;

            angle += deg * Time.deltaTime;
            Transform holderRoot = Cond.Instance.Get<Transform>(holder, Label.BODY);
            Vector3 center = holderRoot != null ? holderRoot.position : holder.Comp.transform.position;
            MoveRoot().position = center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)) * r;
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
