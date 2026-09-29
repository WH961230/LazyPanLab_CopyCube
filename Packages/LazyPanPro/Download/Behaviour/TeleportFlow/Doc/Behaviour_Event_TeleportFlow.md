# Behaviour_Event_TeleportFlow TeleportFlow

一句话：传送流程 — 到前置条件满足才切场景 不感知触发源业务 前置条件 null=无条件走内部请求(RequestTeleport) 非空则左右参数比较通过才放行

## Setting 字段

| 字段 | 含义 |
|---|---|
| SourceSign | 发起传送的实体类型 SourceSign | 发起传送的实体类型 必须与 ObjConfig.Sign 一致 如 Obj_Logo_SceneA_BeginLogo |
| TargetSceneSign | 目标场景标识 | 要跳转的场景标识 传给 Flow.Next 如 SceneB |
| LeftEntitySign | 左边实体 必填 Self=自己 | 左边数据源实体 Sign Self=读自己实体的 Data 如塔B填自己。不允许为空 |
| LeftParamSign | 左边参数标签 必填 | 左边 Data 标签名 如 Energy 不允许为空 |
| RightIsEntityParam | 右边是实体参数 | 勾上=右边读实体参数 不勾=右边用下面的常量值 |
| RightEntitySign | 右边实体 右边是参数时有效 必填 | 右边数据源实体 Sign 仅 RightIsEntityParam 勾上时有效 Self=读自己。不允许为空 |
| RightParamSign | 右边参数标签 右边是参数时有效 必填 | 右边 Data 标签名 如 MaxEnergy 仅 RightIsEntityParam 勾上时有效。不允许为空 |
| RightBoolValue | 右边布尔常量 | 右边常量类型=布尔时有效 |
| RightIntValue | 右边整数常量 | 右边常量类型=整数时有效 |
| RightConstValue | 右边浮点常量 | 右边常量类型=浮点时有效 |
| RightStringValue | 右边字符串常量 | 右边常量类型=字符串时有效 |
| RightVector3Value | 右边向量常量 | 右边常量类型=向量时有效 |

## 使用步骤

1. 在可视化编辑器 Behaviour 标签页找到 Behaviour_Event_TeleportFlow，确认配置数据已生成。
2. 按上表在 Setting 资源里为你的 ObjConfig.Sign 加一条。
3. 需要演示视频时，在本 Doc 目录放 Behaviour_Event_TeleportFlow.mp4，或写 Behaviour_Event_TeleportFlow.url.txt 放一行在线链接。
