# Behaviour_Event_DelayGenerateEntity 延时实体生成器

一句话：只管产怪，不认识波次。写 LivingCount 供别人读。

两种节奏：
- 无触发档案满足：按 GenerateType/IntervalTime/GenerateEntitySign 定时产。
- 有触发档案：按 Profiles 顺序匹配首个满足条件的档案产，支持监听任意 Int/Float/Bool 标签。

## Setting（DelayGenerateEntitySetting）字段

| 字段 | 含义 |
|---|---|
| SourceSign | 发起生成的实体类型，与 ObjConfig.Sign 一致 |
| GenerateType | Once=只产一次，Loop=循环产（无触发时的默认行为） |
| IntervalTime | 无触发时的默认产间隔（秒） |
| GenerateEntitySign | 无触发时要产的实体 Sign |
| Profiles | 触发档案，按序匹配首个满足条件的档案 |

## Profile 单条字段

| 字段 | 含义 |
|---|---|
| WatchSign | 监听的 Data 标签名，如 WaveIndex，为空则无条件命中 |
| WatchEntitySign | 数据源实体 Sign，留空读自己（波次在别的实体上时填波次实体的 Sign） |
| Compare / WatchValue | 比较方式与目标值；监听 WaveIndex 时默认从 1 起 |
| GenerateEntitySign | 本档案要产的实体 Sign |
| Count | 本档案产几只，0 则无限循环产直到被新档案覆盖 |
| Interval | 档案内产间隔（秒） |

触发是上升沿：条件从不满足变满足才触发一次，条件持续满足不会无限重复；Count>0 的档案消费完后锁定到条件回落才解锁。

## 对外读写

- 写：自身 LivingCount（IntData，现存活数），供波次管理器等按需读取
- 读：Profiles 指定的 WatchEntity + WatchSign

## 使用步骤

1. 被产的实体要有 ObjConfig + 初始点（SetUpLocationInformationSign）。
2. 无波次需求：GenerateType+IntervalTime+GenerateEntitySign 三件套即可。
3. 配合波次：WatchSign=WaveIndex、WatchEntitySign=波次实体 Sign、WatchValue=第几波。

## 视频

暂无。录好后放同目录 `Behaviour_Event_DelayGenerateEntity.mp4`，或写 `.url.txt` 放在线链接。
