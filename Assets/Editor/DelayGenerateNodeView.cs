using GraphProcessor;
using LazyPan;

/// <summary>延时生成节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_DelayGenerate))]
public class DelayGenerateNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
