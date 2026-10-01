using System;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.EventSystems;

namespace LazyPan {
    
    /// <summary>
    /// #桌面透明#
    /// </summary>
    
    public class Behaviour_Event_WindowsTransparent : Behaviour {
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        private static extern int SetWindowPos(IntPtr A, IntPtr B, uint a, uint b, uint c, uint d, uint e);

        [DllImport("user32.dll")]
        static extern int SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private struct Margins {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("Dwmapi.dll")]
        private static extern uint DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        const int GWL_EXSTYLE = -20;
        private const uint WS_EX_LAYERED = 0x00080000;
        private const uint WS_EX_TRANSPERENT = 0x00000020;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        private const uint LWA_EVOREEK = 0x00000001;

        private IntPtr hwnd;
        
        public Behaviour_Event_WindowsTransparent(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
#if !UNITY_EDITOR
        hwnd = GetActiveWindow();
        Margins margins = new Margins{ cxLeftWidth = -1};
        DwmExtendFrameIntoClientArea(hwnd, ref margins);
        SetWindowLong(hwnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPERENT);
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, 0);
#endif
            Application.runInBackground = true;
            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {
        }
        
        private void OnUpdate() {
            SetClickThrough(!IsMouseOver2DUI());
        }

        bool IsMouseOver2DUI() {
            // 获取 EventSystem 实例
            EventSystem eventSystem = EventSystem.current;
            // 创建一个 PointerEventData 对象
            PointerEventData pointerEventData = new PointerEventData(eventSystem);
            // 设置鼠标位置
            pointerEventData.position = Input.mousePosition;
            // 创建一个列表来存储射线检测结果
            var results = new System.Collections.Generic.List<RaycastResult>();
            // 使用 GraphicRaycaster 进行射线检测
            eventSystem.RaycastAll(pointerEventData, results);
            // 如果检测到 UI 元素，返回 true
            return results.Count > 0;
        }

        void SetClickThrough(bool clickThrough) {
            if (clickThrough) {
                SetWindowLong(hwnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPERENT);
            } else {
                SetWindowLong(hwnd, GWL_EXSTYLE, WS_EX_LAYERED);
            }
        }
        
        public override void Clear() {
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            base.Clear();
        }
    }
}