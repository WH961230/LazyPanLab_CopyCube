using UnityEngine;

namespace LazyPan {
    public class Flow_SceneC : Flow {
		private Comp UI_SceneC;

		private Entity Obj_Terrain_SceneC_Terrain;
		private Entity Obj_Tower_SceneC_Tower;
		private Entity Obj_Player_SceneC_Player;
		private Entity Obj_Camera_SceneC_Camera;
		private Entity Obj_Wave_SceneC_WaveManager;
		private Entity Obj_Generate_SceneC_EnemyGenerate;
		private Entity Obj_UI_SceneC_HUD;
		private Entity Obj_UI_SceneC_PickOneOfThree;

        public override void Init(Flow baseFlow) {
            base.Init(baseFlow);
            ConsoleEx.Instance.ContentSave("flow", "Flow_SceneC  场景C流程");
			UI_SceneC = UI.Instance.Open("UI_SceneC");

			Obj_Terrain_SceneC_Terrain = Obj.Instance.LoadEntity("Obj_Terrain_SceneC_Terrain");
			Obj_Tower_SceneC_Tower = Obj.Instance.LoadEntity("Obj_Tower_SceneC_Tower");
			Obj_Player_SceneC_Player = Obj.Instance.LoadEntity("Obj_Player_SceneC_Player");
			Obj_Camera_SceneC_Camera = Obj.Instance.LoadEntity("Obj_Camera_SceneC_Camera");
			Obj_Wave_SceneC_WaveManager = Obj.Instance.LoadEntity("Obj_Wave_SceneC_WaveManager");
			Obj_Generate_SceneC_EnemyGenerate = Obj.Instance.LoadEntity("Obj_Generate_SceneC_EnemyGenerate");
			Obj_UI_SceneC_HUD = Obj.Instance.LoadEntity("Obj_UI_SceneC_HUD");
			Obj_UI_SceneC_PickOneOfThree = Obj.Instance.LoadEntity("Obj_UI_SceneC_PickOneOfThree");

        }

		/*获取UI*/
		public override Comp GetUI() {
			return UI_SceneC;
		}


        /*下一步*/
        public override void Next(string teleportSceneSign) {
            Clear();
            Launch.instance.StageLoad(teleportSceneSign);
        }

        public override void Clear() {
            base.Clear();
			Obj.Instance.UnLoadEntity(Obj_UI_SceneC_PickOneOfThree);
			Obj.Instance.UnLoadEntity(Obj_UI_SceneC_HUD);
			Obj.Instance.UnLoadEntity(Obj_Generate_SceneC_EnemyGenerate);
			Obj.Instance.UnLoadEntity(Obj_Wave_SceneC_WaveManager);
			Obj.Instance.UnLoadEntity(Obj_Camera_SceneC_Camera);
			Obj.Instance.UnLoadEntity(Obj_Player_SceneC_Player);
			Obj.Instance.UnLoadEntity(Obj_Tower_SceneC_Tower);
			Obj.Instance.UnLoadEntity(Obj_Terrain_SceneC_Terrain);

			UI.Instance.Close("UI_SceneC");

        }
    }
}