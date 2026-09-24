using GraphProcessor;
using LazyPan;

/// <summary>
/// 控制器移动节点视图：说明书折叠走共享装配。
/// </summary>
[NodeCustomEditor(typeof(BehaviourNode_InputWASDMove))]
public class InputWASDMoveNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
