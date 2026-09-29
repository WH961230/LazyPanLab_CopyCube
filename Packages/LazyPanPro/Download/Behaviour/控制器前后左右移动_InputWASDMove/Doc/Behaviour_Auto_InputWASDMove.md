# Behaviour_Auto_InputWASDMove 控制器前后左右移动

一句话：读输入按 CharacterController 移动，带重力与转向。依赖 inputactions 与 CharacterController。

原理：InputRegister 加载 InputControlSign 监听二维输入；每帧 input 转世界方向，按 RotateSpeed 朝向平滑转向，按 MoveSpeed + Gravity（含贴地 -2 吸附）调 controller.Move。自身 MovementStop 为 true 则输入清零原地不动。

## Setting（InputWASDMoveSetting）字段

| 字段 | 含义 |
|---|---|
| SourceSign | 发起移动的实体类型，与 ObjConfig.Sign 一致 |
| InputControlSign | 输入动作标识（InputRegister 路径），如 Player/Motion，需在 LazyPanInputControl.inputactions 中存在 |
| MoveSpeed | 移动速度 |
| RotateSpeed | 转向速度，每秒转向系数，越大越快，0 不转向 |
| Gravity | 重力，负数向下（保持贴地） |

## 依赖

- Cond 能取到 CharacterController + Body（Transform）
- 输入表 LazyPanInputControl.inputactions 里有对应动作

## 使用步骤

1. 实体挂 CharacterController，输入表加动作。
2. Setting 里 SourceSign 对上实体，InputControlSign 填动作路径。
3. 调 MoveSpeed/RotateSpeed/Gravity；要定身就写 MovementStop=true。

## 视频

暂无。录好后放同目录 `Behaviour_Auto_InputWASDMove.mp4`，或写 `.url.txt` 放在线链接。
