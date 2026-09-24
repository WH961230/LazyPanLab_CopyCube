using GraphProcessor;
using LazyPan;

/// <summary>接触伤害节点视图：说明书折叠走共享装配。</summary>
[NodeCustomEditor(typeof(BehaviourNode_ContactDamage))]
public class ContactDamageNodeView : BaseNodeView {
    public override void Enable() {
        base.Enable();
        NodeMemoHelper.Attach(this);
    }
}
