using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 标记该 string 字段为实体 Sign 字段 编辑器下显示为实体下拉而非文本框
    /// 适用: SourceSign / TargetEntitySign / TriggerEntitySign / WatchEntitySign / WaitWatchEntitySign 等
    /// IncludeTriggerer 置 true 时下拉额外提供 Triggerer(触发者) 选项 仅触发器动作的目标实体用
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EntitySignAttribute : PropertyAttribute {
        public readonly bool IncludeTriggerer;

        public EntitySignAttribute() {
        }

        public EntitySignAttribute(bool includeTriggerer) {
            IncludeTriggerer = includeTriggerer;
        }
    }
}
