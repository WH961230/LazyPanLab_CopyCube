using UnityEngine;


namespace LazyPan {
    public class Behaviour_Event_UIBinder : Behaviour {
        /// <summary>UI绑定节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【UI绑定】占位行为，暂无功能，不用挂。\n" +
            "要在实体上挂 UI 请用 <color=#FFD54F>实体UI绑定</color>，要在屏幕上挂 HUD 请用 <color=#FFD54F>屏幕UI</color>。";
        public Behaviour_Event_UIBinder(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
        }

        public override void DelayedExecute() {
            
        }



        public override void Clear() {
            base.Clear();
        }
    }
}