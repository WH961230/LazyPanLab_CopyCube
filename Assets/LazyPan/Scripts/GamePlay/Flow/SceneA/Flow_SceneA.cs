using UnityEngine;

namespace LazyPan {
    public class Flow_SceneA : Flow {
		private Comp UI_SceneA;

		private Entity Obj_Logo_SceneA_BeginLogo;
		private Entity Obj_Camera_SceneA_Camera;

        public override void Init(Flow baseFlow) {
            base.Init(baseFlow);
            ConsoleEx.Instance.ContentSave("flow", "Flow_SceneA  场景A流程");
			UI_SceneA = UI.Instance.Open("UI_SceneA");

			Obj_Logo_SceneA_BeginLogo = Obj.Instance.LoadEntity("Obj_Logo_SceneA_BeginLogo");
			Obj_Camera_SceneA_Camera = Obj.Instance.LoadEntity("Obj_Camera_SceneA_Camera");

        }

		/*获取UI*/
		public override Comp GetUI() {
			return UI_SceneA;
		}


        /*下一步*/
        public override void Next(string teleportSceneSign) {
            Clear();
            Launch.instance.StageLoad(teleportSceneSign);
        }

        public override void Clear() {
            base.Clear();
			Obj.Instance.UnLoadEntity(Obj_Camera_SceneA_Camera);
			Obj.Instance.UnLoadEntity(Obj_Logo_SceneA_BeginLogo);

			UI.Instance.Close("UI_SceneA");

        }
    }
}