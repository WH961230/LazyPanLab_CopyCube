using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 按同对象上另一个字段的值决定本字段显隐 满足条件才显示
    /// 用法: [ShowIf("WeaponType", WeaponKind.Direct)] 或 [ShowIf("UseCustom", true)] 或 [ShowIf("Name", "火球")]
    /// 只管显示不管数据 藏起来的值还在 运行时按类型只读自己那几个字段 别读脏数据
    /// 注意: 需要标题就用第三个参数 [ShowIf("Mode", Mode.Ratio, "最大值标签")] 不要再叠 [Header]
    /// 因为 Unity 的 Header 是独立绘制的 藏字段藏不住它 标题必须由 ShowIf 自己画
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowIfAttribute : PropertyAttribute {
        public readonly string ConditionField;
        public readonly object ExpectValue;
        public readonly string Header;

        public ShowIfAttribute(string conditionField, object expectValue) {
            ConditionField = conditionField;
            ExpectValue = expectValue;
        }

        public ShowIfAttribute(string conditionField, object expectValue, string header) {
            ConditionField = conditionField;
            ExpectValue = expectValue;
            Header = header;
        }
    }
}
