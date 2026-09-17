#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为查看器全局设置
    /// </summary>
    public static class EntityBehaviourViewerSetting {
        public static bool Enabled = true;
    }

    /// <summary>
    /// 实体行为查看器(仅编辑器) 挂载于实体body 运行时实时显示当前绑定的行为
    /// </summary>
    public class EntityBehaviourViewer : MonoBehaviour {
        private const float RefreshInterval = 0.5f;

        public int EntityID;
        private float nextRefreshTime;
        private string content;
        private static readonly StringBuilder Builder = new StringBuilder();

        public void Init(int entityID) {
            EntityID = entityID;
            Refresh();
        }

        private void Update() {
            if (Time.unscaledTime >= nextRefreshTime) {
                Refresh();
            }
        }

        private void Refresh() {
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
            Builder.Clear();

            if (!EntityRegister.TryGetEntityByID(EntityID, out Entity entity)) {
                content = null;
                return;
            }

            Builder.AppendLine($"[ID:{entity.ID}] {entity.ObjConfig?.Name}");
            if (BehaviourRegister.GetBehaviours(EntityID, out List<Behaviour> behaviours)) {
                for (int i = 0; i < behaviours.Count; i++) {
                    Builder.AppendLine($"· {behaviours[i].BehaviourName} ");
                }
            } else {
                Builder.AppendLine("· 无行为");
            }

            content = Builder.ToString();
        }

        private void OnGUI() {
            if (!EntityBehaviourViewerSetting.Enabled || string.IsNullOrEmpty(content)) {
                return;
            }

            Camera camera = Camera.current != null ? Camera.current : Camera.main;
            if (camera == null) {
                return;
            }

            Vector3 screenPoint = camera.WorldToScreenPoint(transform.position);
            if (screenPoint.z < 0) {
                return;
            }

            Vector2 guiPoint = new Vector2(screenPoint.x, camera.pixelHeight - screenPoint.y);
            GUIStyle style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft };
            Vector2 size = style.CalcSize(new GUIContent(content));//按实际文字计算框体大小
            Rect rect = new Rect(guiPoint.x + 12f, guiPoint.y - size.y - 8f, size.x, size.y);
            GUI.Box(rect, content, style);
        }

        private void OnDrawGizmos() {
            if (!EntityBehaviourViewerSetting.Enabled || string.IsNullOrEmpty(content)) {
                return;
            }

            Handles.Label(transform.position, content);
        }
    }

    public static class EntityBehaviourViewerMenu {
        [MenuItem("Tools/LazyPan/实体行为查看器 开关")]
        public static void Toggle() {
            EntityBehaviourViewerSetting.Enabled = !EntityBehaviourViewerSetting.Enabled;
            Debug.Log($"实体行为查看器:{(EntityBehaviourViewerSetting.Enabled ? "开启" : "关闭")}");
        }
    }
}
#endif
