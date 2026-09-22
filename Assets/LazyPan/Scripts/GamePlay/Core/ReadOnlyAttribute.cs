using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 只读标记：打在参数便签这类自动生成的字段上，图节点和 Inspector 只展示不给改。
    /// 值永远由 BehaviourPayloadDoc 反射重算，改了也会被刷回去。
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute {
    }
}
