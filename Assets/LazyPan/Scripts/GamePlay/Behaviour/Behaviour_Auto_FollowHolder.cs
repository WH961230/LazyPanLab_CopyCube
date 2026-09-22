using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 零件 - 跟随主人
    /// 只干一件事: 围着 HolderID 转 主人没了自己散 不认识环绕球 全经过 Data
    /// 没主人睡觉 常驻不死 散场只靠外部置 Dead
    /// </summary>
    public class Behaviour_Auto_FollowHolder : Behaviour {
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
        private bool isConfigValid;

        public Behaviour_Auto_FollowHolder(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            FollowHolderSetting setting = Loader.LoadAsset<FollowHolderSetting>(AssetType.ASSET, "Setting/FollowHolderSetting");
            if (setting != null) {
                setting.TryGet(entity.ObjConfig.Sign, out FollowHolderSettingData _);
            }

            Cond.Instance.TryGetData(entity, "OrbitAngle", out FloatData angleData);
            angle = angleData != null ? angleData.Float : 0f;

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
            float r = radius != null ? Mathf.Max(radius.Float, 0.5f) : 2f;
            float deg = speed != null ? speed.Float : 180f;

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
