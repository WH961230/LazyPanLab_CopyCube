using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 传送流程
    /// 只做一件事: 前置条件满足时按配置跳一次场景 不引用任何其他行为
    /// 有条件=只看条件(只读 Data 不写 Data) 无条件=只看内部请求标记(调 RequestTeleport)
    /// 请求标记只活在行为内部 不进 Data 不配 ParamValue 跳转只走 Flow.Next
    /// 配置来源 Setting/TeleportFlowSetting 快照同步到自身 TeleportFlowData 便于查看与调试
    /// </summary>
    public class Behaviour_Event_TeleportFlow : Behaviour {
        private const string settingPath = "Setting/TeleportFlowSetting";

        //config
        private TeleportFlowData _teleportData;
        private TeleportFlowData.TeleportFlowConfig _config;

        //runtime 内部请求标记 不进 Data 外部代码调 RequestTeleport 置 true
        private bool _innerRequest;
        private bool hasTeleported;

        public Behaviour_Event_TeleportFlow(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _teleportData = entity.Prefab.AddComponent<TeleportFlowData>();
            _teleportData.EntityID = entity.ID;

            TeleportFlowSetting setting = Loader.LoadAsset<TeleportFlowSetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out TeleportFlowSettingData settingData)) {
                return;
            }

            _config = _teleportData.Config;
            _config.TargetSceneSign = settingData.TargetSceneSign;
            _config.Once = settingData.Once;
            _config.Condition = settingData.Condition ?? new TeleportCondition();

            if (string.IsNullOrEmpty(_config.TargetSceneSign)) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 未配置目标场景标识!", BehaviourSign, entity.ObjConfig.Sign);
                return;
            }

            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {
        }

        /// <summary>
        /// 对外唯一入口 只写内部标记 跳转仍由 OnUpdate 统一执行 本行为不读不调任何其他行为
        /// </summary>
        public void RequestTeleport() {
            _innerRequest = true;
        }

        private void OnUpdate() {
            if (_config.Once && hasTeleported) {
                return;
            }

            // 有条件只看条件 无条件只看内部请求 两条路二选一 不再双重要求
            if (HasCondition(_config.Condition)) {
                if (!IsConditionTrue(_config.Condition)) {
                    return;
                }
            } else if (!_innerRequest) {
                return;
            }

            if (!Flo.Instance.GetCurFlow(out Flow flow)) {
                return;
            }

            hasTeleported = true;
            _innerRequest = false;
            flow.Next(_config.TargetSceneSign);
        }

        /// <summary>
        /// 是否配了前置条件 左边标签为空=无条件
        /// </summary>
        private bool HasCondition(TeleportCondition condition) {
            return condition != null && !string.IsNullOrEmpty(condition.LeftParamSign);
        }

        /// <summary>
        /// 前置条件 左边标签为空=无条件命中 否则左右比较通过才放行 只读 Data 不引用其他行为
        /// 数值走差值比较 字符串/向量仅支持相等与不等 布尔按 1/0 参与数值比较
        /// </summary>
        private bool IsConditionTrue(TeleportCondition condition) {
            if (condition == null || string.IsNullOrEmpty(condition.LeftParamSign)) {
                return true;
            }

            if (!TryGetWatchEntity(condition.LeftEntitySign, out Entity leftEntity)) {
                return false;
            }

            if (!TryReadValue(leftEntity, condition.LeftParamSign, condition.LeftValueType, out object leftValue)) {
                return false;
            }

            object rightValue;
            ParamValueType rightType = condition.LeftValueType;
            if (condition.RightIsEntityParam) {
                if (string.IsNullOrEmpty(condition.RightParamSign)) {
                    return false;
                }

                if (!TryGetWatchEntity(condition.RightEntitySign, out Entity rightEntity)) {
                    return false;
                }

                rightType = condition.RightValueType;
                if (!TryReadValue(rightEntity, condition.RightParamSign, rightType, out rightValue)) {
                    return false;
                }
            } else {
                rightType = condition.RightConstType;
                rightValue = GetConstValue(condition);
            }

            return CompareValues(leftValue, rightValue, rightType, condition.Compare);
        }

        /// <summary>
        /// 积木连接 按配置解析要读的实体 空=读自己 非空=按Sign读其他实体的Data 行为不感知对方类型
        /// </summary>
        private bool TryGetWatchEntity(string sign, out Entity watchEntity) {
            if (string.IsNullOrEmpty(sign)) {
                watchEntity = entity;
                return true;
            }

            return EntityRegister.TryGetEntityBySign(sign, out watchEntity);
        }

        /// <summary>
        /// 通用读值 按类型读 Int/Float/Bool/String/Vector3 拿不到返回 false
        /// </summary>
        private bool TryReadValue(Entity dataEntity, string sign, ParamValueType valueType, out object value) {
            value = null;
            if (dataEntity == null || string.IsNullOrEmpty(sign)) {
                return false;
            }

            switch (valueType) {
                case ParamValueType.Bool:
                    if (Cond.Instance.GetData<BoolData>(dataEntity, sign, out BoolData boolData)) {
                        value = boolData.Bool;
                        return true;
                    }

                    return false;
                case ParamValueType.Int:
                    if (Cond.Instance.GetData<IntData>(dataEntity, sign, out IntData intData)) {
                        value = intData.Int;
                        return true;
                    }

                    return false;
                case ParamValueType.Float:
                    if (Cond.Instance.GetData<FloatData>(dataEntity, sign, out FloatData floatData)) {
                        value = floatData.Float;
                        return true;
                    }

                    return false;
                case ParamValueType.String:
                    if (Cond.Instance.GetData<StringData>(dataEntity, sign, out StringData stringData)) {
                        value = stringData.String;
                        return true;
                    }

                    return false;
                case ParamValueType.Vector3:
                    if (Cond.Instance.GetData<Vector3Data>(dataEntity, sign, out Vector3Data vector3Data)) {
                        value = vector3Data.Vector3;
                        return true;
                    }

                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 右边常量取值 与 RightConstType 对应
        /// </summary>
        private object GetConstValue(TeleportCondition condition) {
            switch (condition.RightConstType) {
                case ParamValueType.Bool:
                    return condition.RightBoolValue;
                case ParamValueType.Int:
                    return condition.RightIntValue;
                case ParamValueType.Float:
                    return condition.RightConstValue;
                case ParamValueType.String:
                    return condition.RightStringValue;
                case ParamValueType.Vector3:
                    return condition.RightVector3Value;
                default:
                    return condition.RightConstValue;
            }
        }

        /// <summary>
        /// 通用比较 数值含 Bool 按大小比 字符串/向量只比相等与不等
        /// </summary>
        private bool CompareValues(object leftValue, object rightValue, ParamValueType valueType, TeleportCompare compare) {
            switch (valueType) {
                case ParamValueType.Bool:
                    return CompareNumbers(ToNumber(leftValue), ToNumber(rightValue), compare);
                case ParamValueType.Int:
                case ParamValueType.Float:
                    return CompareNumbers(ToNumber(leftValue), ToNumber(rightValue), compare);
                case ParamValueType.String: {
                    string left = leftValue as string ?? "";
                    string right = rightValue as string ?? "";
                    if (compare == TeleportCompare.Equal) {
                        return left == right;
                    }

                    if (compare == TeleportCompare.NotEqual) {
                        return left != right;
                    }

                    LogUtil.LogErrorFormat("行为:{0} 字符串只支持相等/不等比较!", BehaviourSign);
                    return false;
                }
                case ParamValueType.Vector3: {
                    if (!(leftValue is Vector3 left) || !(rightValue is Vector3 right)) {
                        return false;
                    }

                    if (compare == TeleportCompare.Equal) {
                        return Vector3.SqrMagnitude(left - right) < 0.000001f;
                    }

                    if (compare == TeleportCompare.NotEqual) {
                        return Vector3.SqrMagnitude(left - right) >= 0.000001f;
                    }

                    LogUtil.LogErrorFormat("行为:{0} 向量只支持相等/不等比较!", BehaviourSign);
                    return false;
                }
                default:
                    return false;
            }
        }

        private float ToNumber(object value) {
            if (value is bool b) {
                return b ? 1f : 0f;
            }

            if (value is int i) {
                return i;
            }

            if (value is float f) {
                return f;
            }

            return 0f;
        }

        private bool CompareNumbers(float left, float right, TeleportCompare compare) {
            switch (compare) {
                case TeleportCompare.Greater:
                    return left > right;
                case TeleportCompare.GreaterEqual:
                    return left >= right;
                case TeleportCompare.Equal:
                    return Mathf.Abs(left - right) < 0.001f;
                case TeleportCompare.LessEqual:
                    return left <= right;
                case TeleportCompare.Less:
                    return left < right;
                case TeleportCompare.NotEqual:
                    return Mathf.Abs(left - right) >= 0.001f;
                default:
                    return left >= right;
            }
        }

        public override void Clear() {
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            base.Clear();
        }
    }
}
