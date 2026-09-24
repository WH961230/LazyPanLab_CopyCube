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
            "【追踪实体】管一个怪自动找路追人，目标没了就原地等。\n" +
            "停走命令看实体级 MoveAttr，谁置停都停。\n" +
            "— 配置参数（TrackingEntitySetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>TargetType</color>：追谁，按实体类型名填，找不到就原地等\n" +
            "- <color=#FFD54F>NavMeshTerrainSign</color>：寻路用的地形实体，如 Obj_Terrain_SceneC_Terrain，不填不查 NavMesh\n" +
            "- <color=#FFD54F>TrackingSpeed</color>：追多快，建议 2~6\n" +
            "- <color=#FFD54F>TrackingStop</color>：true=这个追踪自己先停住，false=跟着大家一起走";
        private const string settingPath = "Setting/TrackingEntitySetting";

        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is TrackingEntitySettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (string.IsNullOrEmpty(c.TargetType)) {
                red.Add("没填追谁，原地等");
            }

            if (c.TrackingSpeed <= 0f) {
                yellow.Add("速度<=0，追不动");
            }

            if (c.TrackingStop) {
                yellow.Add("停止打勾了，自己先停住");
            }

            // 地形填没填只看空不空，预制体上有没有导航组件由编辑器加查去翻，不用开场景
            if (string.IsNullOrEmpty(c.NavMeshTerrainSign)) {
                red.Add("没选寻路地形，先填地形实体");
            }
        }
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
