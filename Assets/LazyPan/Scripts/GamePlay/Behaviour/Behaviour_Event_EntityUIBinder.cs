using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LazyPan {
    /// <summary>
    /// 行为 - 实体UI绑定
    /// 按配置给实体挂载UI预制体 血条 能量 头像等 数值通过实体Data标签驱动 不引入业务
    /// 配置来源 Setting/EntityUIBinderSetting 每个实体可绑定多个UI 每个UI可绑定多条数值
    /// </summary>
    public class Behaviour_Event_EntityUIBinder : Behaviour {
        private const string settingPath = "Setting/EntityUIBinderSetting";

        //config
        private EntityUIBinderData _uiBinderData;
        private EntityUIBinderData.EntityUIBinderConfig _config;

        //runtime
        private bool isConfigValid;
        private Transform cameraTran;
        private List<BoundUI> bounds = new List<BoundUI>();

        public Behaviour_Event_EntityUIBinder(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _uiBinderData = AttachBehaviourData<EntityUIBinderData>();
            
            EntityUIBinderSetting setting = Loader.LoadAsset<EntityUIBinderSetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out EntityUIBindSettingData settingData)) {
                return;
            }

            _config = _uiBinderData.Config;
            CopySetting(settingData);

            if (!CreateBounds()) {
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
        private void CopySetting(EntityUIBindSettingData settingData) {
            if (settingData.Items == null || settingData.Items.Count == 0) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 绑定UI列表为空!", BehaviourSign, entity.ObjConfig.Sign);
                return;
            }

            foreach (EntityUIBindItem item in settingData.Items) {
                EntityUIBindItem copy = new EntityUIBindItem() {
                    UIPrefabSign = item.UIPrefabSign,
                    AttachLabel = item.AttachLabel,
                    Offset = item.Offset,
                    Billboard = item.Billboard,
                };

                foreach (EntityUIDataBind bind in item.DataBinds) {
                    copy.DataBinds.Add(new EntityUIDataBind() {
                        ComponentSign = bind.ComponentSign,
                        ComponentType = bind.ComponentType,
                        Mode = bind.Mode,
                        DataSign = bind.DataSign,
                        MaxDataSign = bind.MaxDataSign,
                        Format = bind.Format,
                    });
                }

                _config.Items.Add(copy);
            }
        }

        /// <summary>
        /// 创建绑定UI 单条失败只跳过 不中断其余绑定
        /// </summary>
        private bool CreateBounds() {
            bool hasBound = false;
            foreach (EntityUIBindItem item in _config.Items) {
                if (CreateBound(item)) {
                    hasBound = true;
                }
            }

            if (!hasBound) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 没有任何UI绑定成功", BehaviourSign, entity.ObjConfig.Sign);
            }

            return hasBound;
        }

        private bool CreateBound(EntityUIBindItem item) {
            if (!BehaviourSigns.Require(item.UIPrefabSign, BehaviourSign, entity.ObjConfig?.Sign, nameof(EntityUIBindItem.UIPrefabSign))) {
                return false;
            }

            //先校验预制体存在 避免Addressables实例化失败导致空引用
            GameObject prefab = Loader.LoadAsset<GameObject>(AssetType.PREFAB, item.UIPrefabSign);
            if (prefab == null) {
                LogUtil.LogErrorFormat("行为:{0} UI绑定失败 未找到UI预制体:{1} 请在 Bundles/Prefabs 下创建", BehaviourSign, item.UIPrefabSign);
                return false;
            }

            Transform parent = BehaviourSigns.ResolveMountPoint(entity, BehaviourSign, item.AttachLabel);
            if (parent == null) {
                return false;
            }

            GameObject go = Loader.LoadGo(prefab.name, item.UIPrefabSign, parent, true);
            go.transform.localPosition = item.Offset;
            //旋转缩放保留预制体初始值 Billboard 模式由 OnUpdate 逐帧覆盖旋转

            BoundUI bound = new BoundUI() {
                Go = go,
                Tran = go.transform,
                Sign = item.UIPrefabSign,
                Billboard = item.Billboard,
            };
            ResolveDataBindings(bound, item);
            bounds.Add(bound);
            return true;
        }

        /// <summary>
        /// 解析数值绑定 优先走预制体Comp标签 兼容无Comp的预制体则按子物体名回退查找
        /// </summary>
        private void ResolveDataBindings(BoundUI bound, EntityUIBindItem item) {
            Comp comp = bound.Go.GetComponent<Comp>();
            foreach (EntityUIDataBind bind in item.DataBinds) {
                if (!BehaviourSigns.Require(bind.ComponentSign, BehaviourSign, entity.ObjConfig?.Sign, nameof(EntityUIDataBind.ComponentSign))) {
                    continue;
                }

                DataBinding binding = new DataBinding() {
                    ComponentType = bind.ComponentType,
                    Mode = bind.Mode,
                    DataSign = bind.DataSign,
                    MaxDataSign = bind.MaxDataSign,
                    Format = bind.Format,
                };

                switch (bind.ComponentType) {
                    case UIDataBindComponentType.Slider:
                        binding.Slider = ResolveSlider(bound, comp, bind.ComponentSign);
                        break;
                    case UIDataBindComponentType.Text:
                        binding.TMPText = ResolveText(bound, comp, bind.ComponentSign);
                        break;
                    case UIDataBindComponentType.Image:
                        binding.Image = ResolveImage(bound, comp, bind.ComponentSign);
                        break;
                }

                if (binding.Slider == null && binding.TMPText == null && binding.Image == null) {
                    LogUtil.LogErrorFormat("行为:{0} UI:{1} 未找到组件:{2} 类型:{3} 请检查预制体Comp标签或子物体名",
                        BehaviourSign, item.UIPrefabSign, bind.ComponentSign, bind.ComponentType);
                    continue;
                }

                bound.Bindings.Add(binding);
            }
        }

        private Slider ResolveSlider(BoundUI bound, Comp comp, string sign) {
            Slider slider = comp != null ? comp.Get<Slider>(sign) : null;
            if (slider != null) {
                return slider;
            }

            //回退 按子物体名查找 子物体名需与ComponentSign一致 当前UI_HealthBar下为Slider
            foreach (Slider tmp in bound.Go.GetComponentsInChildren<Slider>(true)) {
                if (tmp.gameObject.name == sign) {
                    return tmp;
                }
            }

            //仍未命中 取第一个Slider兜底
            Slider[] all = bound.Go.GetComponentsInChildren<Slider>(true);
            return all.Length > 0 ? all[0] : null;
        }

        private TextMeshProUGUI ResolveText(BoundUI bound, Comp comp, string sign) {
            TextMeshProUGUI tmpText = comp != null ? comp.Get<TextMeshProUGUI>(sign) : null;
            if (tmpText != null) {
                return tmpText;
            }

            foreach (TextMeshProUGUI tmp in bound.Go.GetComponentsInChildren<TextMeshProUGUI>(true)) {
                if (tmp.gameObject.name == sign) {
                    return tmp;
                }
            }

            return null;
        }

        private Image ResolveImage(BoundUI bound, Comp comp, string sign) {
            Image image = comp != null ? comp.Get<Image>(sign) : null;
            if (image != null) {
                return image;
            }

            foreach (Image tmp in bound.Go.GetComponentsInChildren<Image>(true)) {
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

            UpdateBillboard();
            UpdateDataBindings();
        }

        /// <summary>
        /// 面板朝向相机 相机实体缺失时下帧重试
        /// </summary>
        private void UpdateBillboard() {
            bool hasBillboard = false;
            foreach (BoundUI bound in bounds) {
                if (bound.Billboard) {
                    hasBillboard = true;
                    break;
                }
            }

            if (!hasBillboard) {
                return;
            }

            if (cameraTran == null) {
                Entity cameraEntity = Cond.Instance.GetCameraEntity();
                if (cameraEntity != null) {
                    cameraTran = Cond.Instance.Get<Transform>(cameraEntity, Label.CAMERA);
                }

                if (cameraTran == null) {
                    return;
                }
            }

            foreach (BoundUI bound in bounds) {
                if (bound.Billboard && bound.Tran != null) {
                    bound.Tran.rotation = cameraTran.rotation;
                }
            }
        }

        /// <summary>
        /// 数值刷新 走实体Data读取 与血量等行为零耦合
        /// </summary>
        private void UpdateDataBindings() {
            foreach (BoundUI bound in bounds) {
                foreach (DataBinding binding in bound.Bindings) {
                    ApplyBinding(binding);
                }
            }
        }

        private void ApplyBinding(DataBinding binding) {
            switch (binding.ComponentType) {
                case UIDataBindComponentType.Slider:
                    if (binding.Slider != null && TryReadValue(binding, out float sliderValue)) {
                        binding.Slider.value = sliderValue;
                    }
                    break;
                case UIDataBindComponentType.Image:
                    if (binding.Image != null && TryReadValue(binding, out float imageValue)) {
                        binding.Image.fillAmount = Mathf.Clamp01(imageValue);
                    }
                    break;
                case UIDataBindComponentType.Text:
                    if (binding.TMPText != null && TryReadText(binding, out string content)) {
                        binding.TMPText.text = content;
                    }
                    break;
            }
        }

        /// <summary>
        /// 读取数值 比例模式读主键与最大值键 直接模式只读主键
        /// </summary>
        private bool TryReadValue(DataBinding binding, out float value) {
            value = 0f;
            if (!TryReadNumber(binding, binding.DataSign, out float current)) {
                return false;
            }

            if (binding.Mode == UIDataBindValueMode.Direct) {
                value = current;
                return true;
            }

            if (!TryReadNumber(binding, binding.MaxDataSign, out float max)) {
                return false;
            }

            if (max <= 0f) {
                return false;
            }

            value = current / max;
            return true;
        }

        private bool TryReadNumber(DataBinding binding, string sign, out float value) {
            value = 0f;
            if (!BehaviourSigns.Require(sign, BehaviourSign, entity.ObjConfig?.Sign, nameof(EntityUIDataBind.DataSign))) {
                LogBindErrorOnce(binding, "数据标签不允许为空!");
                return false;
            }

            if (Cond.Instance.GetData<FloatData>(entity, sign, out FloatData floatData)) {
                value = floatData.Float;
                return true;
            }

            if (Cond.Instance.GetData<IntData>(entity, sign, out IntData intData)) {
                value = intData.Int;
                return true;
            }

            LogBindErrorOnce(binding, $"实体:{entity.ObjConfig.Sign} 未找到数值数据:{sign}");
            return false;
        }

        private bool TryReadText(DataBinding binding, out string content) {
            content = null;
            if (!BehaviourSigns.Require(binding.DataSign, BehaviourSign, entity.ObjConfig?.Sign, nameof(EntityUIDataBind.DataSign))) {
                LogBindErrorOnce(binding, "数据标签不允许为空!");
                return false;
            }

            if (Cond.Instance.GetData<FloatData>(entity, binding.DataSign, out FloatData floatData)) {
                content = floatData.Float.ToString(string.IsNullOrEmpty(binding.Format) ? "F0" : binding.Format);
                return true;
            }

            if (Cond.Instance.GetData<IntData>(entity, binding.DataSign, out IntData intData)) {
                content = intData.Int.ToString();
                return true;
            }

            if (Cond.Instance.GetData<StringData>(entity, binding.DataSign, out StringData stringData)) {
                content = stringData.String;
                return true;
            }

            if (Cond.Instance.GetData<BoolData>(entity, binding.DataSign, out BoolData boolData)) {
                content = boolData.Bool.ToString();
                return true;
            }

            LogBindErrorOnce(binding, $"实体:{entity.ObjConfig.Sign} 未找到数据:{binding.DataSign}");
            return false;
        }

        private void LogBindErrorOnce(DataBinding binding, string message) {
            if (binding.HasLoggedError) {
                return;
            }

            binding.HasLoggedError = true;
            LogUtil.LogErrorFormat("行为:{0} 数值绑定读取失败 {1}", BehaviourSign, message);
        }

        /// <summary>
        /// 获取已绑定UI的Comp 供外部系统交互 不依赖具体业务
        /// </summary>
        public bool TryGetBoundComp(string uiPrefabSign, out Comp comp) {
            comp = null;
            foreach (BoundUI bound in bounds) {
                if (bound.Go != null && bound.Sign == uiPrefabSign) {
                    comp = bound.Go.GetComponent<Comp>();
                    return comp != null;
                }
            }

            return false;
        }

        public override void Clear() {
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            foreach (BoundUI bound in bounds) {
                if (bound.Go != null) {
                    Object.Destroy(bound.Go);
                }
            }

            bounds.Clear();
            DetachBehaviourData<EntityUIBinderData>();
            base.Clear();
        }

        /// <summary>
        /// 运行时绑定实例 仅行为内部使用
        /// </summary>
        private class BoundUI {
            public GameObject Go;
            public Transform Tran;
            public string Sign;
            public bool Billboard;
            public List<DataBinding> Bindings = new List<DataBinding>();
        }

        /// <summary>
        /// 单条数值绑定运行时缓存
        /// </summary>
        private class DataBinding {
            public UIDataBindComponentType ComponentType;
            public UIDataBindValueMode Mode;
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
