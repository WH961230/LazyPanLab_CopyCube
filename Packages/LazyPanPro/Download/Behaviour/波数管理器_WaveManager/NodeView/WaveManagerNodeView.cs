using GraphProcessor;
using LazyPan;

/// <summary>波次管理节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_WaveManager))]
public class WaveManagerNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
