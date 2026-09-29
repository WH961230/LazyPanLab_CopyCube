using UnityEngine;


namespace LazyPan {
    public class Behaviour_Event_UIFloatingWindow : Behaviour {
        /// <summary>浮窗节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【浮窗】占位行为，暂无功能，不用挂。\n" +
            "要在屏幕上挂 HUD 请用 <color=#FFD54F>屏幕UI</color>。";
        public Behaviour_Event_UIFloatingWindow(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
        }

        public override void DelayedExecute() {
            
        }



        public override void Clear() {
            base.Clear();
        }
    }
}