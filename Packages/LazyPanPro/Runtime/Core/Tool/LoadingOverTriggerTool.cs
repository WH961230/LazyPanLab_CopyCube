using UnityEngine;
using UnityEngine.Events;

namespace LazyPan {
    /// <summary>
    /// 游戏加载结束之后触发事件工具
    /// </summary>
    public class LoadingOverTriggerTool : MonoBehaviour {
        public UnityEvent OnLoadingOverEvent = new UnityEvent();
        private void Awake() {
            Game.instance.OnLoadingOverEvent.AddListener(() => {
                OnLoadingOverEvent.Invoke();
            });
        }
    }
}