using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 标记该 string 字段为实体 Sign 字段 编辑器下显示为实体下拉而非文本框
    /// 适用: SourceSign / TargetEntitySign / TriggerEntitySign / WatchEntitySign / WaitWatchEntitySign 等
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EntitySignAttribute : PropertyAttribute {
    }
}
