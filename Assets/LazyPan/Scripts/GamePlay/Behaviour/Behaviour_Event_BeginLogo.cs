using UnityEngine;


namespace LazyPan {
    /// <summary>
    /// 行为 - 开头Logo
    /// 只做一件事: 挂载 Logo 界面并倒计时 结束时调同实体传送行为的内部请求 不写 Data 不配 ParamValue
    /// 倒计时镜像到自己实体的 LogoRemainTime 标签(只写不读 供传送等其他行为按配置读取判定)
    /// 跳转由同实体的传送流程行为执行 两行为仅经方法调用解耦
    /// </summary>
    public class Behaviour_Event_BeginLogo : Behaviour {
        private const string settingPath = "Setting/BeginLogoSetting";
        public const string REMAIN_LABEL = "LogoRemainTime";

        private BeginLogoData _beginLogoEntityData;
        private BeginLogoData.BeginLogoConfig _config;

        private float delayDeployTime;
        private bool isRunning;

        //data 倒计时镜像 其他行为只读此标签做判定
        private FloatData _remainData;

        public Behaviour_Event_BeginLogo(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _beginLogoEntityData = AttachBehaviourData<BeginLogoData>();

            BeginLogoSetting setting = Loader.LoadAsset<BeginLogoSetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out BeginLogoSettingData settingData)) {
                return;
            }

            _config = _beginLogoEntityData.Config;
            delayDeployTime = Mathf.Max(settingData.LogoContinueTime, 0f);
            isRunning = true;

            if (!Cond.Instance.TryGetData(entity, REMAIN_LABEL, out _remainData)) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 倒计时标签初始化失败!", BehaviourSign, entity.ObjConfig.Sign);
                return;
            }

            _remainData.Float = delayDeployTime;

            InitBinding(settingData);

            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        private void InitBinding(BeginLogoSettingData settingData) {
            if (Flo.Instance.GetCurFlow(out Flow outFlow)) {
                Comp ui = outFlow.GetUI();
                Transform root = Cond.Instance.Get<Transform>(ui, "Root");
                GameObject go = Loader.LoadGo("UI_Logo", settingData.UIChildPrefabSign, root, true);
                go.transform.localPosition = Vector3.zero;
            }
        }

        public override void DelayedExecute() {
        }

        private void OnUpdate() {
            if (!isRunning) return;
            if (delayDeployTime > 0) {
                delayDeployTime -= Time.deltaTime;
                if (_remainData != null) {
                    _remainData.Float = Mathf.Max(delayDeployTime, 0f);
                }
            } else {
                isRunning = false;
                if (_remainData != null) {
                    _remainData.Float = 0f;
                }

                // 倒计时结束 调同实体传送行为的内部请求 不写 Data 由传送行为内部消费
                if (entity != null && BehaviourRegister.GetBehaviour(entity, out Behaviour_Event_TeleportFlow teleport)) {
                    teleport.RequestTeleport();
                }
            }
        }

        public override void Clear() {
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            DetachBehaviourData<BeginLogoData>();
            base.Clear();
        }
    }
}