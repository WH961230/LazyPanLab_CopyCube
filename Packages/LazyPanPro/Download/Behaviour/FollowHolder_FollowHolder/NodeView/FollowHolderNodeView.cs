using GraphProcessor;
using LazyPan;

/// <summary>跟随主人节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_FollowHolder))]
public class FollowHolderNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
