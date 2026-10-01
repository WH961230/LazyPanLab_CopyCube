using System.Collections.Generic;
using GraphProcessor;
using LazyPan;

/// <summary>追踪实体节点视图：说明书 + 上岗检查走共享装配，地形导航组件实查挂在加查上。</summary>
[NodeCustomEditor(typeof(BehaviourNode_TrackingEntity))]
public class TrackingEntityNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this, null, TrackingNavCheck);
    }

    /// <summary>
    /// 地形导航实查：不用开场景，直接翻地形预制体，看有没有导航组件，有没有都给结论。
    /// </summary>
    internal static void TrackingNavCheck(object config, List<string> red, List<string> yellow) {
        if (!(config is TrackingEntitySettingData c) || string.IsNullOrEmpty(c.NavMeshTerrainSign)) {
            return;
        }

        var prefab = NodeMemoHelper.FindEntityPrefab(c.NavMeshTerrainSign);
        if (prefab == null) {
            yellow.Add($"找不到地形 {c.NavMeshTerrainSign} 的预制体，确认 Prefabs/Obj 下有同名预制体");
            return;
        }

        var found = new HashSet<string>();
        foreach (var comp in prefab.GetComponentsInChildren<UnityEngine.Component>(true)) {
            if (comp == null) {
                continue;
            }

            string n = comp.GetType().Name;
            if (n.StartsWith("NavMesh") || n == "OffMeshLink") {
                found.Add(n);
            }
        }

        if (found.Count == 0) {
            red.Add($"地形 {c.NavMeshTerrainSign} 预制体上没有导航组件（NavMeshSurface 这类），先挂上");
        }
    }
}
