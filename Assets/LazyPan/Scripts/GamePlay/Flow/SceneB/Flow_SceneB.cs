using UnityEngine;

namespace LazyPan {
    public class Flow_SceneB : Flow {
		private Comp UI_SceneB;

		private Entity Obj_Terrain_SceneB_Terrain;
		private Entity Obj_Tower_SceneB_Tower;
		private Entity Obj_Player_SceneB_Player;
		private Entity Obj_Camera_SceneB_Camera;

        public override void Init(Flow baseFlow) {
            base.Init(baseFlow);
            ConsoleEx.Instance.ContentSave("flow", "Flow_SceneB  场景B流程");
			UI_SceneB = UI.Instance.Open("UI_SceneB");

			Obj_Terrain_SceneB_Terrain = Obj.Instance.LoadEntity("Obj_Terrain_SceneB_Terrain");
			Obj_Tower_SceneB_Tower = Obj.Instance.LoadEntity("Obj_Tower_SceneB_Tower");
			Obj_Player_SceneB_Player = Obj.Instance.LoadEntity("Obj_Player_SceneB_Player");
			Obj_Camera_SceneB_Camera = Obj.Instance.LoadEntity("Obj_Camera_SceneB_Camera");

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
			Obj.Instance.UnLoadEntity(Obj_Camera_SceneB_Camera);
			Obj.Instance.UnLoadEntity(Obj_Player_SceneB_Player);
			Obj.Instance.UnLoadEntity(Obj_Tower_SceneB_Tower);
			Obj.Instance.UnLoadEntity(Obj_Terrain_SceneB_Terrain);

			UI.Instance.Close("UI_SceneB");

        }
    }
}