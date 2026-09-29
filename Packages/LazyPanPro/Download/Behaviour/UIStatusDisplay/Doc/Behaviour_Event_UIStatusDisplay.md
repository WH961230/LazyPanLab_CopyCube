# Behaviour_Event_UIStatusDisplay UIStatusDisplay

一句话：屏幕状态展示 — 把任意实体 Data 刷到屏幕 UI 上。 跟 EntityUIBinder 是两兄弟：EntityUIBinder 是挂头顶血条（世界坐标，跟实体走）， 这个是刷主界面 HUD（屏幕坐标，比如 UI_SceneC 显示玩家血量/等级/经验/波次）。 只读不写，不改任何数值，数值归 ParamValue / StageProgress / Death 管。 配置来源 Setting/UIStatusDisplaySetting，一个实体一条，里面可配多个屏幕 UI 块。

## Setting 字段

| 字段 | 含义 |
|---|---|
| SourceSign | 挂这个行为的实体类型 SourceSign | 挂这个行为的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Player_SceneC_Player。行为挂谁身上，刷新就由谁驱动 |
| Displays | 屏幕UI块列表 | 屏幕UI块列表，一般配1块就够。想同时刷两个界面才配多块 |
| UIName | 屏幕UI名 为空=当前流程主界面 | 屏幕UI名，如 UI_SceneC。留空=自动取当前流程的 GetUI()，一般留空就行。填了就按名字去 UI.Instance.Get 取 |
| UIPrefabSign | HUD预制体标识 为空=直接绑主界面 | HUD预制体标识，Bundles/Prefabs 下相对路径，如 UI/UI_HUD_Status。填了=实例化到主界面挂点下再绑，组件从预制体Comp里拿。留空=老路，直接绑主界面Comp |
| InstanceSign | 实例名 为空=用预制体名 | 实例化出来的HUD物体名，方便层级里找。留空=用预制体文件名。仅 UIPrefabSign 填了才用 |
| ComponentSign | 组件标签 屏幕UI的Comp里配置的Sign | 组件标签，屏幕UI的Comp里配置的Sign，如 Slider / HealthText。无Comp时按子物体名兜底 |
| ComponentType | 组件类型 | 绑定组件类型：Slider=滑条/血条，Text=文本，Image=填充图 |
| Mode | 取值模式 比例=当前/最大 直接=当前值 | Ratio=当前/最大(如 Health/MaxHealth)；Direct=直接取值 |
| DataSign | 数据标签 如Health | 数据标签，取数实体Data里的Sign，如 Health / Level / WaveIndex |
| Format | 文本格式 F0整数 F1一位小数 空则F0 | 文本格式化，F0=整数/F1=一位小数，空则F0（仅 Text 用） |

## 使用步骤

1. 在可视化编辑器 Behaviour 标签页找到 Behaviour_Event_UIStatusDisplay，确认配置数据已生成。
2. 按上表在 Setting 资源里为你的 ObjConfig.Sign 加一条。
3. 需要演示视频时，在本 Doc 目录放 Behaviour_Event_UIStatusDisplay.mp4，或写 Behaviour_Event_UIStatusDisplay.url.txt 放一行在线链接。
