using System.Linq;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using GraphProcessor;
using LazyPan;

/// <summary>
/// 死亡节点视图：说明书折叠走共享装配，另藏掉 SourceSign 那行的全部文字只留选择框。
/// </summary>
[NodeCustomEditor(typeof(BehaviourNode_Death))]
public class DeathNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this, HideSourceSignTitles);
    }

    /// <summary>
    /// SourceSign 那行只留选择框：行里的灰色小标题和上面黑色 Header 一起藏。
    /// </summary>
    internal static void HideSourceSignTitles(VisualElement configField) {
        foreach (var child in configField.Query<PropertyField>().ToList()) {
            if (child.parent != configField) {
                continue;
            }

            string path = child.bindingPath ?? "";
            if (!path.EndsWith(".SourceSign")) {
                continue;
            }

            var grayTitle = child.Q<UnityEngine.UIElements.Label>(className: "unity-property-field__label")
                ?? child.Q<UnityEngine.UIElements.Label>();
            if (grayTitle != null) {
                grayTitle.style.display = DisplayStyle.None;
            }

            int idx = configField.IndexOf(child);
            if (idx > 0 && configField[idx - 1] is UnityEngine.UIElements.Label header) {
                header.style.display = DisplayStyle.None;
            }
        }
    }

    internal static void AlignConfigRows(VisualElement configField) {
        NodeMemoHelper.AlignConfigRows(configField);
    }

}
