using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GraphProcessor;

namespace LazyPan {
    /// <summary>
    /// 通用实体图窗口（包内）：只认实体 Sign，不读 ObjConfig、不碰 Setting、不落盘，纯内存空图。
    /// 新项目只拉包时由它接住“实体设置”按钮，保证点得开、有反馈；
    /// 跑完引导拷贝业务样板后，Assets 的完整版窗口会自动接管（见接管规则）。
    /// 接管规则：本窗口只在 hook 为空时注册；完整版无条件赋值，所以无论初始化顺序如何，有完整版时永远是完整版生效。
    /// </summary>
    public class GenericEntityGraphWindow : BaseGraphWindow {
        private static string _lastSign = "";
        private static string[] _lastBehaviours = new string[0];

        [InitializeOnLoadMethod]
        private static void RegisterGeneric() {
            if (EntityGraphHook.OpenEntityGraph == null) {
                EntityGraphHook.OpenEntityGraph = Open;
            }
        }

        public static void Open(string entitySign, string[] behaviourNames) {
            _lastSign = entitySign ?? "";
            _lastBehaviours = behaviourNames ?? new string[0];
            var window = CreateWindow<GenericEntityGraphWindow>();
            var graph = ScriptableObject.CreateInstance<BaseGraph>();
            graph.name = _lastSign;
            window.InitializeGraph(graph);
            window.Show();
        }

        protected override void InitializeWindow(BaseGraph graph) {
            if (graphView == null) {
                graphView = new LazyPanGraphView(this);
            }
            rootView.Add(graphView);
            BuildGenericBanner(graph);
        }

        private void BuildGenericBanner(BaseGraph graph) {
            var old = rootView.Q<VisualElement>("entity-banner");
            if (old != null) {
                rootView.Remove(old);
            }
            string sign = !string.IsNullOrEmpty(_lastSign) ? _lastSign : (graph != null ? graph.name : "");
            var banner = new VisualElement { name = "entity-banner" };
            banner.style.flexDirection = FlexDirection.Column;
            banner.style.paddingLeft = 8;
            banner.style.paddingRight = 8;
            banner.style.paddingTop = 4;
            banner.style.paddingBottom = 4;
            var title = new UnityEngine.UIElements.Label($"{sign}（通用模式）");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            banner.Add(title);
            int count = _lastBehaviours != null ? _lastBehaviours.Length : 0;
            var hint = new UnityEngine.UIElements.Label(count > 0
                ? $"该实体配置了 {count} 个行为。跑完引导第二步拷贝业务样板后，这里自动升级为完整行为图。"
                : "空图（未读到该实体配置的行为）。跑完引导第二步拷贝业务样板后，这里自动升级为完整行为图。");
            hint.style.fontSize = 11;
            hint.style.whiteSpace = WhiteSpace.Normal;
            banner.Add(hint);
            rootView.Add(banner);
            titleContent = new GUIContent($"实体设置-{sign}");
        }
    }
}
