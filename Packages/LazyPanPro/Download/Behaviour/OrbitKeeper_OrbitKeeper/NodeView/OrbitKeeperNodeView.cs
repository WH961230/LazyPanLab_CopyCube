using GraphProcessor;
using LazyPan;

/// <summary>环绕养球节点视图：说明书折叠 + 上岗检查走共享装配，与其他行为一致。</summary>
[NodeCustomEditor(typeof(BehaviourNode_OrbitKeeper))]
public class OrbitKeeperNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
