using GraphProcessor;
using LazyPan;

/// <summary>瞬间移动节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_Teleportation))]
public class TeleportationNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
