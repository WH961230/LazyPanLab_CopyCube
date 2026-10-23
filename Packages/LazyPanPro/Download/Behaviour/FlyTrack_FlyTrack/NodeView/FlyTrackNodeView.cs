using GraphProcessor;
using LazyPan;

/// <summary>飞行追踪节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_FlyTrack))]
public class FlyTrackNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
