using GraphProcessor;
using LazyPan;

/// <summary>击退节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_Knockback))]
public class KnockbackNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
