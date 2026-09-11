using UnityEngine;
using UnityEngine.InputSystem;

namespace LazyPan {
    /// <summary>
    /// 行为 - 控制器控制WASD前后左右移动
    /// 实体负责装配 行为只消费自身配置与运行时状态
    /// </summary>
    public class Behaviour_Auto_InputWASDMove : Behaviour {
        private const string settingPath = "Setting/InputWASDMoveSetting";

        private InputWASDMoveData _moveData;
        private InputWASDMoveData.InputWASDMoveConfig _config;

        private BoolData _movementStopData;
        private float ySpeed;
        private Vector2 input;

        public Behaviour_Auto_InputWASDMove(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _moveData = entity.Prefab.AddComponent<InputWASDMoveData>();
            _moveData.EntityID = entity.ID;
            
            InputWASDMoveSetting setting = Loader.LoadAsset<InputWASDMoveSetting>(AssetType.ASSET, settingPath);

            if (!setting.TryGet(entity.ObjConfig.Sign, out var settingData)) {
                return;
            }

            _config = _moveData.Config;
            _config.InputControlSign = settingData.InputControlSign;
            _config.MoveSpeed = Mathf.Max(settingData.MoveSpeed, 0f);
            _config.RotateSpeed = Mathf.Max(settingData.RotateSpeed, 0f);
            _config.Gravity = settingData.Gravity;
            Cond.Instance.TryGetData(entity, "MovementStop", out _movementStopData);

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

            bool stop = _movementStopData != null && _movementStopData.Bool;
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
            base.Clear();
        }
    }
}
