using GraphProcessor;
using LazyPan;

/// <summary>传送流程节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_TeleportFlow))]
public class TeleportFlowNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
