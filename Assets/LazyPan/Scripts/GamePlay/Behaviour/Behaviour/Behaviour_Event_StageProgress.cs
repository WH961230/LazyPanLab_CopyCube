using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 阶段进度
    /// 只做一件事: 维护阶段钥匙(Int)与进度数值(Float)的升阶关系 进度装满自动钥匙+1 余量继承到下一阶段
    /// 监听方式: 每帧 OnUpdate 自动归一化 外部只管往进度里加数(如敌人死亡玩家 Exp+10) 不用调本行为
    /// 累加口子: Level 累加已有(while 连升) 另提供 AddProgress/AddStage 供外部直接调 调完下一帧自动归一化
    /// 不认识任何业务词 经验等级杀敌军衔都只是标签名 只读阶段表做真实数值升阶计算
    /// 配置来源 Setting/StageProgressSetting 满级掐顶 不做掉级 不做升级瞬间行为
    /// </summary>
    public class Behaviour_Event_StageProgress : Behaviour {
        /// <summary>阶段进度节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【阶段进度】管升级那套数，进度装满自动升一段，多的进度带到下一段。\n" +
            "— 配置参数（StageProgressSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>StageParamSign</color>：段数存哪个标签，Int 类型\n" +
            "- <color=#FFD54F>ProgressParamSign</color>：进度存哪个标签，Float 类型\n" +
            "- <color=#FFD54F>InitialStage</color>：开局第几段\n" +
            "- <color=#FFD54F>MaxStage</color>：满级第几段，封顶\n" +
            "- <color=#FFD54F>FallbackCap</color>：表里没写的段统一按这个算\n" +
            "- <color=#FFD54F>Caps</color>：每段上限表，一条一段\n" +
            "- <color=#FFD54F>MaxStageParamSign</color>+<color=#FFD54F>MaxProgressParamSign</color>：选填，空=不同步\n" +
            "— 外部怎么互动 —\n" +
            "- <color=#FFD54F>加进度</color>：只管往进度里加数，升段它自己算";
        private const string settingPath = "Setting/StageProgressSetting";

        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is StageProgressSettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (string.IsNullOrEmpty(c.StageParamSign)) {
                red.Add("没填段数存哪个标签");
            }

            if (string.IsNullOrEmpty(c.ProgressParamSign)) {
                red.Add("没填进度存哪个标签");
            }

            if (c.MaxStage < c.InitialStage) {
                red.Add("满级比开局低，升不动");
            }

            if (c.Caps == null || c.Caps.Count == 0) {
                yellow.Add("一段上限没配，全走兜底算");
            }

            if (c.FallbackCap <= 0f) {
                yellow.Add("兜底上限<=0，表里没写的段装不满");
            }

            if (c.StageUpEvents != null) {
                for (int i = 0; i < c.StageUpEvents.Count; i++) {
                    var e = c.StageUpEvents[i];
                    if (e == null) {
                        red.Add($"第{i + 1}个升级事件是空行，删掉");
                        continue;
                    }

                    if (string.IsNullOrEmpty(e.TargetEntitySign)) {
                        red.Add($"第{i + 1}个升级事件没填给谁");
                    }

                    if (string.IsNullOrEmpty(e.ParamSign)) {
                        red.Add($"第{i + 1}个升级事件没填改哪个数");
                    }
                }
            }
        }

        //config
        private StageProgressData _stageData;
        private StageProgressData.StageProgressConfig _config;

        //data
        private IntData _stageIntData;
        private IntData _maxStageIntData;
        private FloatData _progressFloatData;
        private FloatData _maxProgressFloatData;

        public Behaviour_Event_StageProgress(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _stageData = AttachBehaviourData<StageProgressData>();

            StageProgressSetting setting = Loader.LoadAsset<StageProgressSetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out StageProgressSettingData settingData)) {
                return;
            }

            if (!BehaviourSigns.Require(settingData.StageParamSign, BehaviourSign, entity.ObjConfig?.Sign, nameof(StageProgressSettingData.StageParamSign))) {
                return;
            }

            if (!BehaviourSigns.Require(settingData.ProgressParamSign, BehaviourSign, entity.ObjConfig?.Sign, nameof(StageProgressSettingData.ProgressParamSign))) {
                return;
            }

            CopySetting(settingData);
            if (!BindRuntimeData()) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 阶段进度参数绑定失败!", BehaviourSign, entity.ObjConfig.Sign);
                return;
            }

            Normalize();
            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {
        }

        /// <summary>
        /// 配置资产拷贝到实体 Data 与配置资产解耦 上限小于等于 0 的行直接丢弃
        /// </summary>
        private void CopySetting(StageProgressSettingData settingData) {
            _config = _stageData.Config;
            _config.StageParamSign = settingData.StageParamSign;
            _config.MaxStageParamSign = string.IsNullOrEmpty(settingData.MaxStageParamSign) ? string.Empty : settingData.MaxStageParamSign.Trim();
            _config.ProgressParamSign = settingData.ProgressParamSign;
            _config.MaxProgressParamSign = string.IsNullOrEmpty(settingData.MaxProgressParamSign) ? string.Empty : settingData.MaxProgressParamSign.Trim();
            _config.InitialStage = Mathf.Max(settingData.InitialStage, 1);
            _config.MaxStage = Mathf.Max(settingData.MaxStage, _config.InitialStage);
            _config.FallbackCap = Mathf.Max(settingData.FallbackCap, 1f);
            _config.Caps.Clear();
            _config.StageUpEvents.Clear();

            if (settingData.StageUpEvents != null) {
                foreach (ParamModifyItem item in settingData.StageUpEvents) {
                    if (item == null) {
                        continue;
                    }

                    if (!BehaviourSigns.Require(item.TargetEntitySign, BehaviourSign, entity.ObjConfig?.Sign, nameof(ParamModifyItem.TargetEntitySign))) {
                        continue;
                    }

                    if (!BehaviourSigns.Require(item.ParamSign, BehaviourSign, item.TargetEntitySign, nameof(ParamModifyItem.ParamSign))) {
                        continue;
                    }

                    _config.StageUpEvents.Add(new DeathData.ParamModifyConfig() {
                        TargetEntitySign = item.TargetEntitySign,
                        ParamSign = item.ParamSign,
                        ValueType = item.ValueType,
                        Modify = item.Modify,
                        BoolValue = item.BoolValue,
                        IntValue = item.IntValue,
                        FloatValue = item.FloatValue,
                        StringValue = item.StringValue,
                        Vector3Value = item.Vector3Value,
                    });
                }
            }

            if (settingData.Caps == null) {
                return;
            }

            HashSet<int> seen = new HashSet<int>();
            foreach (StageCapItem item in settingData.Caps) {
                if (item == null) {
                    continue;
                }

                if (item.Cap <= 0f) {
                    LogUtil.LogErrorFormat("行为:{0} 实体:{1} 阶段:{2} 上限必须大于 0 已丢弃!", BehaviourSign, entity.ObjConfig?.Sign, item.Stage);
                    continue;
                }

                if (!seen.Add(item.Stage)) {
                    LogUtil.LogErrorFormat("行为:{0} 实体:{1} 阶段:{2} 重复配置 已丢弃!", BehaviourSign, entity.ObjConfig?.Sign, item.Stage);
                    continue;
                }

                _config.Caps.Add(new StageProgressData.StageCapConfig() {
                    Stage = item.Stage,
                    Cap = item.Cap,
                });
            }
        }

        /// <summary>
        /// 绑定阶段钥匙与进度数值 拿不到就自己注册 不依赖任何外部初始赋值
        /// 钥匙低于开局扶到开局 进度为负按 0 算 每次写入同步全局注册表 保证两边读到同一个数
        /// </summary>
        private bool BindRuntimeData() {
            if (!Cond.Instance.TryGetData(entity, _config.StageParamSign, out _stageIntData)) {
                return false;
            }

            _maxStageIntData = null;
            if (!string.IsNullOrEmpty(_config.MaxStageParamSign)) {
                Cond.Instance.TryGetData(entity, _config.MaxStageParamSign, out _maxStageIntData);
            }

            if (!Cond.Instance.TryGetData(entity, _config.ProgressParamSign, out _progressFloatData)) {
                return false;
            }

            _maxProgressFloatData = null;
            if (!string.IsNullOrEmpty(_config.MaxProgressParamSign)) {
                Cond.Instance.TryGetData(entity, _config.MaxProgressParamSign, out _maxProgressFloatData);
            }

            if (_stageIntData.Int < _config.InitialStage) {
                _stageIntData.Int = _config.InitialStage;
            }

            if (_progressFloatData.Float < 0f) {
                _progressFloatData.Float = 0f;
            }

            EntityAttrRegistry.SetNumber(entity, _config.StageParamSign, _stageIntData.Int);
            EntityAttrRegistry.SetNumber(entity, _config.ProgressParamSign, _progressFloatData.Float);
            if (_maxStageIntData != null) {
                EntityAttrRegistry.SetNumber(entity, _config.MaxStageParamSign, _maxStageIntData.Int);
            }
            if (_maxProgressFloatData != null) {
                EntityAttrRegistry.SetNumber(entity, _config.MaxProgressParamSign, _maxProgressFloatData.Float);
            }

            return true;
        }

        private void OnUpdate() {
            Normalize();
            SyncMaxProgress();
            SyncMaxStage();
        }

        /// <summary>
        /// 对外累加口子 往进度里加数(如击杀奖励 Exp+10) 调完下一帧 OnUpdate 自动归一化升阶
        /// </summary>
        public void AddProgress(float amount) {
            if (_progressFloatData == null) {
                return;
            }

            _progressFloatData.Float += amount;
            EntityAttrRegistry.SetNumber(entity, _config.ProgressParamSign, _progressFloatData.Float);
            Normalize();
            SyncMaxProgress();
            SyncMaxStage();
        }

        /// <summary>
        /// 对外累加口子 直接加阶段(如任务奖励 Level+1) 调完自动归一化 进度不动只换上限
        /// </summary>
        public void AddStage(int amount) {
            if (_stageIntData == null) {
                return;
            }

            _stageIntData.Int += amount;
            EntityAttrRegistry.SetNumber(entity, _config.StageParamSign, _stageIntData.Int);
            Normalize();
            SyncMaxProgress();
            SyncMaxStage();
        }

        /// <summary>
        /// 每帧把上限标签同步为当前阶段的上限(如 MaxExp=当前 Level 对应的 Cap) 供滑条显示用
        /// 上限没配则不同步 外部读表查 GetCap 也行
        /// </summary>
        public void SyncMaxProgress() {
            if (_maxProgressFloatData == null || _stageIntData == null) {
                return;
            }

            _maxProgressFloatData.Float = GetCap(_stageIntData.Int);
            EntityAttrRegistry.SetNumber(entity, _config.MaxProgressParamSign, _maxProgressFloatData.Float);
        }

        /// <summary>
        /// 每帧把阶段上限标签同步为满级阶段(如 MaxLevel=MaxStage) 供界面与其他行为读取用
        /// 阶段在这个行为里算 上限就该这里给 上限没配则不同步 外部读配置也行
        /// </summary>
        public void SyncMaxStage() {
            if (_maxStageIntData == null) {
                return;
            }

            _maxStageIntData.Int = _config.MaxStage;
            EntityAttrRegistry.SetNumber(entity, _config.MaxStageParamSign, _maxStageIntData.Int);
        }

        /// <summary>
        /// 查某阶段上限 供外部(如 UI)按需读取 不写数据
        /// </summary>
        public float GetCapOf(int stage) {
            return GetCap(stage);
        }

        /// <summary>
        /// 查某阶段上限 表里写了用表里的 没写用兜底
        /// </summary>
        private float GetCap(int stage) {
            foreach (StageProgressData.StageCapConfig cap in _config.Caps) {
                if (cap.Stage == stage) {
                    return cap.Cap;
                }
            }

            return _config.FallbackCap;
        }

        /// <summary>
        /// 真实升阶计算 进度装满钥匙+1 余量继承 一帧内连升到底不吞数
        /// 流程举例: 1级上限100 2级上限150 Exp 一次 +120 则 120-100=20 升到2级 余20继承
        /// 到满级掐顶 进度卡在上限 不做掉级 进度为负只按住以后拓展
        /// </summary>
        private void Normalize() {
            if (_stageIntData == null || _progressFloatData == null) {
                return;
            }

            if (_progressFloatData.Float < 0f) {
                _progressFloatData.Float = 0f;
            }

            if (_stageIntData.Int < _config.InitialStage) {
                _stageIntData.Int = _config.InitialStage;
            }

            if (_stageIntData.Int > _config.MaxStage) {
                _stageIntData.Int = _config.MaxStage;
            }

            int guard = 0;
            int stageBefore = _stageIntData.Int;
            while (_progressFloatData.Float >= GetCap(_stageIntData.Int)) {
                if (_stageIntData.Int >= _config.MaxStage) {
                    _progressFloatData.Float = GetCap(_stageIntData.Int);
                    break;
                }

                _progressFloatData.Float -= GetCap(_stageIntData.Int);
                _stageIntData.Int += 1;

                guard += 1;
                if (guard > 100000) {
                    LogUtil.LogErrorFormat("行为:{0} 实体:{1} 升阶次数过多 已强制中断 请检查阶段表!", BehaviourSign, entity.ObjConfig?.Sign);
                    break;
                }
            }

            //升了几级触发几次事件(只改数 不调行为 目标不存在就地注册再改 单项失败不影响其余项)
            int gained = _stageIntData.Int - stageBefore;
            EntityAttrRegistry.SetNumber(entity, _config.StageParamSign, _stageIntData.Int);
            EntityAttrRegistry.SetNumber(entity, _config.ProgressParamSign, _progressFloatData.Float);
            for (int i = 0; i < gained; i++) {
                ApplyStageUpEvents();
            }
        }

        /// <summary>
        /// 升阶事件执行一次 按配置改一批参数 单项失败不影响其余项 与死亡结算一个语义
        /// </summary>
        private void ApplyStageUpEvents() {
            foreach (DeathData.ParamModifyConfig config in _config.StageUpEvents) {
                if (!BehaviourSigns.ResolveEntity(entity, BehaviourSign, nameof(ParamModifyItem.TargetEntitySign), config.TargetEntitySign, out Entity target)) {
                    continue;
                }

                switch (config.ValueType) {
                    case ParamValueType.Bool:
                        if (Cond.Instance.TryGetData(target, config.ParamSign, out BoolData boolData)) {
                            boolData.Bool = config.BoolValue;
                            EntityAttrRegistry.SetBool(target, config.ParamSign, config.BoolValue);
                        }

                        break;
                    case ParamValueType.Int:
                        if (Cond.Instance.TryGetData(target, config.ParamSign, out IntData intData)) {
                            intData.Int = config.Modify == ParamModifyType.Add ? intData.Int + config.IntValue : config.IntValue;
                            EntityAttrRegistry.SetNumber(target, config.ParamSign, intData.Int);
                        }

                        break;
                    case ParamValueType.Float:
                        if (Cond.Instance.TryGetData(target, config.ParamSign, out FloatData floatData)) {
                            floatData.Float = config.Modify == ParamModifyType.Add ? floatData.Float + config.FloatValue : config.FloatValue;
                            EntityAttrRegistry.SetNumber(target, config.ParamSign, floatData.Float);
                        }

                        break;
                    case ParamValueType.String:
                        if (Cond.Instance.TryGetData(target, config.ParamSign, out StringData stringData)) {
                            stringData.String = config.StringValue;
                            EntityAttrRegistry.SetText(target, config.ParamSign, config.StringValue);
                        }

                        break;
                    case ParamValueType.Vector3:
                        if (Cond.Instance.TryGetData(target, config.ParamSign, out Vector3Data vector3Data)) {
                            vector3Data.Vector3 = config.Modify == ParamModifyType.Add ? vector3Data.Vector3 + config.Vector3Value : config.Vector3Value;
                        }

                        break;
                    default:
                        LogUtil.LogErrorFormat("行为:{0} 不支持的参数类型:{1}", BehaviourSign, config.ValueType);
                        break;
                }
            }
        }

        public override void Clear() {
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            DetachBehaviourData<StageProgressData>();
            base.Clear();
        }
    }
}
