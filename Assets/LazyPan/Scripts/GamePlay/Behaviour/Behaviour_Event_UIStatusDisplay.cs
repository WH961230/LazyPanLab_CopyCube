using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LazyPan {
    /// <summary>
    /// 行为 - 屏幕状态展示
    /// 只做一件事: 把任意实体 Data 刷到屏幕 UI 上 只读不写 不改任何数值
    /// 跟 EntityUIBinder 是两兄弟: EntityUIBinder 是挂头顶血条(世界坐标 跟实体走)
    /// 这个是刷主界面 HUD(屏幕坐标 比如 UI_SceneC 显示玩家血量/等级/经验/波次)
    /// 数值归 ParamValue / StageProgress / Death 管 本行为不引入业务
    /// 配置来源 Setting/UIStatusDisplaySetting 一个实体一条 里面可配多个屏幕 UI 块
    /// </summary>
    public class Behaviour_Event_UIStatusDisplay : Behaviour {
        private const string settingPath = "Setting/UIStatusDisplaySetting";

        //config
        private UIStatusDisplayData _displayData;
        private UIStatusDisplayData.UIStatusDisplayConfig _config;

        //runtime
        private bool isConfigValid;
        private List<BoundDisplay> displays = new List<BoundDisplay>();

        public Behaviour_Event_UIStatusDisplay(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _displayData = AttachBehaviourData<UIStatusDisplayData>();

            UIStatusDisplaySetting setting = Loader.LoadAsset<UIStatusDisplaySetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out UIStatusDisplaySettingData settingData)) {
                return;
            }

            _config = _displayData.Config;
            CopySetting(settingData);

            if (!ResolveDisplays()) {
                return;
            }

            isConfigValid = true;
            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {

        }

        /// <summary>
        /// 配置拷贝到实体Data 与配置资产解耦
        /// </summary>
        private void CopySetting(UIStatusDisplaySettingData settingData) {
            _config.Displays.Clear();
            if (settingData.Displays == null || settingData.Displays.Count == 0) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 屏幕UI块列表为空!", BehaviourSign, entity.ObjConfig.Sign);
                return;
            }

            foreach (UIStatusDisplayItem item in settingData.Displays) {
                if (item == null) {
                    continue;
                }

                UIStatusDisplayItem copy = new UIStatusDisplayItem() {
                    UIName = item.UIName,
                };

                if (item.DataBinds != null) {
                    foreach (UIStatusDisplayBind bind in item.DataBinds) {
                        if (bind == null) {
                            continue;
                        }

                        copy.DataBinds.Add(new UIStatusDisplayBind() {
                            ComponentSign = bind.ComponentSign,
                            ComponentType = bind.ComponentType,
                            Mode = bind.Mode,
                            SourceEntitySign = bind.SourceEntitySign,
                            DataSign = bind.DataSign,
                            MaxDataSign = bind.MaxDataSign,
                            Format = bind.Format,
                        });
                    }
                }

                _config.Displays.Add(copy);
            }
        }

        /// <summary>
        /// 解析屏幕UI块 单块失败只跳过 不中断其余块
        /// </summary>
        private bool ResolveDisplays() {
            bool hasDisplay = false;
            foreach (UIStatusDisplayItem item in _config.Displays) {
                if (ResolveDisplay(item)) {
                    hasDisplay = true;
                }
            }

            if (!hasDisplay) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 没有任何屏幕UI块解析成功", BehaviourSign, entity.ObjConfig.Sign);
            }

            return hasDisplay;
        }

        private bool ResolveDisplay(UIStatusDisplayItem item) {
            Comp uiComp = ResolveUI(item.UIName);
            if (uiComp == null) {
                return false;
            }

            BoundDisplay display = new BoundDisplay() {
                Comp = uiComp,
                UIName = string.IsNullOrEmpty(item.UIName) ? uiComp.gameObject.name : item.UIName,
                IsMainUI = string.IsNullOrEmpty(item.UIName),
            };
            ResolveDataBindings(display, item);
            if (display.Bindings.Count == 0) {
                LogUtil.LogErrorFormat("行为:{0} 屏幕UI:{1} 没有任何数值绑定成功", BehaviourSign, display.UIName);
                return false;
            }

            displays.Add(display);
            return true;
        }

        /// <summary>
        /// 解析屏幕UI UIName留空=取当前流程主界面 填了按名字取
        /// 流程切换时主界面会变 留空的块每帧重取 不缓存死
        /// </summary>
        private Comp ResolveUI(string uiName) {
            if (string.IsNullOrEmpty(uiName)) {
                if (!Flo.Instance.GetCurFlow(out Flow flow) || flow == null) {
                    LogUtil.LogErrorFormat("行为:{0} 实体:{1} 当前流程为空 无法取主界面!", BehaviourSign, entity.ObjConfig.Sign);
                    return null;
                }

                Comp mainUI = flow.GetUI();
                if (mainUI == null) {
                    LogUtil.LogErrorFormat("行为:{0} 实体:{1} 当前流程主界面为空 请检查 Flow.GetUI()!", BehaviourSign, entity.ObjConfig.Sign);
                    return null;
                }

                return mainUI;
            }

            Comp namedUI = UI.Instance.Get(uiName);
            if (namedUI == null) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 未找到屏幕UI:{2} 请检查 UIConfig.csv 与 Preload!", BehaviourSign, entity.ObjConfig.Sign, uiName);
                return null;
            }

            return namedUI;
        }

        /// <summary>
        /// 解析数值绑定 优先走屏幕UI的Comp标签 兼容无Comp则按子物体名回退查找
        /// 主界面切换后重解时 组件引用按新界面重查 缓存的报错标记清零允许重试
        /// </summary>
        private void ResolveDataBindings(BoundDisplay display, UIStatusDisplayItem item) {
            if (item.DataBinds == null) {
                return;
            }

            foreach (UIStatusDisplayBind bind in item.DataBinds) {
                if (!BehaviourSigns.Require(bind.ComponentSign, BehaviourSign, entity.ObjConfig?.Sign, nameof(UIStatusDisplayBind.ComponentSign))) {
                    continue;
                }

                if (!BehaviourSigns.Require(bind.SourceEntitySign, BehaviourSign, entity.ObjConfig?.Sign, nameof(UIStatusDisplayBind.SourceEntitySign))) {
                    continue;
                }

                if (!BehaviourSigns.Require(bind.DataSign, BehaviourSign, bind.SourceEntitySign, nameof(UIStatusDisplayBind.DataSign))) {
                    continue;
                }

                DataBinding binding = new DataBinding() {
                    ComponentSign = bind.ComponentSign,
                    ComponentType = bind.ComponentType,
                    Mode = bind.Mode,
                    SourceEntitySign = bind.SourceEntitySign,
                    DataSign = bind.DataSign,
                    MaxDataSign = bind.MaxDataSign,
                    Format = bind.Format,
                };

                switch (bind.ComponentType) {
                    case UIDataBindComponentType.Slider:
                        binding.Slider = ResolveSlider(display.Comp, bind.ComponentSign);
                        break;
                    case UIDataBindComponentType.Text:
                        binding.TMPText = ResolveText(display.Comp, bind.ComponentSign);
                        break;
                    case UIDataBindComponentType.Image:
                        binding.Image = ResolveImage(display.Comp, bind.ComponentSign);
                        break;
                }

                if (binding.Slider == null && binding.TMPText == null && binding.Image == null) {
                    LogUtil.LogErrorFormat("行为:{0} 屏幕UI:{1} 未找到组件:{2} 类型:{3} 请检查屏幕UI预制体Comp标签或子物体名",
                        BehaviourSign, display.UIName, bind.ComponentSign, bind.ComponentType);
                    continue;
                }

                display.Bindings.Add(binding);
            }
        }

        /// <summary>
        /// 切换主界面后按新界面重查组件引用
        /// </summary>
        private void RebindDisplay(BoundDisplay display) {
            foreach (DataBinding binding in display.Bindings) {
                binding.Slider = null;
                binding.TMPText = null;
                binding.Image = null;

                switch (binding.ComponentType) {
                    case UIDataBindComponentType.Slider:
                        binding.Slider = ResolveSlider(display.Comp, binding.ComponentSign);
                        break;
                    case UIDataBindComponentType.Text:
                        binding.TMPText = ResolveText(display.Comp, binding.ComponentSign);
                        break;
                    case UIDataBindComponentType.Image:
                        binding.Image = ResolveImage(display.Comp, binding.ComponentSign);
                        break;
                }

                binding.HasLoggedError = false;
            }
        }

        private Slider ResolveSlider(Comp uiComp, string sign) {
            if (uiComp == null) {
                return null;
            }

            Slider slider = uiComp.Get<Slider>(sign);
            if (slider != null) {
                return slider;
            }

            //回退 按子物体名查找
            foreach (Slider tmp in uiComp.GetComponentsInChildren<Slider>(true)) {
                if (tmp.gameObject.name == sign) {
                    return tmp;
                }
            }

            //仍未命中 取第一个Slider兜底
            Slider[] all = uiComp.GetComponentsInChildren<Slider>(true);
            return all.Length > 0 ? all[0] : null;
        }

        private TextMeshProUGUI ResolveText(Comp uiComp, string sign) {
            if (uiComp == null) {
                return null;
            }

            TextMeshProUGUI tmpText = uiComp.Get<TextMeshProUGUI>(sign);
            if (tmpText != null) {
                return tmpText;
            }

            foreach (TextMeshProUGUI tmp in uiComp.GetComponentsInChildren<TextMeshProUGUI>(true)) {
                if (tmp.gameObject.name == sign) {
                    return tmp;
                }
            }

            return null;
        }

        private Image ResolveImage(Comp uiComp, string sign) {
            if (uiComp == null) {
                return null;
            }

            Image image = uiComp.Get<Image>(sign);
            if (image != null) {
                return image;
            }

            foreach (Image tmp in uiComp.GetComponentsInChildren<Image>(true)) {
                if (tmp.gameObject.name == sign) {
                    return tmp;
                }
            }

            return null;
        }

        private void OnUpdate() {
            if (!isConfigValid) {
                return;
            }

            RefreshDisplays();
        }

        /// <summary>
        /// 数值刷新 只读不写 与血量经验波次等行为零耦合
        /// UIName留空的块每帧重取主界面 流程切换后组件引用过期则按新界面重解绑定
        /// </summary>
        private void RefreshDisplays() {
            foreach (BoundDisplay display in displays) {
                if (display.IsMainUI) {
                    Comp mainUI = ResolveUI(null);
                    if (mainUI == null) {
                        continue;
                    }

                    if (mainUI != display.Comp) {
                        display.Comp = mainUI;
                        RebindDisplay(display);
                    }
                }

                if (display.Comp == null) {
                    continue;
                }

                foreach (DataBinding binding in display.Bindings) {
                    ApplyBinding(binding);
                }
            }
        }

        private void ApplyBinding(DataBinding binding) {
            if (!TryResolveSourceEntity(binding, out Entity source)) {
                return;
            }

            switch (binding.ComponentType) {
                case UIDataBindComponentType.Slider:
                    if (binding.Slider != null && TryReadValue(source, binding, out float sliderValue)) {
                        binding.Slider.value = sliderValue;
                    }
                    break;
                case UIDataBindComponentType.Image:
                    if (binding.Image != null && TryReadValue(source, binding, out float imageValue)) {
                        binding.Image.fillAmount = Mathf.Clamp01(imageValue);
                    }
                    break;
                case UIDataBindComponentType.Text:
                    if (binding.TMPText != null && TryReadText(source, binding, out string content)) {
                        binding.TMPText.text = content;
                    }
                    break;
            }
        }

        /// <summary>
        /// 解析取数实体 Self=自己 其他按Sign读对方 不缓存 目标销毁下帧重解
        /// </summary>
        private bool TryResolveSourceEntity(DataBinding binding, out Entity source) {
            return BehaviourSigns.ResolveEntity(entity, BehaviourSign, nameof(UIStatusDisplayBind.SourceEntitySign), binding.SourceEntitySign, out source);
        }

        /// <summary>
        /// 读取数值 比例模式读主键与最大值键 直接模式只读主键
        /// </summary>
        private bool TryReadValue(Entity source, DataBinding binding, out float value) {
            value = 0f;
            if (!TryReadNumber(source, binding, binding.DataSign, out float current)) {
                return false;
            }

            if (binding.Mode == UIDataBindValueMode.Direct) {
                value = current;
                return true;
            }

            if (!TryReadNumber(source, binding, binding.MaxDataSign, out float max)) {
                return false;
            }

            if (max <= 0f) {
                return false;
            }

            value = current / max;
            return true;
        }

        private bool TryReadNumber(Entity source, DataBinding binding, string sign, out float value) {
            value = 0f;
            if (!BehaviourSigns.Require(sign, BehaviourSign, source?.ObjConfig?.Sign, nameof(UIStatusDisplayBind.DataSign))) {
                LogBindErrorOnce(binding, "数据标签不允许为空!");
                return false;
            }

            if (Cond.Instance.GetData<FloatData>(source, sign, out FloatData floatData)) {
                value = floatData.Float;
                return true;
            }

            if (Cond.Instance.GetData<IntData>(source, sign, out IntData intData)) {
                value = intData.Int;
                return true;
            }

            LogBindErrorOnce(binding, $"实体:{source?.ObjConfig?.Sign} 未找到数值数据:{sign}");
            return false;
        }

        private bool TryReadText(Entity source, DataBinding binding, out string content) {
            content = null;
            if (Cond.Instance.GetData<FloatData>(source, binding.DataSign, out FloatData floatData)) {
                content = floatData.Float.ToString(string.IsNullOrEmpty(binding.Format) ? "F0" : binding.Format);
                return true;
            }

            if (Cond.Instance.GetData<IntData>(source, binding.DataSign, out IntData intData)) {
                content = intData.Int.ToString();
                return true;
            }

            if (Cond.Instance.GetData<StringData>(source, binding.DataSign, out StringData stringData)) {
                content = stringData.String;
                return true;
            }

            if (Cond.Instance.GetData<BoolData>(source, binding.DataSign, out BoolData boolData)) {
                content = boolData.Bool.ToString();
                return true;
            }

            LogBindErrorOnce(binding, $"实体:{source?.ObjConfig?.Sign} 未找到数据:{binding.DataSign}");
            return false;
        }

        private void LogBindErrorOnce(DataBinding binding, string message) {
            if (binding.HasLoggedError) {
                return;
            }

            binding.HasLoggedError = true;
            LogUtil.LogErrorFormat("行为:{0} 屏幕状态读取失败 {1}", BehaviourSign, message);
        }

        public override void Clear() {
            if (Game.instance != null) {
                Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            }
            displays.Clear();
            DetachBehaviourData<UIStatusDisplayData>();
            base.Clear();
        }

        /// <summary>
        /// 运行时屏幕UI块 IsMainUI=true 表示 UIName 留空 每帧重取主界面
        /// </summary>
        private class BoundDisplay {
            public Comp Comp;
            public string UIName;
            public bool IsMainUI;
            public List<DataBinding> Bindings = new List<DataBinding>();
        }

        /// <summary>
        /// 单条数值绑定运行时缓存
        /// </summary>
        private class DataBinding {
            public string ComponentSign;
            public UIDataBindComponentType ComponentType;
            public UIDataBindValueMode Mode;
            public string SourceEntitySign;
            public string DataSign;
            public string MaxDataSign;
            public string Format;
            public Slider Slider;
            public TextMeshProUGUI TMPText;
            public Image Image;
            public bool HasLoggedError;
        }
    }
}
