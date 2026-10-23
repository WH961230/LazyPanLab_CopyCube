# Behaviour_Event_EquipmentMountManager EquipmentMountManager

一句话：装备挂载 — 管理实体与装备的挂载/拆卸 物理装备挂预制体 虚拟装备纯数据。挂载后递增 {槽位}TriggerTick 触发信号 触发什么不可知。

## Setting 字段

| 字段 | 含义 |
|---|---|
| SourceSign | 发起挂载的实体类型 SourceSign | 发起挂载的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Player_SceneB_Player |
| SlotSign | 槽位标识 | 槽位唯一标识，如 Hand/Back/Skill1。不可含 | 分隔符。运行时产出 Data: {槽位}Mounted / {槽位}TriggerTick / {槽位}DetachTick |
| EquipmentPrefabSign | 装备标识 Virtual=虚拟装备 | 物理装备填 Bundles/Prefabs 相对路径(如 Equipment/Sword_01)会实例化挂载；Virtual=虚拟装备(如技能) 仅写数据不生成物体。不允许为空 |
| MountPointLabel | 挂点标签 物理装备用 Root=实体根 | 挂点标签，实体 Comp 里的 Transform Sign，如 Hand。Root=挂到实体根节点。不允许为空。虚拟装备忽略此项 |
| OffsetPosition | 位置偏移 | 挂点局部位置偏移。虚拟装备忽略此项 |
| OffsetRotation | 旋转偏移 欧拉角 | 挂点局部旋转偏移，欧拉角。虚拟装备忽略此项 |
| OffsetScale | 缩放 默认1,1,1 | 挂载缩放，默认 1,1,1。虚拟装备忽略此项 |

## 使用步骤

1. 在可视化编辑器 Behaviour 标签页找到 Behaviour_Event_EquipmentMountManager，确认配置数据已生成。
2. 按上表在 Setting 资源里为你的 ObjConfig.Sign 加一条。
3. 需要演示视频时，在本 Doc 目录放 Behaviour_Event_EquipmentMountManager.mp4，或写 Behaviour_Event_EquipmentMountManager.url.txt 放一行在线链接。
