using GraphProcessor;
using LazyPan;

/// <summary>实体UI绑定节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_EntityUIBinder))]
public class EntityUIBinderNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
