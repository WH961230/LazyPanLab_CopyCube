using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LazyPan {
    /// <summary>
    /// 行为 - 控制器控制WASD前后左右移动
    /// 实体负责装配 行为只消费自身配置与运行时状态
    /// </summary>
    public class Behaviour_Auto_InputWASDMove : Behaviour {
        private const string settingPath = "Setting/InputWASDMoveSetting";

        public static readonly string[] RequiredComponents = { "CharacterController" };

        private InputWASDMoveData _moveData;
        private InputWASDMoveData.InputWASDMoveConfig _config;

        private BoolData _movementStopData;
        private float ySpeed;
        private Vector2 input;

        public Behaviour_Auto_InputWASDMove(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _moveData = entity.Prefab.AddComponent<InputWASDMoveData>();
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

        public static void CheckContract(object config, List<string> red, List<string> yellow) {
            var c = (InputWASDMoveSettingData) config;
            if (string.IsNullOrEmpty(c.SourceSign)) {
                red.Add("SourceSign为空，填了才知道查谁的预制体");
            }
            if (string.IsNullOrEmpty(c.InputControlSign)) {
                red.Add("InputControlSign为空，填动作路径才识别按键");
            }
            if (c.MoveSpeed <= 0f) {
                red.Add("MoveSpeed须>0，0或负数不会动");
            }
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(c.SourceSign)) {
                return;
            }
            GameObject prefab = FindEntityPrefab(c.SourceSign);
            if (prefab == null) {
                yellow.Add($"找不到预制体 {c.SourceSign}，确认 Prefabs/Obj 下有同名预制体");
                return;
            }
            bool hasCC = false;
            var comp = prefab.GetComponent<Comp>();
            if (comp != null && comp.CharacterControllers != null) {
                foreach (var d in comp.CharacterControllers) {
                    if (d != null && d.Sign == "CharacterController" && d.Controller != null) {
                        hasCC = true;
                        break;
                    }
                }
            }
            if (!hasCC) {
                red.Add($"预制体 {c.SourceSign} 的 Comp 里没有挂载 CharacterController（Sign=CharacterController），先去 Comp 的 CharacterControllers 里登记再跑");
            }
            bool hasBody = false;
            if (comp != null && comp.Transforms != null) {
                foreach (var t in comp.Transforms) {
                    if (t != null && t.Sign == "Body" && t.Tran != null) {
                        hasBody = true;
                        break;
                    }
                }
            }
            if (!hasBody) {
                red.Add($"预制体 {c.SourceSign} 的 Comp 里没有登记 Transform:Body（Sign=Body），先去 Comp 的 Transforms 里登记再跑，不然不会转身");
            }
#endif
        }

#if UNITY_EDITOR
        static GameObject FindEntityPrefab(string sourceSign) {
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets(sourceSign + " t:Prefab")) {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) {
                    continue;
                }
                if (!System.IO.Path.GetFileNameWithoutExtension(path).Equals(sourceSign, System.StringComparison.Ordinal)) {
                    continue;
                }
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) {
                    return prefab;
                }
            }
            return null;
        }
#endif
    }
}
