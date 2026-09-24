using UnityEngine;
using UnityEngine.InputSystem;

namespace LazyPan {
    /// <summary>
    /// 行为 - 控制器控制WASD前后左右移动
    /// 只做一件事: 读手柄输入推 CharacterController 走路 碰墙自己停 不调别的移动行为
    /// 停走命令看实体级 MoveAttr.Stopped(谁置 true 都停) 自己的速度/转向只存在自己家
    /// 配置来源 Setting/InputWASDMoveSetting 运行时状态只写自己的 InputWASDMoveData
    /// </summary>
    public class Behaviour_Auto_InputWASDMove : Behaviour {
        /// <summary>移动节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【控制器移动】管一个实体的 WASD 走路，读输入、推 CharacterController，不调别的移动行为。\n" +
            "停走命令看实体级 MoveAttr，谁置停都停。\n" +
            "— 配置参数（InputWASDMoveSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>InputControlSign</color>：输入方案名，填 InputRegister 里的那一个，错了就没反应\n" +
            "- <color=#FFD54F>MoveSpeed</color>：移动速度，0=不动，建议 3~8\n" +
            "- <color=#FFD54F>RotateSpeed</color>：转向速度，0=不转身，建议 5~15\n" +
            "- <color=#FFD54F>Gravity</color>：重力加速度，一般填负数（如 -20），落地后自动压住\n" +
            "— 数据交流（读写实体级 MoveAttr，不直接调别的行为）—\n" +
            "- <color=#FFD54F>停走</color>：MoveAttr.Stopped=true 全体移动行为一起停，false=恢复\n" +
            "- <color=#FFD54F>位置</color>：推 CharacterController，身体朝向跟着输入转";
        private const string settingPath = "Setting/InputWASDMoveSetting";

        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is InputWASDMoveSettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (string.IsNullOrEmpty(c.InputControlSign)) {
                red.Add("输入方案名没填，推杆没反应，先去 InputRegister 对名字");
            }

            if (c.MoveSpeed <= 0f) {
                yellow.Add("速度<=0，人物不动");
            }

            if (c.RotateSpeed <= 0f) {
                yellow.Add("转速<=0，人物不转身");
            }

            if (c.Gravity == 0f) {
                yellow.Add("重力=0，人物会飘");
            } else if (c.Gravity > 0f) {
                yellow.Add("重力是正数，人物往天上飞");
            }
        }

        private InputWASDMoveData _moveData;

        private MoveAttr _moveAttr;

        public Behaviour_Auto_InputWASDMove(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _moveData = AttachBehaviourData<InputWASDMoveData>();
            
            InputWASDMoveSetting setting = Loader.LoadAsset<InputWASDMoveSetting>(AssetType.ASSET, settingPath);

            if (!setting.TryGet(entity.ObjConfig.Sign, out var settingData)) {
                return;
            }

            _moveData.Config.InputControlSign = settingData.InputControlSign;
            _moveData.Config.MoveSpeed = Mathf.Max(settingData.MoveSpeed, 0f);
            _moveData.Config.RotateSpeed = Mathf.Max(settingData.RotateSpeed, 0f);
            _moveData.Config.Gravity = settingData.Gravity;
            _moveAttr = EntityAttrRegistry.RegisterOrGetMove(entity);

            InputRegister.Instance.Load(_moveData.Config.InputControlSign, OnMotion);
            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {
        }

        private void OnMotion(InputAction.CallbackContext context) {
            _moveData.InputVec = context.ReadValue<Vector2>();
        }

        private bool GetControllers(out CharacterController controller, out Transform transform) {
            controller = Cond.Instance.Get<CharacterController>(entity, "CharacterController");
            transform = Cond.Instance.Get<Transform>(entity, "Body");
            return controller != null;
        }

        private void OnUpdate() {
            if (!GetControllers(out CharacterController controller, out Transform bodyTran)) {
                return;
            }

            bool stop = _moveAttr != null && _moveAttr.Stopped;
            if (stop) {
                _moveData.InputVec = Vector2.zero;
            }

            Vector2 input = _moveData.InputVec;
            var cfg = _moveData.Config;
            Vector3 move = Vector3.zero;
            if (!stop && input.sqrMagnitude > 0.0001f) {
                move = Vector3.right * input.x + Vector3.forward * input.y;
                move.Normalize();
                if (move.sqrMagnitude > 0.001f && cfg.RotateSpeed > 0f) {
                    Quaternion target = Quaternion.LookRotation(move);
                    bodyTran.rotation = Quaternion.Slerp(bodyTran.rotation, target, cfg.RotateSpeed * Time.deltaTime);
                }
            }

            _moveData.YSpeed += cfg.Gravity * Time.deltaTime;
            if (controller.isGrounded && _moveData.YSpeed < 0f) {
                _moveData.YSpeed = -2f;
            }

            Vector3 velocity = move * cfg.MoveSpeed + Vector3.up * _moveData.YSpeed;
            controller.Move(velocity * Time.deltaTime);
        }

        public override void Clear() {
            InputRegister.Instance.UnLoad(_moveData.Config.InputControlSign, OnMotion);
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            DetachBehaviourData<InputWASDMoveData>();
            base.Clear();
        }
    }
}
