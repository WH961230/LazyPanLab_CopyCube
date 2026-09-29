using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 延时实体生成
    /// 积木 埋头产怪 不感知波次 也不感知其他行为
    /// 自有节奏: 无触发档案时按 GenerateType/IntervalTime/GenerateEntitySign 定时产
    /// 触发档案: 通用监听任意 IntData/FloatData/BoolData 标签 满足条件按档案产
    /// 仅写自身 Data: LivingCount  供其他积木按需读取 不强求
    /// </summary>
    public class Behaviour_Event_DelayGenerateEntity : Behaviour {
        /// <summary>延时生成节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【延时生成】管一个产怪点按节奏产怪，产谁产几个全在这里定。\n" +
            "— 配置参数（DelayGenerateEntitySetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>GenerateType</color>：没触发时 Once=只产一次，Loop=循环产\n" +
            "- <color=#FFD54F>IntervalTime</color>：没触发时隔几秒产一个\n" +
            "- <color=#FFD54F>GenerateEntitySign</color>：没触发时产谁\n" +
            "- <color=#FFD54F>Profiles</color>：触发档案，按顺序谁先满足用谁；空着就只按默认节奏产\n" +
            "— 档案里每条怎么填 —\n" +
            "- <color=#FFD54F>WatchSign</color>+<color=#FFD54F>WatchEntitySign</color>：盯着谁的哪个数看，如波次实体的 WaveIndex\n" +
            "- <color=#FFD54F>Compare</color>+<color=#FFD54F>WatchValue</color>：数到几开产，如 >=1\n" +
            "- <color=#FFD54F>Count</color>：这条产几只，0=一直产到被下一条顶掉";
        private const string settingPath = "Setting/DelayGenerateEntitySetting";

        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is DelayGenerateEntitySettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (string.IsNullOrEmpty(c.GenerateEntitySign)) {
                red.Add("没填产谁，默认节奏产不出东西");
            }

            if (c.IntervalTime <= 0f) {
                yellow.Add("默认间隔<=0，会按 0.01 算，产得飞快");
            }

            if (c.Profiles == null || c.Profiles.Count == 0) {
                yellow.Add("一条档案没有，只按默认节奏产");
                return;
            }

            for (int i = 0; i < c.Profiles.Count; i++) {
                var p = c.Profiles[i];
                if (string.IsNullOrEmpty(p.WatchSign)) {
                    red.Add($"第{i + 1}条档案没填盯哪个数");
                }

                if (string.IsNullOrEmpty(p.WatchEntitySign)) {
                    red.Add($"第{i + 1}条档案没填数据源实体");
                }

                if (string.IsNullOrEmpty(p.GenerateEntitySign)) {
                    red.Add($"第{i + 1}条档案没填产谁");
                }

                if (p.Count < 0) {
                    yellow.Add($"第{i + 1}条档案数量是负数");
                }

                if (p.Interval <= 0f) {
                    yellow.Add($"第{i + 1}条档案间隔<=0，产得飞快");
                }
            }
        }
        public const string LIVINGCOUNT_LABEL = "LivingCount";

        private DelayGenerateEntityData _delayGenerateEntityData;
        private DelayGenerateEntityData.DelayGenerateEntityConfig _config;

        private float delayDeployTime;
        private bool hasGenerated;
        
        private List<Entity> _instances = new List<Entity>();
        private IntData _livingCountData;
        
        private int activeProfileIndex = -1;
        private int profileRemain;
        private float profileTimer;
        private bool profileActive;
        private bool[] profileWasTrue;
        private bool[] profileConsumed;

        public Behaviour_Event_DelayGenerateEntity(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _delayGenerateEntityData = AttachBehaviourData<DelayGenerateEntityData>();

            DelayGenerateEntitySetting setting = Loader.LoadAsset<DelayGenerateEntitySetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out DelayGenerateEntitySettingData settingData)) {
                return;
            }

            _config = _delayGenerateEntityData.Config;
            _config.GenerateType = settingData.GenerateType;
            _config.IntervalTime = Mathf.Max(settingData.IntervalTime, 0.01f);
            if (!BehaviourSigns.Require(settingData.GenerateEntitySign, BehaviourSign, entity.ObjConfig?.Sign, nameof(DelayGenerateEntitySettingData.GenerateEntitySign))) {
                return;
            }

            _config.GenerateEntitySign = settingData.GenerateEntitySign;
            _config.Profiles.Clear();
            foreach (DelayGenerateProfile profile in settingData.Profiles) {
                if (!BehaviourSigns.Require(profile.WatchSign, BehaviourSign, entity.ObjConfig?.Sign, nameof(DelayGenerateProfile.WatchSign))) {
                    return;
                }

                if (!BehaviourSigns.Require(profile.WatchEntitySign, BehaviourSign, entity.ObjConfig?.Sign, nameof(DelayGenerateProfile.WatchEntitySign))) {
                    return;
                }

                if (!BehaviourSigns.Require(profile.GenerateEntitySign, BehaviourSign, entity.ObjConfig?.Sign, nameof(DelayGenerateProfile.GenerateEntitySign))) {
                    return;
                }

                _config.Profiles.Add(new DelayGenerateProfile() {
                    WatchSign = profile.WatchSign,
                    WatchEntitySign = profile.WatchEntitySign,
                    Compare = profile.Compare,
                    WatchValue = profile.WatchValue,
                    GenerateEntitySign = profile.GenerateEntitySign,
                    Count = Mathf.Max(profile.Count, 0),
                    Interval = Mathf.Max(profile.Interval, 0.01f),
                });
            }

            delayDeployTime = _config.IntervalTime;

            InitProfileState();

            Cond.Instance.TryGetData(entity, LIVINGCOUNT_LABEL, out _livingCountData);
            _livingCountData.Int = _instances.Count;

            EntityRegister.OnEntityRemovedEvent.AddListener(OnEntityRemoved);
            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() { }

        private void InitProfileState() {
            profileWasTrue = new bool[_config.Profiles.Count];
            profileConsumed = new bool[_config.Profiles.Count];
        }

        private void OnEntityRemoved(int id) {
            for (int i = _instances.Count - 1; i >= 0; i--) {
                if (_instances[i].ID == id) {
                    _instances.RemoveAt(i);
                    _livingCountData.Int = _instances.Count;
                }
            }
        }

        private void OnUpdate() {
            UpdateProfileWatch();
            if (profileActive) {
                UpdateProfileSpawn();
            } else {
                UpdateSelfSpawn();
            }
        }

        /// <summary>
        /// 通用监听 边沿触发 条件从满足回落再满足才可再次触发 避免条件持续满足时无限重复产怪
        /// </summary>
        private void UpdateProfileWatch() {
            if (profileActive) return;
            for (int i = 0; i < _config.Profiles.Count; i++) {
                DelayGenerateProfile profile = _config.Profiles[i];
                bool conditionTrue = IsProfileConditionTrue(profile);

                //额度已消费 锁存到条件回落 解锁后才能再次触发
                if (profileConsumed[i]) {
                    if (!conditionTrue) {
                        profileConsumed[i] = false;
                    }
                    profileWasTrue[i] = conditionTrue;
                    continue;
                }

                //上升沿触发
                if (conditionTrue && !profileWasTrue[i]) {
                    activeProfileIndex = i;
                    profileRemain = profile.Count;
                    profileTimer = 0f;
                    profileActive = true;
                    profileWasTrue[i] = true;
                    if (profile.Count > 0) {
                        profileConsumed[i] = true;
                    }

                    return;
                }

                profileWasTrue[i] = conditionTrue;
            }
        }

        private bool IsProfileConditionTrue(DelayGenerateProfile profile) {
            if (!TryGetWatchEntity(profile.WatchEntitySign, out Entity watchEntity)) {
                return false;
            }

            return CheckWatch(watchEntity, profile.WatchSign, profile.Compare, profile.WatchValue);
        }

        private void UpdateProfileSpawn() {
            DelayGenerateProfile profile = _config.Profiles[activeProfileIndex];
            if (profile.Count > 0 && profileRemain <= 0) {
                profileActive = false;
                activeProfileIndex = -1;
                return;
            }

            profileTimer -= Time.deltaTime;
            if (profileTimer > 0f) return;
            profileTimer = profile.Interval;

            if (!CreateEntity(profile.GenerateEntitySign)) {
                if (profile.Count > 0) profileRemain--;
                return;
            }

            if (profile.Count > 0) profileRemain--;
        }

        private void UpdateSelfSpawn() {
            if (_config.GenerateType == GenerateType.Once && hasGenerated) return;
            if (delayDeployTime > 0) {
                delayDeployTime -= Time.deltaTime;
                return;
            }

            delayDeployTime = _config.IntervalTime;
            if (!CreateEntity(_config.GenerateEntitySign)) return;
            if (_config.GenerateType == GenerateType.Once) hasGenerated = true;
        }

        /// <summary>
        /// 积木连接 按配置解析要读的实体 Self=读自己 其他按Sign读其他实体的Data 行为不感知对方类型
        /// </summary>
        private bool TryGetWatchEntity(string sign, out Entity watchEntity) {
            return BehaviourSigns.ResolveEntity(entity, BehaviourSign, nameof(DelayGenerateProfile.WatchEntitySign), sign, out watchEntity);
        }

        private bool CheckWatch(Entity dataEntity, string sign, WatchCompare compare, int target) {
            if (dataEntity == null) return false;
            if (!BehaviourSigns.Require(sign, BehaviourSign, dataEntity.ObjConfig?.Sign, nameof(DelayGenerateProfile.WatchSign))) {
                return false;
            }
            int current;
            if (Cond.Instance.GetData<IntData>(dataEntity, sign, out IntData intData)) current = intData.Int;
            else if (Cond.Instance.GetData<FloatData>(dataEntity, sign, out FloatData floatData)) current = Mathf.RoundToInt(floatData.Float);
            else if (Cond.Instance.GetData<BoolData>(dataEntity, sign, out BoolData boolData)) current = boolData.Bool ? 1 : 0;
            else return false;
            switch (compare) {
                case WatchCompare.Equal: return current == target;
                case WatchCompare.GreaterEqual: return current >= target;
                case WatchCompare.LessEqual: return current <= target;
                case WatchCompare.Greater: return current > target;
                case WatchCompare.Less: return current < target;
                default: return current == target;
            }
        }

        private bool CreateEntity(string entitySign) {
            if (string.IsNullOrEmpty(entitySign)) {
                LogUtil.LogError("生成实体失败 未配置生成实体标识!");
                return false;
            }

            ObjConfig entityConfig = ObjConfig.Get(entitySign);
            if (entityConfig == null) {
                LogUtil.LogErrorFormat("生成实体失败 未找到实体配置:{0}", entitySign);
                return false;
            }

            if (string.IsNullOrEmpty(entityConfig.SetUpLocationInformationSign)) {
                LogUtil.LogErrorFormat("生成实体失败 实体:{0} 未配置初始点!", entitySign);
                return false;
            }

            LocationInformationData locationData = LocationUtil.Instance.GetRandomPosition(entityConfig.SetUpLocationInformationSign);
            if (locationData == null) {
                return false;
            }

            Entity instance = Obj.Instance.LoadEntity(entitySign);
            _instances.Add(instance);
            instance.SetBeginLocationInfo(locationData);
            _livingCountData.Int = _instances.Count;
            LogUtil.LogFormat($"实体: {entitySign} 生成成功!");
            return true;
        }

        public override void Clear() {
            EntityRegister.OnEntityRemovedEvent.RemoveListener(OnEntityRemoved);
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            if (_instances.Count > 0) {
                for (int i = _instances.Count - 1; i >= 0; i--) {
                    Entity tmpEntity = _instances[i];
                    _instances.RemoveAt(i);
                    Obj.Instance.UnLoadEntity(tmpEntity);
                }
            }
            _livingCountData.Int = 0;
            DetachBehaviourData<DelayGenerateEntityData>();
            base.Clear();
        }
    }
}