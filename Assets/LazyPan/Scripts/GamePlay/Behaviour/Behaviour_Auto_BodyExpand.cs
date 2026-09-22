using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 零件 - 体型扩散
    /// 只干一件事: 按 ExpandSpeed 长大 到 MaxRadius 停下并 Dead 全程只改自己 不认识圆环
    /// 没 MaxRadius 睡觉 半径1的体型约定(直径=2倍半径) 视觉与伤害圈对齐
    /// </summary>
    public class Behaviour_Auto_BodyExpand : Behaviour {
        /// <summary>
        /// 对外契约
        /// </summary>
        public static readonly PayloadContractDef[] RequiredPayload = {
            new PayloadContractDef() { Sign = "MaxRadius", ValueType = ParamValueType.Float, FloatDefault = 5f },
            new PayloadContractDef() { Sign = "ExpandSpeed", ValueType = ParamValueType.Float, FloatDefault = 3f },
        };

        /// <summary>
        /// 模块依赖 扩满散场走死亡 无血条分支
        /// </summary>
        public static readonly string[] RequiredModules = { "死亡" };

        //runtime
        private float radius;
        private bool isConfigValid;

        public Behaviour_Auto_BodyExpand(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            BodyExpandSetting setting = Loader.LoadAsset<BodyExpandSetting>(AssetType.ASSET, "Setting/BodyExpandSetting");
            if (setting != null) {
                setting.TryGet(entity.ObjConfig.Sign, out BodyExpandSettingData _);
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

            Cond.Instance.TryGetData(entity, "MaxRadius", out FloatData maxRadius);
            Cond.Instance.TryGetData(entity, "ExpandSpeed", out FloatData expandSpeed);
            float max = maxRadius != null ? maxRadius.Float : 0f;
            if (max <= 0f) {
                return;
            }

            float step = (expandSpeed != null ? Mathf.Max(expandSpeed.Float, 0.1f) : 3f) * Time.deltaTime;
            radius = Mathf.Min(radius + step, max);
            Transform root = MoveRoot();
            root.localScale = Vector3.one * Mathf.Max(radius * 2f, 0.01f);

            //伤害圈跟着长 接触伤害零件认 DamageRadius 零件之间只经过 Data 传话
            if (Cond.Instance.TryGetData(entity, "DamageRadius", out FloatData damageRadius)) {
                damageRadius.Float = radius;
            }

            if (radius >= max) {
                if (Cond.Instance.TryGetData(entity, DataLabels.Dead, out BoolData dead)) {
                    dead.Bool = true;
                }

                if (Game.instance != null) {
                    Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
                }
                isConfigValid = false;
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
