using UnityEngine;
using UnityEngine.InputSystem;

namespace LazyPan {
    /// <summary>
    /// 行为 - 瞬移位移
    /// 只做一件事: 读瞬移按键推 CharacterController 按曲线飞一段 碰墙自己停 不调别的移动行为
    /// 停走命令看实体级 MoveAttr.Stopped(谁置 true 都停) 瞬移占领看 MoveAttr.Teleporting(飞完自动松开)
    /// 配置来源 Setting/TeleportationSetting 运行时状态只写自己的 TeleportationData
    /// </summary>
    public class Behaviour_Auto_Teleportation : Behaviour {
        /// <summary>瞬移节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【瞬间移动】管一个实体的瞬移，按按键、朝身体朝向、按曲线飞一段，不调别的移动行为。\n" +
            "停走命令看实体级 MoveAttr，谁置停都停。\n" +
            "— 配置参数（TeleportationSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>TeleportInputSign</color>：触发按键，填 InputRegister 里的那一个，如 Global/Space，错了就没反应\n" +
            "- <color=#FFD54F>Distance</color>：瞬移距离，0=原地罚站，建议 4~8\n" +
            "- <color=#FFD54F>Duration</color>：瞬移耗时（秒），走完曲线就停，建议 0.2~0.4\n" +
            "- <color=#FFD54F>SpeedCurve</color>：速度曲线，x=进度0~1，y=速度倍率，前快后慢就拉成前高后低\n" +
            "- <color=#FFD54F>Cooldown</color>：冷却（秒），防连按，建议 0.5~1.5\n" +
            "- <color=#FFD54F>TeleportPriority</color>：瞬移优先级，跟 MoveAttr.MovePriority 比，大就压住 WASD\n" +
            "— 数据交流（读写实体级 MoveAttr，不直接调别的行为）—\n" +
            "- <color=#FFD54F>停走</color>：MoveAttr.Stopped=true 全体移动行为一起停，false=恢复\n" +
            "- <color=#FFD54F>占领</color>：起飞置 MoveAttr.Teleporting=true，WASD 水平让路只留重力，落地=false=恢复\n" +
            "- <color=#FFD54F>位置</color>：推 CharacterController，方向取身体朝向";
        /// <summary>硬依赖的 Unity 组件：检查按钮真去预制体上找，缺了判红</summary>
        public static readonly string[] RequiredComponents = { "CharacterController" };
        private const string settingPath = "Setting/TeleportationSetting";

        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is TeleportationSettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (string.IsNullOrEmpty(c.TeleportInputSign)) {
                red.Add("触发按键没填，瞬移没反应，先去 InputRegister 对名字");
            }

            if (c.Distance <= 0f) {
                yellow.Add("距离<=0，原地罚站");
            }

            if (c.Duration <= 0f) {
                red.Add("耗时<=0，飞不动，先填个 0.25 试试");
            }

            if (c.Cooldown < 0f) {
                yellow.Add("冷却填了负数，会被钳到 0");
            }
        }

        private TeleportationData _moveData;

        private MoveAttr _moveAttr;

        public Behaviour_Auto_Teleportation(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _moveData = AttachBehaviourData<TeleportationData>();

            TeleportationSetting setting = Loader.LoadAsset<TeleportationSetting>(AssetType.ASSET, settingPath);

            if (!setting.TryGet(entity.ObjConfig.Sign, out TeleportationSettingData settingData)) {
                return;
            }

            _moveData.Config.TeleportInputSign = settingData.TeleportInputSign;
            _moveData.Config.Distance = Mathf.Max(settingData.Distance, 0f);
            _moveData.Config.Duration = Mathf.Max(settingData.Duration, 0.01f);
            _moveData.Config.SpeedCurve = settingData.SpeedCurve ?? AnimationCurve.Linear(0f, 1f, 1f, 1f);
            _moveData.Config.Cooldown = Mathf.Max(settingData.Cooldown, 0f);
            _moveData.Config.TeleportPriority = settingData.TeleportPriority;
            _moveAttr = EntityAttrRegistry.RegisterOrGetMove(entity);
            if (_moveAttr != null) {
                _moveAttr.TeleportPriority = _moveData.Config.TeleportPriority;
            }

            InputRegister.Instance.Load(_moveData.Config.TeleportInputSign, OnTeleportKey);
            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {
        }

        private void OnTeleportKey(InputAction.CallbackContext context) {
            if (!context.started && !context.performed) {
                return;
            }

            if (_moveData.IsTeleporting || _moveData.CooldownLeft > 0f) {
                return;
            }

            if (_moveAttr != null && _moveAttr.Stopped) {
                return;
            }

            Transform bodyTran = Cond.Instance.Get<Transform>(entity, "Body");
            Vector3 dir = bodyTran != null ? bodyTran.forward : entity.Comp.transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) {
                dir = Vector3.forward;
            }

            _moveData.Direction = dir.normalized;
            _moveData.Elapsed = 0f;
            _moveData.IsTeleporting = true;
            if (_moveAttr != null) {
                _moveAttr.Teleporting = true;
                _moveAttr.TeleportPriority = _moveData.Config.TeleportPriority;
            }
        }

        private bool GetControllers(out CharacterController controller) {
            controller = Cond.Instance.Get<CharacterController>(entity, "CharacterController");
            return controller != null;
        }

        private void OnUpdate() {
            if (_moveData.CooldownLeft > 0f) {
                _moveData.CooldownLeft -= Time.deltaTime;
            }

            if (!_moveData.IsTeleporting) {
                return;
            }

            // 击退占领：优先级更高的击退进行中，瞬移暂停等击退飞完（Elapsed 不走，时间冻结）
            if (_moveAttr != null && _moveAttr.KnockingBack
                && _moveAttr.KnockbackPriority >= _moveAttr.TeleportPriority) {
                return;
            }

            if (!GetControllers(out CharacterController controller)) {
                StopTeleport();
                return;
            }

            _moveData.Elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_moveData.Elapsed / _moveData.Config.Duration);
            float curveK = _moveData.Config.SpeedCurve != null && _moveData.Config.SpeedCurve.length > 0 ? Mathf.Max(_moveData.Config.SpeedCurve.Evaluate(t), 0f) : 1f;
            float speed = _moveData.Config.Distance / _moveData.Config.Duration * curveK;
            controller.Move(_moveData.Direction * speed * Time.deltaTime);
            if (t >= 1f) {
                StopTeleport();
            }
        }

        private void StopTeleport() {
            _moveData.IsTeleporting = false;
            _moveData.CooldownLeft = _moveData.Config.Cooldown;
            if (_moveAttr != null) {
                _moveAttr.Teleporting = false;
            }
        }

        public override void Clear() {
            InputRegister.Instance.UnLoad(_moveData.Config.TeleportInputSign, OnTeleportKey);
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            if (_moveAttr != null) {
                _moveAttr.Teleporting = false;
            }

            DetachBehaviourData<TeleportationData>();
            base.Clear();
        }
    }
}
