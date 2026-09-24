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
        /// <summary>屏幕UI节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【屏幕UI】管屏幕上挂几块 HUD，数值跟着实体走。\n" +
            "— 配置参数（UIStatusDisplaySetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>Displays</color>：UI 块列表，一块一行\n" +
            "— 每块怎么填 —\n" +
            "- <color=#FFD54F>UIName</color>：挂到哪个屏幕UI，空=当前流程主界面\n" +
            "- <color=#FFD54F>UIPrefabSign</color>：用哪个 HUD，空=直接绑主界面\n" +
            "- <color=#FFD54F>MountSign</color>：挂在哪个点上，Root=主界面根\n" +
            "- <color=#FFD54F>InstanceSign</color>：实例名，空=用预制体名\n" +
            "- <color=#FFD54F>DataBinds</color>：数值绑定，一条绑一个数\n" +
            "— 数值绑定每条怎么填 —\n" +
            "- <color=#FFD54F>ComponentSign</color>+<color=#FFD54F>ComponentType</color>：UI上哪个零件\n" +
            "- <color=#FFD54F>Mode</color>：比例=当前/最大，直给=当前值\n" +
            "- <color=#FFD54F>SourceEntitySign</color>：取谁的数，Self=自己\n" +
            "- <color=#FFD54F>DataSign</color>+<color=#FFD54F>MaxDataSign</color>：哪个数，如 Health\n" +
            "- <color=#FFD54F>Format</color>：显示格式，空=整数";
        private const string settingPath = "Setting/UIStatusDisplaySetting";

        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is UIStatusDisplaySettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (c.Displays == null || c.Displays.Count == 0) {
                red.Add("一块 UI 没挂，挂了白挂");
                return;
            }

            for (int i = 0; i < c.Displays.Count; i++) {
                var d = c.Displays[i];
                if (d == null) {
                    red.Add($"第{i + 1}块是空行，删掉");
                    continue;
                }

                if (d.DataBinds == null || d.DataBinds.Count == 0) {
                    yellow.Add($"第{i + 1}块没绑数，纯摆设");
                    continue;
                }

                for (int j = 0; j < d.DataBinds.Count; j++) {
                    var b = d.DataBinds[j];
                    if (b == null) {
                        red.Add($"第{i + 1}块第{j + 1}条绑定是空行，删掉");
                        continue;
                    }

                    if (string.IsNullOrEmpty(b.ComponentSign)) {
                        red.Add($"第{i + 1}块第{j + 1}条绑定没填哪个零件");
                    }

                    if (string.IsNullOrEmpty(b.DataSign)) {
                        red.Add($"第{i + 1}块第{j + 1}条绑定没填读哪个数");
                    }

                    if (string.IsNullOrEmpty(b.SourceEntitySign)) {
                        yellow.Add($"第{i + 1}块第{j + 1}条绑定没填取谁的数");
                    }
                }
            }
        }

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
                    UIPrefabSign = item.UIPrefabSign,
                    MountSign = item.MountSign,
                    InstanceSign = item.InstanceSign,
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

            //注入模式: 实例化HUD预制体到主界面挂点下 组件从预制体上拿
            if (!string.IsNullOrEmpty(item.UIPrefabSign)) {
                return ResolveInjectedDisplay(item, uiComp);
            }

            //老路: 直接绑主界面Comp
            BoundDisplay display = new BoundDisplay() {
                Comp = uiComp,
                BindRoot = uiComp.gameObject,
                MountUI = uiComp,
                UIName = string.IsNullOrEmpty(item.UIName) ? uiComp.gameObject.name : item.UIName,
                IsMainUI = string.IsNullOrEmpty(item.UIName),
                Item = item,
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
        /// 注入模式 和头顶血条同套路 只是挂载点从实体挂点换成主界面挂点
        /// 挂点从主界面Comp按 MountSign 取 Root=主界面根 拿不到回退主界面根并报错
        /// </summary>
        private bool ResolveInjectedDisplay(UIStatusDisplayItem item, Comp uiComp) {
            Transform mount = ResolveMount(uiComp, item.MountSign);
            if (mount == null) {
                return false;
            }

            string instanceName = string.IsNullOrEmpty(item.InstanceSign) ? StripPrefabName(item.UIPrefabSign) : item.InstanceSign;
            GameObject go = null;
            try {
                go = Loader.LoadGo(instanceName, item.UIPrefabSign, mount, true);
            } catch (System.Exception e) {
                LogUtil.LogErrorFormat("行为:{0} HUD预制体:{1} 实例化失败:{2}", BehaviourSign, item.UIPrefabSign, e.Message);
                return false;
            }

            if (go == null) {
                LogUtil.LogErrorFormat("行为:{0} HUD预制体:{1} 实例化返回空 请检查地址!", BehaviourSign, item.UIPrefabSign);
                return false;
            }

            //挂点局部归零 避免带偏移 用预制体自己的 anchoredPosition
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            BoundDisplay display = new BoundDisplay() {
                Comp = go.GetComponent<Comp>(),
                BindRoot = go,
                MountUI = uiComp,
                HUDInstance = go,
                UIName = instanceName,
                IsMainUI = string.IsNullOrEmpty(item.UIName),
                Item = item,
            };
            ResolveDataBindings(display, item);
            if (display.Bindings.Count == 0) {
                Object.Destroy(go);
                LogUtil.LogErrorFormat("行为:{0} HUD预制体:{1} 没有任何数值绑定成功 已销毁实例", BehaviourSign, item.UIPrefabSign);
                return false;
            }

            displays.Add(display);
            return true;
        }

        private Transform ResolveMount(Comp uiComp, string mountSign) {
            if (string.IsNullOrEmpty(mountSign) || mountSign == BehaviourSigns.Root) {
                return uiComp.transform;
            }

            Transform mount = uiComp.Get<Transform>(mountSign);
            if (mount != null) {
                return mount;
            }

            //回退 按子物体名找
            Transform found = uiComp.transform.Find(mountSign);
            if (found != null) {
                return found;
            }

            LogUtil.LogErrorFormat("行为:{0} 主界面:{1} 未找到挂点:{2} 已回退主界面根 请检查主界面Comp标签", BehaviourSign, uiComp.gameObject.name, mountSign);
            return uiComp.transform;
        }

        private string StripPrefabName(string prefabSign) {
            if (string.IsNullOrEmpty(prefabSign)) {
                return "HUD";
            }

            int slash = prefabSign.LastIndexOf('/');
            return slash >= 0 ? prefabSign.Substring(slash + 1) : prefabSign;
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
                        binding.Slider = ResolveSlider(display.Comp, display.BindRoot, bind.ComponentSign);
                        break;
                    case UIDataBindComponentType.Text:
                        binding.TMPText = ResolveText(display.Comp, display.BindRoot, bind.ComponentSign);
                        break;
                    case UIDataBindComponentType.Image:
                        binding.Image = ResolveImage(display.Comp, display.BindRoot, bind.ComponentSign);
                        break;
                }

                if (binding.Slider == null && binding.TMPText == null && binding.Image == null) {
                    LogUtil.LogErrorFormat("行为:{0} 屏幕UI:{1} 未找到组件:{2} 类型:{3} 请检查HUD预制体或屏幕UI的Comp标签或子物体名",
                        BehaviourSign, display.UIName, bind.ComponentSign, bind.ComponentType);
                    continue;
                }

                display.Bindings.Add(binding);
            }
        }

        /// <summary>
        /// 老路切换主界面后按新界面重查组件引用 注入模式不走这里 走整块重建
        /// </summary>
        private void RebindDisplay(BoundDisplay display) {
            foreach (DataBinding binding in display.Bindings) {
                binding.Slider = null;
                binding.TMPText = null;
                binding.Image = null;

                switch (binding.ComponentType) {
                    case UIDataBindComponentType.Slider:
                        binding.Slider = ResolveSlider(display.Comp, display.BindRoot, binding.ComponentSign);
                        break;
                    case UIDataBindComponentType.Text:
                        binding.TMPText = ResolveText(display.Comp, display.BindRoot, binding.ComponentSign);
                        break;
                    case UIDataBindComponentType.Image:
                        binding.Image = ResolveImage(display.Comp, display.BindRoot, binding.ComponentSign);
                        break;
                }

                binding.HasLoggedError = false;
            }
        }

        /// <summary>
        /// 优先走Comp标签 Comp为空(预制体没挂Comp)则按子物体名回退查找
        /// </summary>
        private Slider ResolveSlider(Comp uiComp, GameObject root, string sign) {
            if (uiComp != null) {
                Slider slider = uiComp.Get<Slider>(sign);
                if (slider != null) {
                    return slider;
                }
            }

            if (root == null) {
                return null;
            }

            //回退 按子物体名查找
            foreach (Slider tmp in root.GetComponentsInChildren<Slider>(true)) {
                if (tmp.gameObject.name == sign) {
                    return tmp;
                }
            }

            //仍未命中 取第一个Slider兜底
            Slider[] all = root.GetComponentsInChildren<Slider>(true);
            return all.Length > 0 ? all[0] : null;
        }

        private TextMeshProUGUI ResolveText(Comp uiComp, GameObject root, string sign) {
            if (uiComp != null) {
                TextMeshProUGUI tmpText = uiComp.Get<TextMeshProUGUI>(sign);
                if (tmpText != null) {
                    return tmpText;
                }
            }

            if (root == null) {
                return null;
            }

            foreach (TextMeshProUGUI tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true)) {
                if (tmp.gameObject.name == sign) {
                    return tmp;
                }
            }

            return null;
        }

        private Image ResolveImage(Comp uiComp, GameObject root, string sign) {
            if (uiComp != null) {
                Image image = uiComp.Get<Image>(sign);
                if (image != null) {
                    return image;
                }
            }

            if (root == null) {
                return null;
            }

            foreach (Image tmp in root.GetComponentsInChildren<Image>(true)) {
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
        /// UIName留空的块每帧重取主界面 主界面变了整块重建 老路重绑组件 注入路销毁旧实例重建新实例
        /// </summary>
        private void RefreshDisplays() {
            for (int i = displays.Count - 1; i >= 0; i--) {
                BoundDisplay display = displays[i];
                if (display.IsMainUI) {
                    Comp mainUI = ResolveUI(null);
                    if (mainUI == null) {
                        continue;
                    }

                    if (mainUI != display.MountUI) {
                        DestroyInstance(display);
                        displays.RemoveAt(i);
                        ResolveDisplay(display.Item);
                        continue;
                    }

                    if (display.HUDInstance == null) {
                        RebindDisplay(display);
                    }
                }

                if (display.BindRoot == null) {
                    continue;
                }

                foreach (DataBinding binding in display.Bindings) {
                    ApplyBinding(binding);
                }
            }
        }

        private void DestroyInstance(BoundDisplay display) {
            if (display.HUDInstance != null) {
                Object.Destroy(display.HUDInstance);
                display.HUDInstance = null;
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

            if (EntityAttrRegistry.TryGetNumber(source, sign, out float regValue)) {
                value = regValue;
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

            if (EntityAttrRegistry.TryGetNumber(source, binding.DataSign, out float regNum)) {
                content = regNum.ToString(string.IsNullOrEmpty(binding.Format) ? "F0" : binding.Format);
                return true;
            }

            if (EntityAttrRegistry.TryGetText(source, binding.DataSign, out string regText)) {
                content = regText;
                return true;
            }

            if (EntityAttrRegistry.TryGetBool(source, binding.DataSign, out bool regBool)) {
                content = regBool.ToString();
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
            foreach (BoundDisplay display in displays) {
                DestroyInstance(display);
            }
            displays.Clear();
            DetachBehaviourData<UIStatusDisplayData>();
            base.Clear();
        }

        /// <summary>
        /// 运行时屏幕UI块 IsMainUI=true 表示 UIName 留空 每帧重取主界面
        /// 老路 Comp=主界面 BindRoot=主界面物体 注入路 Comp=预制体实例的Comp BindRoot=实例物体 HUDInstance=实例(释放用)
        /// </summary>
        private class BoundDisplay {
            public Comp Comp;
            public GameObject BindRoot;
            public Comp MountUI;
            public GameObject HUDInstance;
            public string UIName;
            public bool IsMainUI;
            public UIStatusDisplayItem Item;
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
