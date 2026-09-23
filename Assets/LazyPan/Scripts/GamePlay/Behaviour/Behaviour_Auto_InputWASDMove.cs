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
            "本行为数据自带，不用去 ParamValue 配任何东西；停走命令看实体级 MoveAttr，谁置停都停。\n" +
            "— 配置参数（InputWASDMoveSetting 里按 SourceSign 配）—\n" +
            "- InputControlSign：输入方案名，填 InputRegister 里的那一个，错了就没反应\n" +
            "- MoveSpeed：移动速度，0=不动，建议 3~8\n" +
            "- RotateSpeed：转向速度，0=不转身，建议 5~15\n" +
            "- Gravity：重力加速度，一般填负数（如 -20），落地后自动压住\n" +
            "— 运行时数据（InputWASDMoveData，自己管）—\n" +
            "- 输入向量：回调里实时更新，外部不用管\n" +
            "- ySpeed：下坠速度，内部累计，落地自动回压\n" +
            "— 数据交流（读写实体级 MoveAttr，不直接调别的行为）—\n" +
            "- 停走：MoveAttr.Stopped=true 全体移动行为一起停，false=恢复\n" +
            "- 位置：推 CharacterController，身体朝向跟着输入转";
        private const string settingPath = "Setting/InputWASDMoveSetting";

        private InputWASDMoveData _moveData;
        private InputWASDMoveData.InputWASDMoveConfig _config;

        private MoveAttr _moveAttr;
        private float ySpeed;
        private Vector2 input;

        public Behaviour_Auto_InputWASDMove(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _moveData = AttachBehaviourData<InputWASDMoveData>();
            
            InputWASDMoveSetting setting = Loader.LoadAsset<InputWASDMoveSetting>(AssetType.ASSET, settingPath);

            if (!setting.TryGet(entity.ObjConfig.Sign, out var settingData)) {
                return;
            }

            _config = _moveData.Config;
            _config.InputControlSign = settingData.InputControlSign;
            _config.MoveSpeed = Mathf.Max(settingData.MoveSpeed, 0f);
            _config.RotateSpeed = Mathf.Max(settingData.RotateSpeed, 0f);
            _config.Gravity = settingData.Gravity;
            _moveAttr = EntityAttrRegistry.RegisterOrGetMove(entity);

            InputRegister.Instance.Load(_config.InputControlSign, OnMotion);
            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {
        }

        private void OnMotion(InputAction.CallbackContext context) {
            input = context.ReadValue<Vector2>();
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
                input = Vector2.zero;
            }

            Vector3 move = Vector3.zero;
            if (!stop && input.sqrMagnitude > 0.0001f) {
                move = Vector3.right * input.x + Vector3.forward * input.y;
                move.Normalize();
                if (move.sqrMagnitude > 0.001f && _config.RotateSpeed > 0f) {
                    Quaternion target = Quaternion.LookRotation(move);
                    bodyTran.rotation = Quaternion.Slerp(bodyTran.rotation, target, _config.RotateSpeed * Time.deltaTime);
                }
            }

            ySpeed += _config.Gravity * Time.deltaTime;
            if (controller.isGrounded && ySpeed < 0f) {
                ySpeed = -2f;
            }

            Vector3 velocity = move * _config.MoveSpeed + Vector3.up * ySpeed;
            controller.Move(velocity * Time.deltaTime);
        }

        public override void Clear() {
            InputRegister.Instance.UnLoad(_config.InputControlSign, OnMotion);
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            DetachBehaviourData<InputWASDMoveData>();
            base.Clear();
        }
    }
}
