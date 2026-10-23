using GraphProcessor;
using LazyPan;

/// <summary>体型扩散节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_BodyExpand))]
public class BodyExpandNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
