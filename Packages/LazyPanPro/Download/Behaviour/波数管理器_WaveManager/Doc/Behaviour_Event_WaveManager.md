# Behaviour_Event_WaveManager 波数管理器

一句话：只报第几波、歇几秒、啥时候报下一波，不产怪不感知其他行为。写 WaveIndex/WaveState/WaveRestRemain。

状态机：InitialDelay（首波前延迟）→ Rest（计时到就下一波）/ Waiting（等指定 Data 满足条件再计时）→ Completed（走完，可 Loop 回绕）。未开始时对外报 起始波数-1，保证监听方在第一波正式开始时才收到上升沿。

## Setting（WaveManagerSetting）字段

| 字段 | 含义 |
|---|---|
| SourceSign | 发起波次的实体类型，与 ObjConfig.Sign 一致 |
| StartWaveIndex | 第一波写入 WaveIndex 的数值，默认1（产怪档案按此匹配） |
| InitialDelay | 首波前延迟秒数，0 立即开第一波 |
| Loop | 全部走完后是否回到起始波重来 |
| Waves | 波次列表，按序执行，一条一波 |

## WaveEntry 单条字段

| 字段 | 含义 |
|---|---|
| RestDuration | 本波结束后到下一波的等待秒数，0 立即下一波 |
| AdvanceMode | Interval=计时到就下一波；WaitValue=等指定 Data 满足条件再计时 |
| WaitWatchSign | 等待的 Data 标签名，如 LivingCount，为空则不等待 |
| WaitWatchEntitySign | 数据源实体 Sign，留空读自己；波次与产怪分属两个实体时，填产怪实体 Sign 读它的 LivingCount |
| WaitTargetValue / Compare | 等待的目标值与比较方式，如 LivingCount Equal 0 表示等怪清光 |

## 对外读写

- 写：自身 WaveIndex（Int）、WaveState（String）、WaveRestRemain（Float）
- 读：WaitWatchEntitySign 指定实体的 WaitWatchSign

## 经典搭配

产怪行为配 `WatchSign=WaveIndex、WatchEntitySign=波次实体、WatchValue=N`，波次这边配 `WaitWatchSign=LivingCount、WaitWatchEntitySign=产怪实体、WaitTargetValue=0`，就形成“报波→产怪→清光→下一波”闭环。

## 视频

暂无。录好后放同目录 `Behaviour_Event_WaveManager.mp4`，或写 `.url.txt` 放在线链接。
