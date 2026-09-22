using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 零件 - 飞行追踪
    /// 只干一件事: 按 Speed 往前飞 TargetID>0 就 homing 追它 =0 就按出生方向直走
    /// 没 Speed 睡觉 不认识枪不认识子弹 全经过 Data
    /// </summary>
    public class Behaviour_Auto_FlyTrack : Behaviour {
        /// <summary>
        /// 对外契约 TargetID/TargetType 由触发器交接不用配
        /// </summary>
        public static readonly PayloadContractDef[] RequiredPayload = {
            new PayloadContractDef() { Sign = "Speed", ValueType = ParamValueType.Float, FloatDefault = 10f },
        };

        //runtime
        private Vector3 direction = Vector3.forward;
        private float settingSpeed = 10f;
        private bool isConfigValid;

        public Behaviour_Auto_FlyTrack(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            FlyTrackSetting setting = Loader.LoadAsset<FlyTrackSetting>(AssetType.ASSET, "Setting/FlyTrackSetting");
            if (setting != null && setting.TryGet(entity.ObjConfig.Sign, out FlyTrackSettingData data)) {
                settingSpeed = data.Speed;
            }

            Transform root = MoveRoot();
            if (root != null) {
                direction = root.forward;
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

            Cond.Instance.TryGetData(entity, "Speed", out FloatData speed);
            float spd = speed != null ? speed.Float : settingSpeed;
            if (spd <= 0f) {
                return;
            }

            Vector3 selfPos = MoveRoot().position;
            if (Cond.Instance.TryGetData(entity, DataLabels.TargetID, out IntData targetID)
                && targetID.Int > 0
                && EntityRegister.TryGetEntityByID(targetID.Int, out Entity target)
                && target != null) {
                Transform targetBody = Cond.Instance.Get<Transform>(target, Label.BODY);
                Vector3 targetPos = targetBody != null ? targetBody.position : target.Comp.transform.position;
                Vector3 toTarget = targetPos - selfPos;
                if (toTarget.sqrMagnitude > 0.0001f) {
                    direction = toTarget.normalized;
                }
            }

            float step = spd * Time.deltaTime;
            MoveRoot().position = selfPos + direction * step;
            if (direction.sqrMagnitude > 0.0001f) {
                MoveRoot().LookAt(selfPos + direction);
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
