using UnityEngine;
using UnityEngine.AI;

namespace LazyPan {
    /// <summary>
    /// 行为 - 追踪实体
    /// 只做一件事: 让 NavMeshAgent 追着目标实体跑 目标没了就地等 不调别的移动行为
    /// 停走命令看实体级 MoveAttr.Stopped(谁置 true 都停) 自己的速度/目标只存在自己家
    /// 配置来源 Setting/TrackingEntitySetting 运行时状态只写自己的 TrackingEntityData
    /// </summary>
    public class Behaviour_Auto_TrackingEntityByNavMeshAgent : Behaviour {
        /// <summary>追踪节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【追踪实体】管一个实体的自动追人，NavMeshAgent 找路追目标，不调别的移动行为。\n" +
            "本行为数据自带，不用去 ParamValue 配任何东西；停走命令看实体级 MoveAttr，谁置停都停。\n" +
            "— 配置参数（TrackingEntitySetting 里按 SourceSign 配）—\n" +
            "- TargetType：追谁，按实体类型名填，找不到就原地等\n" +
            "- TrackingSpeed：追击速度，建议 2~6，填到 NavMeshAgent.speed\n" +
            "- TrackingStop：true=本行为自己先停住，false=跟着实体级开关走\n" +
            "— 运行时数据（TrackingEntityData，自己管）—\n" +
            "- 目标实体：第一次找到后缓存，目标销毁自动清空重找\n" +
            "- NavMeshAgent：第一次拿到后缓存，拿不到就不跑\n" +
            "— 数据交流（读写实体级 MoveAttr，不直接调别的行为）—\n" +
            "- 停走：自己 TrackingStop 或 MoveAttr.Stopped 任一 true 就停\n" +
            "- 寻路：目标 Body 位置丢给 SetDestination，目标没 Body 就重找";
        private const string settingPath = "Setting/TrackingEntitySetting";
        private NavMeshAgent _navMeshAgent;
        private Entity _targetEntity;
        private TrackingEntityData _trackingData;
        private TrackingEntityData.TrackingConfig _config;
        private MoveAttr _moveAttr;

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
            _moveAttr = EntityAttrRegistry.RegisterOrGetMove(entity);

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
            if (_config.TrackingStop || (_moveAttr != null && _moveAttr.Stopped)) {
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
