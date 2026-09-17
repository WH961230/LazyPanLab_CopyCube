using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using GraphProcessor;
using LazyPan;

/// <summary>
/// 过滤节点创建菜单 只放行 LazyPan 的行为图节点 自动收集 BehaviourGraphNode 全部子类 屏蔽库示例节点
/// </summary>
public class LazyPanGraphView : BaseGraphView {
    static List<(string path, Type type)> cachedEntries;

    public LazyPanGraphView(EditorWindow window) : base(window) {
    }

    public override IEnumerable<(string path, Type type)> FilterCreateNodeMenuEntries() {
        if (cachedEntries == null) {
            cachedEntries = new List<(string path, Type type)>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<BehaviourGraphNode>()) {
                if (type.IsAbstract)
                    continue;
                var attr = Attribute.GetCustomAttribute(type, typeof(NodeMenuItemAttribute), false) as NodeMenuItemAttribute;
                var path = attr != null ? attr.menuTitle : $"LazyPan/行为/{type.Name}";
                cachedEntries.Add((path: path, type: type));
            }
        }

        foreach (var entry in cachedEntries)
            yield return entry;
    }

    /// <summary>
    /// Ctrl/Cmd+E 全部展开 Ctrl/Cmd+Shift+E 全部收缩(库 KeyDownCallback 为 virtual 在此扩展)
    /// 同时折叠节点外壳和里面的数据 Foldout 数据区才跟着动
    /// </summary>
    protected override void KeyDownCallback(KeyDownEvent e) {
        if ((e.ctrlKey || e.commandKey) && e.keyCode == UnityEngine.KeyCode.E) {
            SetAllExpanded(!e.shiftKey);
            e.StopPropagation();
            return;
        }

        base.KeyDownCallback(e);
    }

    void SetAllExpanded(bool expanded) {
        foreach (var view in nodeViews) {
            if (view == null || view.nodeTarget == null)
                continue;
            view.nodeTarget.expanded = expanded;
            view.expanded = expanded;
            view.RefreshExpandedState();
            foreach (var foldout in view.Query<Foldout>().ToList())
                foldout.value = expanded;
        }
    }
}
