# Behaviour_Event_Death 死亡

一句话：只做生命归零后的死亡处理与延迟销毁，不管血量数值。数值参数 Health/MaxHealth/Dead 归实体参数（实体参数）配置初始化，本行为只读写。

原理：监听实体 Data 的 Dead 标记，Kill()置 Dead 触发 Die，Revive()清除 Dead 并复位计时；DeathAction=DestroyEntity 且 DeathDelay>0 时 OnUpdate 倒计时再销毁。

## Setting（DeathSetting）字段

| 字段 | 含义 |
|---|---|
| SourceSign | 发起实体的类型标识，与 ObjConfig.Sign 一致 |
| DeathDelay | 死亡后延迟几秒再执行 DeathAction，0=立即执行 |
| DeathAction | None=仅置 Dead 标记，DestroyEntity=销毁实体，DisableEntity=失活预制体 |

## 前置要求

实体必须在 实体参数配置里配好 Health（Float）、MaxHealth（Float）、Dead（Bool），缺 Dead 会报错。

## 对外接口

- Kill()：处决，置 Dead 标记（外部可用作即死入口）
- Revive()：复活，清除 Dead 标记并复位计时

## 视频

暂无。录好后放同目录 `Behaviour_Event_Death.mp4`，或写 `Behaviour_Event_Death.url.txt` 放在线链接。
