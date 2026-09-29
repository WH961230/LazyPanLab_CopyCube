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
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is BodyExpandSettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (c.MaxRadius < 0f) {
                red.Add("半径是负数，长不大就睡了");
            } else if (c.MaxRadius == 0f) {
                yellow.Add("半径=0，沿用节点上配的数，确认节点上配了");
            }

            if (c.ExpandSpeed <= 0f) {
                yellow.Add("速度<=0，沿用节点上配的数，确认节点上配了");
            }
        }
        /// <summary>扩散节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【体型扩散】管一个东西从小变大，长到头自动停下并走死亡。\n" +
            "— 配置参数（BodyExpandSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>MaxRadius</color>：长到这个半径就停，0=沿用节点上配的数\n" +
            "- <color=#FFD54F>ExpandSpeed</color>：每秒长大多少，建议 1~5，0=沿用节点上配的数\n" +
            "— 外部怎么互动 —\n" +
            "- <color=#FFD54F>停下</color>：长满自动停并喊死，不用你管";
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
