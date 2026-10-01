using UnityEngine;


namespace LazyPan {
    public class Behaviour_Event_BeginLogo : Behaviour {
        private const string settingPath = "Setting/BeginLogoSetting";

        private BeginLogoData _beginLogoEntityData;
        private BeginLogoData.BeginLogoConfig _config;

        private float delayDeployTime;
        private bool isRunning;

        public Behaviour_Event_BeginLogo(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _beginLogoEntityData = entity.Prefab.AddComponent<BeginLogoData>();
            _beginLogoEntityData.EntityID = entity.ID;

            BeginLogoSetting setting = Loader.LoadAsset<BeginLogoSetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out BeginLogoSettingData settingData)) {
                return;
            }

            _config = _beginLogoEntityData.Config;
            _config.EndJumpToScene = settingData.EndJumpToScene;
            delayDeployTime = settingData.LogoContinueTime;
            isRunning = true;

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
            } else {
                if (Flo.Instance.GetCurFlow(out Flow flow)) {
                    flow.Next(_config.EndJumpToScene);
                    isRunning = false;
                }
            }
        }

        public override void Clear() {
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            base.Clear();
        }
    }
}