using GraphProcessor;
using LazyPan;

/// <summary>装备挂载节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_EquipmentMount))]
public class EquipmentMountNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
