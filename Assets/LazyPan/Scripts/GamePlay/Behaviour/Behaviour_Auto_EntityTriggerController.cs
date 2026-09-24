using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 实体触发控制器
    /// 订阅实体上指定 Comp 的触发器事件 按进入/停留/离开/范围外 四个相位对配置的实体(含自己)增减 Data 参数
    /// 行为只消费自身配置 通过 Data 标签与其他实体解耦交互 不感知对方业务 典型:玩家进塔范围 Energy 每秒涨 离开每秒降
    /// 配置来源 Setting/EntityTriggerControllerSetting 运行时状态写自身 Data(IsInTrigger/InsideCount)
    /// </summary>
    public class Behaviour_Auto_EntityTriggerController : Behaviour {
        /// <summary>触发器节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【触发器】管一块地盘，谁进来出去干什么全在这里定。\n" +
            "— 配置参数（EntityTriggerControllerSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>CompTriggerSign</color>：用地盘上哪个触发器，Root=实体根\n" +
            "- <color=#FFD54F>Rules</color>：触发规则，一条规则管一类人\n" +
            "— 每条规则怎么填 —\n" +
            "- <color=#FFD54F>TriggerEntitySign</color>：谁算数，Any=谁都算\n" +
            "- <color=#FFD54F>EnterActions</color>：进来瞬间触发一次\n" +
            "- <color=#FFD54F>StayActions</color>：待着不动每帧都触发\n" +
            "- <color=#FFD54F>ExitActions</color>：离开瞬间触发一次\n" +
            "- <color=#FFD54F>OutsideActions</color>：在范围外每帧触发\n" +
            "— 每个动作怎么填 —\n" +
            "- <color=#FFD54F>TargetEntitySign</color>：改谁，Self=自己，Triggerer=触发的人\n" +
            "- <color=#FFD54F>ParamSign</color>：改哪个数\n" +
            "- <color=#FFD54F>Modify</color>：Set=直接给，Add=累加\n" +
            "- <color=#FFD54F>Min</color>/<color=#FFD54F>Max</color>：改完夹在范围内，只对整数小数有效";
        private const string settingPath = "Setting/EntityTriggerControllerSetting";

        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is EntityTriggerControllerSettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (string.IsNullOrEmpty(c.CompTriggerSign)) {
                red.Add("没填用地盘上哪个触发器");
            }

            if (c.Rules == null || c.Rules.Count == 0) {
                red.Add("一条规则没有，谁来都没反应");
                return;
            }

            for (int i = 0; i < c.Rules.Count; i++) {
                var r = c.Rules[i];
                if (r == null) {
                    red.Add($"第{i + 1}条规则是空行，删掉");
                    continue;
                }

                if (string.IsNullOrEmpty(r.TriggerEntitySign)) {
                    red.Add($"第{i + 1}条规则没填谁算数（谁都算填 Any）");
                }

                CheckActions($"第{i + 1}条规则进来", r.EnterActions, red);
                CheckActions($"第{i + 1}条规则停留", r.StayActions, red);
                CheckActions($"第{i + 1}条规则离开", r.ExitActions, red);
                CheckActions($"第{i + 1}条规则范围外", r.OutsideActions, red);
            }
        }

        static void CheckActions(string where, System.Collections.Generic.List<TriggerAction> actions, System.Collections.Generic.List<string> red) {
            if (actions == null) {
                return;
            }

            for (int k = 0; k < actions.Count; k++) {
                var a = actions[k];
                if (a == null) {
                    red.Add($"{where}第{k + 1}个动作是空行，删掉");
                    continue;
                }

                if (string.IsNullOrEmpty(a.TargetEntitySign)) {
                    red.Add($"{where}第{k + 1}个动作没填改谁");
                }

                if (string.IsNullOrEmpty(a.ParamSign)) {
                    red.Add($"{where}第{k + 1}个动作没填改哪个数");
                }

                if (a.Min > a.Max) {
                    red.Add($"{where}第{k + 1}个动作下限比上限大");
                }
            }
        }

        //config
        private EntityTriggerControllerData _triggerData;
        private EntityTriggerControllerData.EntityTriggerControllerConfig _config;
        private List<RuleRuntime> _rules = new List<RuleRuntime>();

        //runtime
        private Comp _triggerComp;

        public Behaviour_Auto_EntityTriggerController(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _triggerData = AttachBehaviourData<EntityTriggerControllerData>();

            EntityTriggerControllerSetting setting = Loader.LoadAsset<EntityTriggerControllerSetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out EntityTriggerControllerSettingData settingData)) {
                return;
            }

            _config = _triggerData.Config;
            _config.IsInTrigger = false;
            _config.InsideCount = 0;

            CopySetting(settingData);
            if (_rules.Count == 0) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 触发规则为空!", BehaviourSign, entity.ObjConfig.Sign);
                return;
            }

            BindTriggerComp(settingData.CompTriggerSign);
            if (_triggerComp == null) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 未找到触发器组件:{2}", BehaviourSign, entity.ObjConfig.Sign, settingData.CompTriggerSign);
                return;
            }

            EntityRegister.OnEntityRemovedEvent.AddListener(OnEntityRemoved);
            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {
        }

        /// <summary>
        /// 配置拷贝到运行时规则 每条规则维护自己的触发者集合
        /// </summary>
        private void CopySetting(EntityTriggerControllerSettingData settingData) {
            foreach (TriggerRule rule in settingData.Rules) {
                _rules.Add(new RuleRuntime() { Rule = rule, Inside = new HashSet<int>() });
            }
        }

        /// <summary>
        /// 按 CompTriggerSign 解出带触发碰撞体的 Comp 并订阅触发器事件 Root=实体根
        /// </summary>
        private void BindTriggerComp(string compTriggerSign) {
            _triggerComp = compTriggerSign == BehaviourSigns.Root ? entity.Comp : Cond.Instance.Get<Comp>(entity, compTriggerSign);
            if (_triggerComp == null) {
                return;
            }

            _triggerComp.OnTriggerEnterEvent.AddListener(OnTriggerEnter);
            _triggerComp.OnTriggerStayEvent.AddListener(OnTriggerStay);
            _triggerComp.OnTriggerExitEvent.AddListener(OnTriggerExit);
        }

        private void OnTriggerEnter(Collider other) {
            if (!TryResolveEntity(other, out Entity otherEntity)) {
                return;
            }

            EnterRule(otherEntity);
        }

        private void OnTriggerStay(Collider other) {
            // 兜底: 进入事件遗漏时按进入处理 已在集合内的不重复触发进入相位
            if (!TryResolveEntity(other, out Entity otherEntity)) {
                return;
            }

            EnterRule(otherEntity);
        }

        private void OnTriggerExit(Collider other) {
            if (!TryResolveEntity(other, out Entity otherEntity)) {
                return;
            }

            foreach (RuleRuntime runtime in _rules) {
                if (runtime.Inside.Remove(otherEntity.ID)) {
                    ApplyActions(runtime.Rule.ExitActions, 1f, otherEntity);
                }
            }
        }

        /// <summary>
        /// 命中规则且首次进入 收集进集合并执行进入相位 已存在则跳过避免重复
        /// </summary>
        private void EnterRule(Entity otherEntity) {
            foreach (RuleRuntime runtime in _rules) {
                if (!Matches(runtime.Rule, otherEntity)) {
                    continue;
                }

                if (runtime.Inside.Add(otherEntity.ID)) {
                    ApplyActions(runtime.Rule.EnterActions, 1f, otherEntity);
                }
            }
        }

        /// <summary>
        /// 每帧驱动 范围内走停留相位 范围外走范围外相位 AddPerSecond 按 deltaTime 累加
        /// 停留相位按每一位停留的触发者分别执行 谁在范围内谁受作用
        /// </summary>
        private void OnUpdate() {
            bool anyInside = false;
            int insideCount = 0;
            float dt = Time.deltaTime;

            foreach (RuleRuntime runtime in _rules) {
                if (runtime.Inside.Count > 0) {
                    anyInside = true;
                    insideCount += runtime.Inside.Count;
                    foreach (int insideID in runtime.Inside) {
                        if (EntityRegister.TryGetEntityByID(insideID, out Entity triggerer)) {
                            ApplyActions(runtime.Rule.StayActions, dt, triggerer);
                        }
                    }
                } else {
                    ApplyActions(runtime.Rule.OutsideActions, dt, null);
                }
            }

            _config.IsInTrigger = anyInside;
            _config.InsideCount = insideCount;
        }

        /// <summary>
        /// 从碰撞体反查实体 沿父链逐级回溯定位 Comp 再经注册表反查 兼容子物体触发碰撞体
        /// </summary>
        private bool TryResolveEntity(Collider collider, out Entity otherEntity) {
            otherEntity = null;
            if (collider == null) {
                return false;
            }

            Transform tran = collider.transform;
            while (tran != null) {
                Comp comp = tran.GetComponent<Comp>();
                if (comp != null && EntityRegister.TryGetEntityByComp(comp, out otherEntity)) {
                    break;
                }

                tran = tran.parent;
            }

            // 忽略触发源自身
            return otherEntity != null && otherEntity != entity;
        }

        private bool Matches(TriggerRule rule, Entity otherEntity) {
            return rule.TriggerEntitySign == BehaviourSigns.Any || rule.TriggerEntitySign == otherEntity.ObjConfig.Sign;
        }

        private void ApplyActions(List<TriggerAction> actions, float dt, Entity triggerer) {
            if (actions == null) {
                return;
            }

            foreach (TriggerAction action in actions) {
                ApplyAction(action, dt, triggerer);
            }
        }

        /// <summary>
        /// 单条参数操作 Set赋值 Add累加 AddPerSecond按dt累加 数值类型受 Min/Max 钳制 目标缺失静默跳过避免每帧报错
        /// 目标解析优先用触发者 Triggerer 其余按 Sign 查实体
        /// </summary>
        private void ApplyAction(TriggerAction action, float dt, Entity triggerer) {
            if (action == null || string.IsNullOrEmpty(action.ParamSign)) {
                return;
            }

            if (!ResolveTargetEntity(action.TargetEntitySign, triggerer, out Entity target)) {
                return;
            }

            float scale = action.Modify == TriggerModifyType.AddPerSecond ? dt : 1f;

            switch (action.ValueType) {
                case ParamValueType.Bool:
                    if (Cond.Instance.TryGetData(target, action.ParamSign, out BoolData boolData)) {
                        boolData.Bool = action.BoolValue;
                    }

                    break;
                case ParamValueType.Int:
                    if (Cond.Instance.TryGetData(target, action.ParamSign, out IntData intData)) {
                        float intResult = action.Modify == TriggerModifyType.Set ? action.IntValue : intData.Int + action.IntValue * scale;
                        intData.Int = Mathf.RoundToInt(Mathf.Clamp(intResult, action.Min, action.Max));
                    }

                    break;
                case ParamValueType.Float:
                    if (Cond.Instance.TryGetData(target, action.ParamSign, out FloatData floatData)) {
                        float floatResult = action.Modify == TriggerModifyType.Set ? action.FloatValue : floatData.Float + action.FloatValue * scale;
                        floatData.Float = Mathf.Clamp(floatResult, action.Min, action.Max);
                    }

                    break;
                case ParamValueType.String:
                    if (Cond.Instance.TryGetData(target, action.ParamSign, out StringData stringData)) {
                        stringData.String = action.StringValue;
                    }

                    break;
                case ParamValueType.Vector3:
                    if (Cond.Instance.TryGetData(target, action.ParamSign, out Vector3Data vector3Data)) {
                        vector3Data.Vector3 = action.Modify == TriggerModifyType.Set
                            ? action.Vector3Value
                            : vector3Data.Vector3 + action.Vector3Value * scale;
                    }

                    break;
            }
        }

        /// <summary>
        /// 解析被修改实体 Triggerer=作用在触发者身上 Self/自己Sign=触发源自己 其他按Sign查其他实体 行为不感知对方类型
        /// </summary>
        private bool ResolveTargetEntity(string targetEntitySign, Entity triggerer, out Entity target) {
            if (targetEntitySign == BehaviourSigns.Triggerer) {
                target = triggerer;
                return target != null;
            }

            return BehaviourSigns.ResolveEntity(entity, BehaviourSign, nameof(TriggerAction.TargetEntitySign), targetEntitySign, out target);
        }

        private void OnEntityRemoved(int id) {
            foreach (RuleRuntime runtime in _rules) {
                runtime.Inside.Remove(id);
            }
        }

        public override void Clear() {
            EntityRegister.OnEntityRemovedEvent.RemoveListener(OnEntityRemoved);
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            if (_triggerComp != null) {
                _triggerComp.OnTriggerEnterEvent.RemoveListener(OnTriggerEnter);
                _triggerComp.OnTriggerStayEvent.RemoveListener(OnTriggerStay);
                _triggerComp.OnTriggerExitEvent.RemoveListener(OnTriggerExit);
            }

            _rules.Clear();
            DetachBehaviourData<EntityTriggerControllerData>();
            base.Clear();
        }

        /// <summary>
        /// 单条规则的运行时状态 仅行为内部使用
        /// </summary>
        private class RuleRuntime {
            public TriggerRule Rule;
            public HashSet<int> Inside;
        }
    }
}
