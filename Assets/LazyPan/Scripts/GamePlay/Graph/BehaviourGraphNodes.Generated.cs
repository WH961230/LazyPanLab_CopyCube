using System;
using GraphProcessor;

namespace LazyPan {
    /// <summary>
    /// 控制器前后左右移动行为节点 对应 InputWASDMoveSettingData 一条
    /// 参数便签见 BehaviourPayloadDoc.Get(nameof(Behaviour_Auto_InputWASDMove))，节点身上只读显示
    /// </summary>
    [Serializable]
    [NodeMenuItem("LazyPan/行为/控制器前后左右移动")]
    public class BehaviourNode_InputWASDMove : BehaviourGraphNode {
        public InputWASDMoveSettingData Config;
        public override string name => "控制器前后左右移动";
        public override string BehaviourSign => nameof(Behaviour_Auto_InputWASDMove);
    }


}
