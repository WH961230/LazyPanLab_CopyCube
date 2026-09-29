# Behaviour_Auto_Teleportation Teleportation

一句话：行为 - 瞬移位移 只做一件事: 读瞬移按键推 CharacterController 按曲线飞一段 碰墙自己停 不调别的移动行为 停走命令看实体级 MoveAttr.Stopped(谁置 true 都停) 瞬移占领看 MoveAttr.Teleporting(飞完自动松开) 配置来源 Setting/TeleportationSetting 运行时状态只写自己的 TeleportationData

## Setting 字段

| 字段 | 含义 |
|---|---|
| SpeedCurve | 速度曲线：x=进度0~1, y=速度倍率 | -- |

## 使用步骤

1. 在可视化编辑器 Behaviour 标签页找到 Behaviour_Auto_Teleportation，确认配置数据已生成。
2. 按上表在 Setting 资源里为你的 ObjConfig.Sign 加一条。
3. 需要演示视频时，在本 Doc 目录放 Behaviour_Auto_Teleportation.mp4，或写 Behaviour_Auto_Teleportation.url.txt 放一行在线链接。
