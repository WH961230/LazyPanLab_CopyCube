using GraphProcessor;
using LazyPan;

/// <summary>屏幕UI节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_UIStatusDisplay))]
public class UIStatusDisplayNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
