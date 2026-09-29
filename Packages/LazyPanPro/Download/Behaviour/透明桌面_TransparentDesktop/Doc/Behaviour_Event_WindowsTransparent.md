# Behaviour_Event_WindowsTransparent 透明桌面

一句话：Windows 桌面透明穿透行为（仅非编辑器打包运行生效）。

原理：非 UNITY_EDITOR 下调 user32/Dwmapi 把窗口设为 layered+穿透+顶置；每帧用 EventSystem 射线检测鼠标是否在 2D UI 上，在 UI 上就恢复可点击，否则穿透。Application.runInBackground=true。

注意：无 Setting（SettingScript）配置，即装即用；编辑器下只跑穿透判断逻辑，不调 Win32 API。

## 视频

暂无。录好后放同目录 `Behaviour_Event_WindowsTransparent.mp4`，或写 `.url.txt` 放在线链接。
