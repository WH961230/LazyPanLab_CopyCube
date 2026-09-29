# Behaviour_Auto_TrackingEntityByNavMeshAgent 寻路导航追踪实体

一句话：驱动 NavMeshAgent 去追一个目标实体，读 MovementStop 可暂停。

原理：每帧从 Cond 取 NavMeshAgent，按 TrackingSpeed 写 speed，把目标实体的 Body 位置 SetDestination。TrackingStop 为 true，或同实体 Data 上 MovementStop(Bool)为 true，就停住并清空路径。

## Setting（TrackingEntitySetting）字段

| 字段 | 含义 |
|---|---|
| SourceSign | 发起追踪的实体类型，必须与 ObjConfig.Sign 一致 |
| TrackingSpeed | 追踪速度，直接写到 NavMeshAgent.speed |
| TrackingStop | 为 true 则原地停并清空路径 |
| TargetType | 被追踪的实体类型（ObjConfig.Type 列），如 Player，运行时随机取一个 |

## 对外读写

- 读：自身 MovementStop（BoolData），目标实体 Body（Transform）
- 写：自身 TrackingEntityData.Config（TrackingStop / TrackingSpeed / TargetEntityType）

## 使用步骤

1. 实体配好 NavMeshAgent（Cond 能取到）+ ObjConfig.Sign 在 Setting 里加一条。
2. TargetType 填目标的 Type，被追踪方要有 Body。
3. 想暂停就写 MovementStop=true，或把 TrackingStop 配成 true。

## 视频

暂无。录好后放同目录 `Behaviour_Auto_TrackingEntityByNavMeshAgent.mp4`，或写 `Behaviour_Auto_TrackingEntityByNavMeshAgent.url.txt` 放在线链接。
