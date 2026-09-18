using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 波数管理器
    /// 积木 只负责波次递增与波间节奏 不产怪 不感知任何其他行为
    /// 仅读写自身 Data: WaveIndex/WaveState/WaveRestRemain  等待条件通过通用 WatchWatchSign 读任意 IntData
    /// </summary>
    public class Behaviour_Event_WaveManager : Behaviour {
        private const string settingPath = "Setting/WaveManagerSetting";
        public const string WAVEINDEX_LABEL = "WaveIndex";
        public const string WAVESTATE_LABEL = "WaveState";
        public const string WAVERESTREMAIN_LABEL = "WaveRestRemain";

        private WaveManagerData _waveData;
        private WaveManagerData.WaveManagerConfig _config;

        private WaveManagerState state;
        private float stateTimer;
        private int waveIndex;
        private int waveNumber;
        private IntData _waveIndexData;
        private StringData _waveStateData;
        private FloatData _waveRestRemainData;

        public Behaviour_Event_WaveManager(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _waveData = AttachBehaviourData<WaveManagerData>();
            WaveManagerSetting setting = Loader.LoadAsset<WaveManagerSetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out WaveManagerSettingData settingData)) {
                return;
            }

            CopySetting(settingData);
            if (!ValidateSetting()) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 波次配置校验失败!", BehaviourSign, entity.ObjConfig.Sign);
                return;
            }

            InitRuntimeData();
            Game.instance.OnUpdateEvent.AddListener(OnUpdate);

            if (_config.InitialDelay > 0f) {
                EnterState(WaveManagerState.InitialDelay, _config.InitialDelay);
            } else {
                StartWave(waveIndex);
            }
        }

        public override void DelayedExecute() { }

        private void CopySetting(WaveManagerSettingData settingData) {
            _config = _waveData.Config;
            _config.StartWaveIndex = settingData.StartWaveIndex;
            _config.InitialDelay = Mathf.Max(settingData.InitialDelay, 0f);
            _config.Loop = settingData.Loop;
            //未开始时对外报 起始波数-1 保证监听方在第一波正式开始时才收到上升沿
            waveNumber = _config.StartWaveIndex - 1;
            _config.Waves.Clear();
            foreach (WaveEntry entry in settingData.Waves) {
                if (entry.AdvanceMode == WaveAdvanceMode.WaitValue) {
                    if (!BehaviourSigns.Require(entry.WaitWatchSign, BehaviourSign, entity.ObjConfig?.Sign, nameof(WaveEntry.WaitWatchSign))) {
                        return;
                    }

                    if (!BehaviourSigns.Require(entry.WaitWatchEntitySign, BehaviourSign, entity.ObjConfig?.Sign, nameof(WaveEntry.WaitWatchEntitySign))) {
                        return;
                    }
                }

                _config.Waves.Add(new WaveEntry() {
                    RestDuration = Mathf.Max(entry.RestDuration, 0f),
                    AdvanceMode = entry.AdvanceMode,
                    WaitWatchSign = entry.WaitWatchSign,
                    WaitWatchEntitySign = entry.WaitWatchEntitySign,
                    WaitTargetValue = entry.WaitTargetValue,
                    Compare = entry.Compare,
                });
            }
        }

        private bool ValidateSetting() {
            if (_config.Waves == null || _config.Waves.Count == 0) {
                LogUtil.LogErrorFormat("行为:{0} Waves 为空", BehaviourSign);
                return false;
            }
            return true;
        }

        private void InitRuntimeData() {
            Cond.Instance.TryGetData(entity, WAVEINDEX_LABEL, out _waveIndexData);
            Cond.Instance.TryGetData(entity, WAVESTATE_LABEL, out _waveStateData);
            Cond.Instance.TryGetData(entity, WAVERESTREMAIN_LABEL, out _waveRestRemainData);
            _waveIndexData.Int = waveNumber;
            _waveStateData.String = WaveManagerState.Idle.ToString();
            _waveRestRemainData.Float = 0f;
        }

        private void EnterState(WaveManagerState next, float timer) {
            state = next;
            stateTimer = timer;
            _waveStateData.String = state.ToString();
            _waveRestRemainData.Float = next == WaveManagerState.Rest || next == WaveManagerState.Waiting ? timer : 0f;
        }

        private void StartWave(int index) {
            waveIndex = index;
            waveNumber = waveIndex + _config.StartWaveIndex;
            _waveIndexData.Int = waveNumber;
            _waveStateData.String = state.ToString();
            WaveEntry entry = _config.Waves[waveIndex];
            if (entry.AdvanceMode == WaveAdvanceMode.WaitValue && !string.IsNullOrEmpty(entry.WaitWatchSign)) {
                EnterState(WaveManagerState.Waiting, entry.RestDuration);
            } else if (entry.RestDuration > 0f) {
                EnterState(WaveManagerState.Rest, entry.RestDuration);
            } else {
                CompleteCurrentWave();
            }
        }

        private void CompleteCurrentWave() {
            int next = waveIndex + 1;
            if (next < _config.Waves.Count) {
                StartWave(next);
                return;
            }
            if (_config.Loop) {
                StartWave(0);
                return;
            }
            EnterState(WaveManagerState.Completed, 0f);
        }

        private void OnUpdate() {
            switch (state) {
                case WaveManagerState.InitialDelay: UpdateInitialDelay(); break;
                case WaveManagerState.Rest: UpdateRest(); break;
                case WaveManagerState.Waiting: UpdateWaiting(); break;
                case WaveManagerState.Completed: break;
            }
        }

        private void UpdateInitialDelay() {
            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f) return;
            StartWave(0);
        }

        private void UpdateRest() {
            stateTimer -= Time.deltaTime;
            _waveRestRemainData.Float = Mathf.Max(stateTimer, 0f);
            if (stateTimer > 0f) return;
            CompleteCurrentWave();
        }

        private void UpdateWaiting() {
            WaveEntry entry = _config.Waves[waveIndex];
            if (!TryGetWatchEntity(entry.WaitWatchEntitySign, out Entity watchEntity)) {
                return;
            }
            if (!CheckWatch(watchEntity, entry.WaitWatchSign, entry.Compare, entry.WaitTargetValue)) return;
            if (stateTimer > 0f) {
                stateTimer -= Time.deltaTime;
                _waveRestRemainData.Float = Mathf.Max(stateTimer, 0f);
                if (stateTimer > 0f) return;
            }
            CompleteCurrentWave();
        }

        /// <summary>
        /// 积木连接 按配置解析要读的实体 Self=读自己 其他按Sign读其他实体的Data 行为不感知对方类型
        /// </summary>
        private bool TryGetWatchEntity(string sign, out Entity watchEntity) {
            return BehaviourSigns.ResolveEntity(entity, BehaviourSign, nameof(WaveEntry.WaitWatchEntitySign), sign, out watchEntity);
        }

        private bool CheckWatch(Entity dataEntity, string sign, WatchCompare compare, int target) {
            if (dataEntity == null) return false;
            if (!BehaviourSigns.Require(sign, BehaviourSign, dataEntity.ObjConfig?.Sign, nameof(WaveEntry.WaitWatchSign))) {
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

        public override void Clear() {
            if (Game.instance != null) Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            DetachBehaviourData<WaveManagerData>();
            base.Clear();
        }
    }
}
