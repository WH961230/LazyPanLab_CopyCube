using System;

namespace LazyPan {
    /// <summary>
    /// 全局总览打开钩子 包程序集无法直接引用 Assets 侧类型 由外部程序集启动时赋值委托
    /// F2 先走钩子开图（Assets/Editor/GlobalEntityMapWindow），钩子没赋值才落回包内文字版
    /// </summary>
    public static class GlobalMapHook {
        /// <summary>打开全局图总览 无参数</summary>
        public static Action OpenGlobalMap;
    }
}
