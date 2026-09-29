# Behaviour_Event_EntityUIBinder EntityUIBinder

一句话：实体挂载界面 — 把 UI 预制体挂到实体节点并按 Data 自动刷新数值。

## Setting 字段

| 字段 | 含义 |
|---|---|
| SourceSign | 发起绑定的实体类型 | 发起绑定的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Enemy_SceneC_Enemy |
| Items | 绑定UI列表 | 绑定UI列表，一个实体可挂多个UI |
| UIPrefabSign | UI预制体标识 Bundles/Prefabs/UI下 | UI预制体标识，Bundles/Prefabs 下相对路径，如 UI/UI_HealthBar |
| AttachLabel | 挂点标签 Root=实体根 如Foot | 挂点标签（实体Comp里的Transform Sign），Root=挂实体根节点，如 UIRoot/Foot/Body。不允许为空 |
| Offset | 挂点局部偏移 | 挂点局部偏移 |
| Billboard | 是否始终面向相机 | 勾上则始终面向相机（Billboard） |
| ComponentSign | 组件标签 预制体Comp里配置的Sign | 组件标签，UI预制体Comp里配置的Sign，如 Slider；无Comp时按子物体名兜底 |
| ComponentType | 组件类型 | 绑定组件类型：Slider=滑条/血条，Text=文本，Image=填充图 |
| Mode | 取值模式 比例=当前/最大 直接=当前值 | Ratio=当前/最大(如 Health/MaxHealth)；Direct=直接取值 |
| DataSign | 数据标签 实体Data里的Sign 如Health | 数据标签，实体Data里的Sign，如 Health |
| Format | 文本格式 F0整数 F1一位小数 空则F0 | 文本格式化，F0=整数/F1=一位小数，空则F0（仅 Text 用） |

## 使用步骤

1. 在可视化编辑器 Behaviour 标签页找到 Behaviour_Event_EntityUIBinder，确认配置数据已生成。
2. 按上表在 Setting 资源里为你的 ObjConfig.Sign 加一条。
3. 需要演示视频时，在本 Doc 目录放 Behaviour_Event_EntityUIBinder.mp4，或写 Behaviour_Event_EntityUIBinder.url.txt 放一行在线链接。
