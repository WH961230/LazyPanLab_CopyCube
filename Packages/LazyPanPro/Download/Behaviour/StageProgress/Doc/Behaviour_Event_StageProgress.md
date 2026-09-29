# Behaviour_Event_StageProgress StageProgress

一句话：阶段进度 — 通用阶段与上限关系，不管业务词。只认阶段钥匙(Int)与进度值(Float)及一张阶段对照上限表。

## Setting 字段

| 字段 | 含义 |
|---|---|
| SourceSign | 使用该行为的实体类型 SourceSign | 使用该行为的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Player_SceneC_Player |
| StageParamSign | 阶段钥匙标签 必填 Int类型 | 阶段钥匙的 Data 标签名，如 Level。必须是 IntData。不允许为空 |
| MaxStageParamSign | 阶段上限标签 选填 Int类型 为空=不同步 | 阶段上限的 Data 标签名，如 MaxLevel。必须是 IntData。每一帧同步为满级阶段，供界面与其他行为读取用。为留给老存档可置空，为置可空则不同步 |
| ProgressParamSign | 进度数值标签 必填 Float类型 | 进度数值的 Data 标签名，如 Exp。必须是 FloatData。不允许为空 |
| MaxProgressParamSign | 进度上限标签 选填 Float类型 为空=不同步 | 进度上限的 Data 标签名，如 MaxExp。必须是 FloatData。为每一帧同步为当前阶段的上限，供血条/滑条显示用。为解决为什么升级了条还按100算的问题。为留给老存档可置空，为置可空则不同步 |

## 使用步骤

1. 在可视化编辑器 Behaviour 标签页找到 Behaviour_Event_StageProgress，确认配置数据已生成。
2. 按上表在 Setting 资源里为你的 ObjConfig.Sign 加一条。
3. 需要演示视频时，在本 Doc 目录放 Behaviour_Event_StageProgress.mp4，或写 Behaviour_Event_StageProgress.url.txt 放一行在线链接。
