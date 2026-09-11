using UnityEngine;

namespace LazyPan {
    public class Flow_SceneB : Flow {
		private Comp UI_SceneB;

		private Entity Obj_Terrain_Terrain1;
		private Entity Obj_Tower_Tower1;
		private Entity Obj_Player_Player1;
		private Entity Obj_Camera_Camera1;

        public override void Init(Flow baseFlow) {
            base.Init(baseFlow);
            ConsoleEx.Instance.ContentSave("flow", "Flow_SceneB  场景B流程");
			UI_SceneB = UI.Instance.Open("UI_SceneB");

			Obj_Terrain_Terrain1 = Obj.Instance.LoadEntity("Obj_Terrain_Terrain1");
			Obj_Tower_Tower1 = Obj.Instance.LoadEntity("Obj_Tower_Tower1");
			Obj_Player_Player1 = Obj.Instance.LoadEntity("Obj_Player_Player1");
			Obj_Camera_Camera1 = Obj.Instance.LoadEntity("Obj_Camera_Camera1");

        }

		/*获取UI*/
		public override Comp GetUI() {
			return UI_SceneB;
		}


        /*下一步*/
        public override void Next(string teleportSceneSign) {
            Clear();
            Launch.instance.StageLoad(teleportSceneSign);
        }

        public override void Clear() {
            base.Clear();
			Obj.Instance.UnLoadEntity(Obj_Camera_Camera1);
			Obj.Instance.UnLoadEntity(Obj_Player_Player1);
			Obj.Instance.UnLoadEntity(Obj_Tower_Tower1);
			Obj.Instance.UnLoadEntity(Obj_Terrain_Terrain1);

			UI.Instance.Close("UI_SceneB");

        }
    }
}