# Behaviour_Auto_EntityTriggerController EntityTriggerController

一句话：实体触发控制器 — 源实体触发器命中目标实体时 按进入/停留/离开/范围外 四个相位给自己或其他实体增减参数 不读业务。典型: 玩家进塔范围 Energy 每秒涨 离开每秒降。

## Setting 字段

| 字段 | 含义 |
|---|---|
| SourceSign | 触发源实体类型 SourceSign | 持有触发器碰撞体的实体，必须与 ObjConfig.Sign 一致，如 Obj_Tower_SceneB_Tower |
| CompTriggerSign | 组件触发器标识 Root=实体根 | 实体上带触发碰撞体的 Comp 标签，如 Trigger。Root=用实体自身根 Comp。不允许为空 |
| TriggerEntitySign | 触发者实体标识 Any=任意实体 | 允许触发此规则的实体 Sign，如 Obj_Player_SceneB_Player。Any=任意实体进入都算。不允许为空 |
| TargetEntitySign | 被修改实体 必填 Self=自己 Triggerer=触发者 | 要增减参数的目标实体 Sign，Self=触发源实体自己(塔)。Triggerer=带起本次规则的那只实体(谁碰撞就改谁)。填对方 Sign 即改其他实体参数。不允许为空 |
| ParamSign | 参数标签 必填 | 目标实体上的 Data 标签名，如 Energy。不允许为空，不存在时自动创建 |
| ValueType | 参数类型 | 参数类型，决定读写哪一种 Data |
| Modify | 修改方式 | Set=直接赋值 Add=累加增量 AddPerSecond=按 deltaTime 累加(仅停留/范围外每帧相位有意义) |
| BoolValue | 布尔值 | ValueType=Bool 时的值 |
| IntValue | 整数值 | ValueType=Int 时的值或增量 |
| FloatValue | 浮点值 | ValueType=Float 时的值或增量，AddPerSecond 填每秒速率(正增负减)，如 1 或 -1 |
| StringValue | 字符串值 | ValueType=String 时的值 |
| Vector3Value | 向量值 | ValueType=Vector3 时的值或增量 |

## 使用步骤

1. 在可视化编辑器 Behaviour 标签页找到 Behaviour_Auto_EntityTriggerController，确认配置数据已生成。
2. 按上表在 Setting 资源里为你的 ObjConfig.Sign 加一条。
3. 需要演示视频时，在本 Doc 目录放 Behaviour_Auto_EntityTriggerController.mp4，或写 Behaviour_Auto_EntityTriggerController.url.txt 放一行在线链接。
