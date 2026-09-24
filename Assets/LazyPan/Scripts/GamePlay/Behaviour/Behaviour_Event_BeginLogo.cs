using UnityEngine;


namespace LazyPan {
    /// <summary>
    /// 行为 - 开头Logo
    /// 只做一件事: 挂载 Logo 界面并倒计时 结束时调同实体传送行为的内部请求 不写 Data 不配 ParamValue
    /// 倒计时镜像到自己实体的 LogoRemainTime 标签(只写不读 供传送等其他行为按配置读取判定)
    /// 跳转由同实体的传送流程行为执行 两行为仅经方法调用解耦
    /// </summary>
    public class Behaviour_Event_BeginLogo : Behaviour {
        /// <summary>开头Logo节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【开头Logo】管开场播几秒 Logo，播完自动跳下一步。\n" +
            "— 配置参数（BeginLogoSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>UIParentPrefabSign</color>：Logo 挂在哪个界面上，如 UI/UI_SceneA\n" +
            "- <color=#FFD54F>UIChildPrefabSign</color>：挂哪个 Logo，如 UI/UI_Logo\n" +
            "- <color=#FFD54F>LogoContinueTime</color>：播几秒，建议 3~8，0=一闪而过";
        private const string settingPath = "Setting/BeginLogoSetting";

        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is BeginLogoSettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (string.IsNullOrEmpty(c.UIParentPrefabSign)) {
                red.Add("没填挂在哪个界面上，Logo 不显示");
            }

            if (string.IsNullOrEmpty(c.UIChildPrefabSign)) {
                red.Add("没填挂哪个 Logo，Logo 不显示");
            }

            if (c.LogoContinueTime < 0f) {
                yellow.Add("播放时间是负数，会当 0 用");
            } else if (c.LogoContinueTime == 0f) {
                yellow.Add("播放时间=0，一闪而过");
            }
        }
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