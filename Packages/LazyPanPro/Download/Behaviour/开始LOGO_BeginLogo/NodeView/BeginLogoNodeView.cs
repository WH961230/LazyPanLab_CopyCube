using GraphProcessor;
using LazyPan;

/// <summary>开头Logo节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_BeginLogo))]
public class BeginLogoNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
