# Behaviour_Event_BeginLogo 开始 LOGO

一句话：显示开场 Logo 预制体，倒计时结束跳场景。

原理：InitBinding 时从当前 Flow 拿 UI 的 Root，按 UIChildPrefabSign 加载 `UI_Logo`，LogoContinueTime 倒计时走完调 `flow.Next(EndJumpToScene)`。

## Setting（BeginLogoSetting）字段

| 字段 | 含义 |
|---|---|
| SourceSign | 发起挂载的实体类型，与 ObjConfig.Sign 一致 |
| UIParentPrefabSign | 挂载在哪个 UI 预制体上面 |
| UIChildPrefabSign | 挂载哪个子预制体（传给 Loader.LoadGo("UI_Logo", …)） |
| LogoContinueTime | 播放时间（秒） |
| EndJumpToScene | 播放完成后跳转场景 |

## 使用步骤

1. SourceSign 对上 Logo 实体，UIChildPrefabSign 填 Logo 子预制体。
2. LogoContinueTime 给几秒播几秒，EndJumpToScene 填下一场景名。

## 视频

暂无。录好后放同目录 `Behaviour_Event_BeginLogo.mp4`，或写 `.url.txt` 放在线链接。
