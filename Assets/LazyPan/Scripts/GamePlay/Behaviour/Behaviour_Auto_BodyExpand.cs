using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 零件 - 体型扩散
    /// 只干一件事: 按 ExpandSpeed 长大 到 MaxRadius 停下并 Dead 全程只改自己 不认识圆环
    /// 参数节点自带：直读 BodyExpandSettingData 的 MaxRadius/ExpandSpeed，不用再配 ParamValue；
    /// 老资产填 0 则回退读 Data（ParamValue 写的 MaxRadius/ExpandSpeed），兼容旧配置
    /// 没 MaxRadius 睡觉 半径1的体型约定(直径=2倍半径) 视觉与伤害圈对齐
    /// </summary>
    public class Behaviour_Auto_BodyExpand : Behaviour {
        /// <summary>
        /// 对外契约（仅兼容老配置：Setting 为 0 时才读这些 Data）
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
        private float settingMax;
        private float settingSpeed;
        private bool isConfigValid;

        public Behaviour_Auto_BodyExpand(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            BodyExpandSetting setting = Loader.LoadAsset<BodyExpandSetting>(AssetType.ASSET, "Setting/BodyExpandSetting");
            if (setting != null && setting.TryGet(entity.ObjConfig.Sign, out BodyExpandSettingData data)) {
                settingMax = data.MaxRadius;
                settingSpeed = data.ExpandSpeed;
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

            Cond.Instance.TryGetData(entity, "MaxRadius", out FloatData maxData);
            Cond.Instance.TryGetData(entity, "ExpandSpeed", out FloatData speedData);
            float max = maxData != null ? maxData.Float : (settingMax > 0f ? settingMax : 5f);
            if (max <= 0f) {
                return;
            }

            float spd = speedData != null ? speedData.Float : (settingSpeed > 0f ? settingSpeed : 3f);
            float step = Mathf.Max(spd, 0.1f) * Time.deltaTime;
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
