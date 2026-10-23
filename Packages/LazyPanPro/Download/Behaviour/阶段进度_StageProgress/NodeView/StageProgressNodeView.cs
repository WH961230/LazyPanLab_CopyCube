using GraphProcessor;
using LazyPan;

/// <summary>阶段进度节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_StageProgress))]
public class StageProgressNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
