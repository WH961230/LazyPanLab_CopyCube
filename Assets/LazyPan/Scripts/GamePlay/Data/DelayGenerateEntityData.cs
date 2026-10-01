using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 延时生成实体数据 仅承载行为参数 不含运行时状态与业务依赖
    /// </summary>
    public class DelayGenerateEntityData : Data {
        [Header("延时生成行为参数")] public DelayGenerateEntityConfig Config = new DelayGenerateEntityConfig();

        [Serializable]
        public class DelayGenerateEntityConfig {
            [Header("生成类型")] public GenerateType GenerateType;
            [Header("生成间隔")] public float IntervalTime;
            [Header("生成实体标识")] public string GenerateEntitySign;
            [Header("触发档案")] public System.Collections.Generic.List<DelayGenerateProfile> Profiles = new System.Collections.Generic.List<DelayGenerateProfile>();
        }
    }
}