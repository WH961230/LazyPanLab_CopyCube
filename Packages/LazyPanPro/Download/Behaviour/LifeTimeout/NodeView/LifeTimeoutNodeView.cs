using GraphProcessor;
using LazyPan;

/// <summary>寿命节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_LifeTimeout))]
public class LifeTimeoutNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
