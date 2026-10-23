using GraphProcessor;
using LazyPan;

/// <summary>触发器节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_EntityTriggerController))]
public class EntityTriggerControllerNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
