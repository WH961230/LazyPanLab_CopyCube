using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LazyPan {
    /// <summary>
    /// 行为 - 肉鸽三选一
    /// 只做三件事: 暂停开奖 点牌生效 关面板继续 UI 壳和内容绝对独立
    /// UI 壳只会贴纸条(标题/描述) 点了谁喊一嗓子 内容池只管奖池和效果包 中间拿纸条传话
    /// 效果生效复用老积木语义(目标实体+参数+Set/Add) 自己不算数 不认识任何卡名
    /// 配置来源 Setting/UIPickOneOfThreeSetting 一个实体一条 里面配一个奖池
    /// 对外入口 Open() 由升级满级等外部调用 关面板后自动恢复时间
    /// </summary>
    public class Behaviour_Event_UIPickOneOfThree : Behaviour {
        private const string settingPath = "Setting/UIPickOneOfThreeSetting";
        private const int pickCount = 3;

        //面板 Comp 标签约定 Card0/Card1/Card2=按钮 Card0_Title=标题 以此类推
        private const string cardButtonSign = "Card";
        private const string cardTitleSuffix = "_Title";
        private const string cardDescSuffix = "_Desc";

        //config
        private UIPickOneOfThreeData _pickData;
        private UIPickOneOfThreeData.UIPickOneOfThreeConfig _config;

        //runtime
        private GameObject panelInstance;
        private float prevTimeScale = 1f;
        private float autoOpenElapsed;

        public Behaviour_Event_UIPickOneOfThree(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _pickData = AttachBehaviourData<UIPickOneOfThreeData>();

            UIPickOneOfThreeSetting setting = Loader.LoadAsset<UIPickOneOfThreeSetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out UIPickOneOfThreeSettingData settingData)) {
                return;
            }

            _config = _pickData.Config;
            CopySetting(settingData);

            //测试开关开了就计时 进场景几秒后自动弹一次 正式接升级调用后填0关闭
            if (_config.AutoOpenDelay > 0f) {
                Game.instance.OnUpdateEvent.AddListener(OnTestTick);
            }
        }

        public override void DelayedExecute() {

        }

        /// <summary>
        /// 配置拷贝到实体Data 与配置资产解耦 空卡直接丢弃
        /// </summary>
        private void CopySetting(UIPickOneOfThreeSettingData settingData) {
            _config.PanelPrefabSign = settingData.PanelPrefabSign;
            _config.MountSign = string.IsNullOrEmpty(settingData.MountSign) ? BehaviourSigns.Root : settingData.MountSign;
            _config.AutoOpenDelay = settingData.AutoOpenDelay;
            _config.Pool.Clear();
            _config.CurrentPicks.Clear();
            _config.IsOpen = false;

            if (settingData.Pool == null || settingData.Pool.Count == 0) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 奖池为空!", BehaviourSign, entity.ObjConfig.Sign);
                return;
            }

            foreach (PickCardItem card in settingData.Pool) {
                if (card == null || string.IsNullOrEmpty(card.Title)) {
                    continue;
                }

                PickCardItem copy = new PickCardItem() {
                    Title = card.Title,
                    Description = card.Description,
                    IconName = card.IconName,
                };

                if (card.Effects != null) {
                    foreach (PickCardEffect effect in card.Effects) {
                        if (effect == null) {
                            continue;
                        }

                        copy.Effects.Add(new PickCardEffect() {
                            TargetEntitySign = effect.TargetEntitySign,
                            ParamSign = effect.ParamSign,
                            ValueType = effect.ValueType,
                            Modify = effect.Modify,
                            BoolValue = effect.BoolValue,
                            IntValue = effect.IntValue,
                            FloatValue = effect.FloatValue,
                            StringValue = effect.StringValue,
                            Vector3Value = effect.Vector3Value,
                        });
                    }
                }

                _config.Pool.Add(copy);
            }
        }

        /// <summary>
        /// 测试计时 到点自动开一次 正式接线后把 AutoOpenDelay 填0即可关闭
        /// </summary>
        private void OnTestTick() {
            autoOpenElapsed += Time.deltaTime;
            if (autoOpenElapsed < _config.AutoOpenDelay) {
                return;
            }

            Game.instance.OnUpdateEvent.RemoveListener(OnTestTick);
            Open();
        }

        /// <summary>
        /// 对外唯一入口 开奖 面板已开时直接返回 奖池不足3张有多少上多少
        /// </summary>
        public void Open() {
            if (_config.IsOpen) {
                return;
            }

            if (_config.Pool.Count == 0) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 奖池为空 无法开奖!", BehaviourSign, entity.ObjConfig.Sign);
                return;
            }

            if (!BehaviourSigns.Require(_config.PanelPrefabSign, BehaviourSign, entity.ObjConfig?.Sign, nameof(UIPickOneOfThreeSettingData.PanelPrefabSign))) {
                return;
            }

            if (!TryBuildPanel()) {
                return;
            }

            RollPicks();
            FillPanel();
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _config.IsOpen = true;
        }

        /// <summary>
        /// 摸牌 随机3张不重复 奖池不够3张全上
        /// </summary>
        private void RollPicks() {
            _config.CurrentPicks.Clear();
            List<int> candidates = new List<int>();
            for (int i = 0; i < _config.Pool.Count; i++) {
                candidates.Add(i);
            }

            int count = Mathf.Min(pickCount, candidates.Count);
            for (int i = 0; i < count; i++) {
                int slot = Random.Range(0, candidates.Count);
                _config.CurrentPicks.Add(candidates[slot]);
                candidates.RemoveAt(slot);
            }
        }

        /// <summary>
        /// 面板实例化到主界面挂点下 跟 HUD 注入一个套路
        /// </summary>
        private bool TryBuildPanel() {
            ClosePanelInstance();

            if (!Flo.Instance.GetCurFlow(out Flow flow) || flow == null) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 当前流程为空 无法取主界面!", BehaviourSign, entity.ObjConfig.Sign);
                return false;
            }

            Comp mainUI = flow.GetUI();
            if (mainUI == null) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 当前流程主界面为空 请检查 Flow.GetUI()!", BehaviourSign, entity.ObjConfig.Sign);
                return false;
            }

            Transform mount = mainUI.transform;
            if (_config.MountSign != BehaviourSigns.Root) {
                Transform found = mainUI.Get<Transform>(_config.MountSign);
                if (found == null) {
                    LogUtil.LogErrorFormat("行为:{0} 实体:{1} 主界面未找到挂点:{2} 已挂根节点!", BehaviourSign, entity.ObjConfig.Sign, _config.MountSign);
                } else {
                    mount = found;
                }
            }

            panelInstance = Loader.LoadGo(_config.PanelPrefabSign, _config.PanelPrefabSign, mount, true);
            if (panelInstance == null) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 面板预制体加载失败:{2}", BehaviourSign, entity.ObjConfig.Sign, _config.PanelPrefabSign);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 贴纸条 UI 只管贴字 点了谁喊一嗓子 不认识卡的内容
        /// 面板契约(缺哪个报哪个 新人照着报错补预制体就行):
        /// 按钮 Card0/Card1/Card2 + 每张卡2个文本 Card{i}_Title/Card{i}_Desc
        /// 优先走预制体根上 Comp 标签 找不到再按子物体名兜底
        /// </summary>
        private void FillPanel() {
            Comp comp = panelInstance.GetComponent<Comp>();
            if (comp == null) {
                LogUtil.LogErrorFormat("行为:{0} 面板预制体根上没挂 Comp 脚本!请在 {1} 根上挂 Comp 再配标签 否则只能靠子物体名兜底",
                    BehaviourSign, _config.PanelPrefabSign);
            }

            List<string> missing = new List<string>();
            for (int i = 0; i < _config.CurrentPicks.Count; i++) {
                PickCardItem card = _config.Pool[_config.CurrentPicks[i]];
                string buttonSign = cardButtonSign + i;
                string titleSign = cardButtonSign + i + cardTitleSuffix;
                string descSign = cardButtonSign + i + cardDescSuffix;

                if (ResolveText(comp, titleSign) is TextMeshProUGUI title) {
                    title.text = card.Title;
                } else {
                    missing.Add(titleSign + "(标题文本)");
                }

                if (ResolveText(comp, descSign) is TextMeshProUGUI desc) {
                    desc.text = card.Description;
                } else {
                    missing.Add(descSign + "(描述文本)");
                }

                Button button = ResolveButton(comp, buttonSign);
                if (button == null) {
                    missing.Add(buttonSign + "(按钮 点不了这张卡!)");
                    continue;
                }

                int pickIndex = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Choose(pickIndex));
            }

            if (missing.Count > 0) {
                LogUtil.LogErrorFormat("行为:{0} 面板缺 {1} 个组件:{2}\n请打开预制体 {3}:1)按钮改名 Card0/1/2 2)每按钮下放2个TMP改名 CardX_Title/CardX_Desc 3)根上 Comp 里 Buttons/TextMeshProUGUIs 把引用拖进去",
                    BehaviourSign, missing.Count, string.Join(" / ", missing), _config.PanelPrefabSign);
            }
        }

        private TextMeshProUGUI ResolveText(Comp comp, string sign) {
            if (comp != null) {
                TextMeshProUGUI tmpText = comp.Get<TextMeshProUGUI>(sign);
                if (tmpText != null) {
                    return tmpText;
                }
            }

            foreach (TextMeshProUGUI tmp in panelInstance.GetComponentsInChildren<TextMeshProUGUI>(true)) {
                if (tmp.gameObject.name == sign) {
                    return tmp;
                }
            }

            return null;
        }

        private Button ResolveButton(Comp comp, string sign) {
            if (comp != null) {
                Button button = comp.Get<Button>(sign);
                if (button != null) {
                    return button;
                }
            }

            foreach (Button tmp in panelInstance.GetComponentsInChildren<Button>(true)) {
                if (tmp.gameObject.name == sign) {
                    return tmp;
                }
            }

            return null;
        }

        /// <summary>
        /// 点牌生效 效果逐条改数 单条失败不影响其余 生效后关面板恢复时间
        /// </summary>
        public void Choose(int pickIndex) {
            if (!_config.IsOpen || pickIndex < 0 || pickIndex >= _config.CurrentPicks.Count) {
                return;
            }

            PickCardItem card = _config.Pool[_config.CurrentPicks[pickIndex]];
            ApplyEffects(card);
            Close();
        }

        private void ApplyEffects(PickCardItem card) {
            if (card.Effects == null) {
                return;
            }

            foreach (PickCardEffect effect in card.Effects) {
                if (!BehaviourSigns.ResolveEntity(entity, BehaviourSign, nameof(PickCardEffect.TargetEntitySign), effect.TargetEntitySign, out Entity target)) {
                    continue;
                }

                if (!BehaviourSigns.Require(effect.ParamSign, BehaviourSign, target?.ObjConfig?.Sign, nameof(PickCardEffect.ParamSign))) {
                    continue;
                }

                switch (effect.ValueType) {
                    case ParamValueType.Bool:
                        if (Cond.Instance.TryGetData(target, effect.ParamSign, out BoolData boolData)) {
                            boolData.Bool = effect.BoolValue;
                        }
                        break;
                    case ParamValueType.Int:
                        if (Cond.Instance.TryGetData(target, effect.ParamSign, out IntData intData)) {
                            intData.Int = effect.Modify == DeathModifyType.Add ? intData.Int + effect.IntValue : effect.IntValue;
                        }
                        break;
                    case ParamValueType.Float:
                        if (Cond.Instance.TryGetData(target, effect.ParamSign, out FloatData floatData)) {
                            floatData.Float = effect.Modify == DeathModifyType.Add ? floatData.Float + effect.FloatValue : effect.FloatValue;
                        }
                        break;
                    case ParamValueType.String:
                        if (Cond.Instance.TryGetData(target, effect.ParamSign, out StringData stringData)) {
                            stringData.String = effect.StringValue;
                        }
                        break;
                    case ParamValueType.Vector3:
                        if (Cond.Instance.TryGetData(target, effect.ParamSign, out Vector3Data vector3Data)) {
                            vector3Data.Vector3 = effect.Modify == DeathModifyType.Add ? vector3Data.Vector3 + effect.Vector3Value : effect.Vector3Value;
                        }
                        break;
                    default:
                        LogUtil.LogErrorFormat("行为:{0} 不支持的参数类型:{1}", BehaviourSign, effect.ValueType);
                        break;
                }
            }
        }

        /// <summary>
        /// 关面板恢复时间 面板没开时只做兜底
        /// </summary>
        public void Close() {
            ClosePanelInstance();
            _config.CurrentPicks.Clear();
            if (_config.IsOpen) {
                Time.timeScale = prevTimeScale;
                _config.IsOpen = false;
            }
        }

        private void ClosePanelInstance() {
            if (panelInstance != null) {
                Object.Destroy(panelInstance);
                panelInstance = null;
            }
        }

        public override void Clear() {
            if (Game.instance != null) {
                Game.instance.OnUpdateEvent.RemoveListener(OnTestTick);
            }
            Close();
            DetachBehaviourData<UIPickOneOfThreeData>();
            base.Clear();
        }
    }
}
