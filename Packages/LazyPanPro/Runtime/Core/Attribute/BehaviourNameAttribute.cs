using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 标记该 string 字段为行为中文名字段 编辑器下显示为 BehaviourConfig 下拉而非文本框
    /// 适用: GlobalMapColors.BehaviourColorEntry.Name 等
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class BehaviourNameAttribute : PropertyAttribute {
    }
}
