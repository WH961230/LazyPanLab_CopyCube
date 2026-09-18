using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 实体参数赋值
    /// 只做一件事: 按配置给自己或配置的目标实体写 Data 属性 不读业务不做判定 可一次配多条参数项
    /// 配置来源 Setting/ParamValueSetting 参数快照同步到自身 ParamValueData 便于查看与调试
    /// </summary>
    public class Behaviour_Event_ParamValue : Behaviour {
        private const string settingPath = "Setting/ParamValueSetting";

        //config
        private ParamValueData _paramData;
        private List<ParamValueData.ParamValueConfig> _configs = new List<ParamValueData.ParamValueConfig>();

        public Behaviour_Event_ParamValue(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _paramData = AttachBehaviourData<ParamValueData>();

            ParamValueSetting setting = Loader.LoadAsset<ParamValueSetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out ParamValueSettingData settingData)) {
                return;
            }

            CopySetting(settingData);
            if (_configs.Count == 0) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 参数项为空!", BehaviourSign, entity.ObjConfig.Sign);
                return;
            }

            foreach (ParamValueData.ParamValueConfig config in _configs) {
                if (config.ApplyOnInit) {
                    Apply(config);
                }
            }
        }

        public override void DelayedExecute() {
        }

        /// <summary>
        /// 配置拷贝到实体Data 与配置资产解耦 空标签项直接丢弃
        /// </summary>
        private void CopySetting(ParamValueSettingData settingData) {
            if (settingData.Items == null) {
                return;
            }

            foreach (ParamValueItem item in settingData.Items) {
                if (item == null) {
                    continue;
                }

                if (!BehaviourSigns.Require(item.TargetEntitySign, BehaviourSign, entity.ObjConfig?.Sign, nameof(ParamValueItem.TargetEntitySign))) {
                    continue;
                }

                if (!BehaviourSigns.Require(item.ParamSign, BehaviourSign, item.TargetEntitySign, nameof(ParamValueItem.ParamSign))) {
                    continue;
                }

                ParamValueData.ParamValueConfig config = new ParamValueData.ParamValueConfig() {
                    TargetEntitySign = item.TargetEntitySign,
                    ParamSign = item.ParamSign,
                    ValueType = item.ValueType,
                    BoolValue = item.BoolValue,
                    IntValue = item.IntValue,
                    FloatValue = item.FloatValue,
                    StringValue = item.StringValue,
                    Vector3Value = item.Vector3Value,
                    ApplyOnInit = item.ApplyOnInit,
                };
                _configs.Add(config);
                _paramData.Configs.Add(config);
            }
        }

        /// <summary>
        /// 对外唯一入口 执行全部参数项赋值 供触发器等外部按需调用
        /// </summary>
        public void Apply() {
            foreach (ParamValueData.ParamValueConfig config in _configs) {
                Apply(config);
            }
        }

        /// <summary>
        /// 按标签执行单项赋值 供外部精确触发 单项失败不影响其余项
        /// </summary>
        public bool Apply(string paramSign) {
            foreach (ParamValueData.ParamValueConfig config in _configs) {
                if (config.ParamSign == paramSign) {
                    return Apply(config);
                }
            }

            LogUtil.LogErrorFormat("行为:{0} 未找到参数项:{1}", BehaviourSign, paramSign);
            return false;
        }

        /// <summary>
        /// 单项赋值 目标不存在时返回 false 不抛异常 由调用方决定是否重试
        /// </summary>
        private bool Apply(ParamValueData.ParamValueConfig config) {
            if (!TryGetTargetEntity(config.TargetEntitySign, out Entity target)) {
                return false;
            }

            switch (config.ValueType) {
                case ParamValueType.Bool:
                    if (!Cond.Instance.TryGetData(target, config.ParamSign, out BoolData boolData)) {
                        return false;
                    }

                    boolData.Bool = config.BoolValue;
                    return true;
                case ParamValueType.Int:
                    if (!Cond.Instance.TryGetData(target, config.ParamSign, out IntData intData)) {
                        return false;
                    }

                    intData.Int = config.IntValue;
                    return true;
                case ParamValueType.Float:
                    if (!Cond.Instance.TryGetData(target, config.ParamSign, out FloatData floatData)) {
                        return false;
                    }

                    floatData.Float = config.FloatValue;
                    return true;
                case ParamValueType.String:
                    if (!Cond.Instance.TryGetData(target, config.ParamSign, out StringData stringData)) {
                        return false;
                    }

                    stringData.String = config.StringValue;
                    return true;
                case ParamValueType.Vector3:
                    if (!Cond.Instance.TryGetData(target, config.ParamSign, out Vector3Data vector3Data)) {
                        return false;
                    }

                    vector3Data.Vector3 = config.Vector3Value;
                    return true;
                default:
                    LogUtil.LogErrorFormat("行为:{0} 不支持的参数类型:{1}", BehaviourSign, config.ValueType);
                    return false;
            }
        }

        /// <summary>
        /// 积木连接 按配置解析要写的实体 Self=写自己 其他按Sign写其他实体的Data 行为不感知对方类型
        /// </summary>
        private bool TryGetTargetEntity(string targetEntitySign, out Entity target) {
            return BehaviourSigns.ResolveEntity(entity, BehaviourSign, nameof(ParamValueItem.TargetEntitySign), targetEntitySign, out target);
        }

        public override void Clear() {
            DetachBehaviourData<ParamValueData>();
            base.Clear();
        }
    }
}
