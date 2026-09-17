using System;

namespace LazyPan {
    /// <summary>
    /// 实体图打开钩子 包程序集无法直接引用 Assets 侧类型 由外部程序集启动时赋值委托
    /// </summary>
    public static class EntityGraphHook {
        /// <summary>打开实体图 参数: 实体Sign, 该行配置的行为中文名列表(ObjConfig.SetUpBehaviourName 按 | 拆分)</summary>
        public static Action<string, string[]> OpenEntityGraph;
    }
}