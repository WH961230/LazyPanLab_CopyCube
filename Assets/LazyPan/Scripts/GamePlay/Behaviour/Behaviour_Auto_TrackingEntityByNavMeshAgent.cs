using UnityEngine;
using UnityEngine.AI;

namespace LazyPan {
    /// <summary>
    /// 行为 - 追踪实体
    /// 实体负责装配 行为只消费自身配置与运行时状态
    /// </summary>
    public class Behaviour_Auto_TrackingEntityByNavMeshAgent : Behaviour {
        private const string settingPath = "Setting/TrackingEntitySetting";
        private NavMeshAgent _navMeshAgent;
        private Entity _targetEntity;
        private TrackingEntityData _trackingData;
        private TrackingEntityData.TrackingConfig _config;
        private BoolData _movementStopData;

        public Behaviour_Auto_TrackingEntityByNavMeshAgent(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _trackingData = AttachBehaviourData<TrackingEntityData>();
            
            TrackingEntitySetting setting = Loader.LoadAsset<TrackingEntitySetting>(AssetType.ASSET, settingPath);

            if (!setting.TryGet(entity.ObjConfig.Sign, out var settingData)) {
                return;
            }

            _config = _trackingData.Config;
            _config.TrackingStop = settingData.TrackingStop;
            _config.TrackingSpeed = settingData.TrackingSpeed;
            _config.TargetEntityType = settingData.TargetType;
            Cond.Instance.TryGetData(entity, "MovementStop", out _movementStopData);

            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {
        }

        private bool GetNavMeshAgent() {
            if (_navMeshAgent != null) {
                return true;
            }

            _navMeshAgent = Cond.Instance.Get<NavMeshAgent>(entity, "NavMeshAgent");
            return _navMeshAgent != null;
        }
        
        private bool GetTargetEntity() {
            if (_targetEntity != null) {
                return true;
            }

            if (!EntityRegister.TryGetRandEntityByType(_config.TargetEntityType, out _targetEntity)) {
                return false;
            }

            if (_targetEntity == entity) {
                _targetEntity = null;
                return false;
            }

            return true;
        }

        private void OnUpdate() {
            if (entity.Prefab == null || !GetNavMeshAgent() || !GetTargetEntity()) {
                return;
            }

            if (_targetEntity.Prefab == null) {
                _targetEntity = null;
                return;
            }

            _navMeshAgent.speed = _config.TrackingSpeed;
            //配置默认停止或实体级运行时停止命令 任一为真即停
            if (_config.TrackingStop || (_movementStopData != null && _movementStopData.Bool)) {
                if (_navMeshAgent.velocity.magnitude > 0) {
                    _navMeshAgent.isStopped = true;
                    _navMeshAgent.ResetPath();
                }
            } else {
                Transform body = Cond.Instance.Get<Transform>(_targetEntity, "Body");
                if (body != null) {
                    _navMeshAgent.SetDestination(body.position);
                } else {
                    _targetEntity = null;
                }
            }
        }

        public override void Clear() {
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            DetachBehaviourData<TrackingEntityData>();
            base.Clear();
        }
    }
}
