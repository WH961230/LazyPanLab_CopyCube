# Behaviour_Auto_ContactDamage ContactDamage

一句话：接触伤害配置占位 参数全在自己 Data 里 这里只做图节点与 Setting 的挂钩

## Setting 字段

| 字段 | 含义 |
|---|---|
| SourceSign | 实体 SourceSign | 实体 Sign，与 ObjConfig.Sign 一致 |
| TargetType | 目标实体类型 空=不伤人 | 打哪类实体，如 Player。Data 里写的优先，这个是保底，两边都空就不伤人 |
| DamageRadius | 伤害半径 | <=0 表示跟 Data 走（比如圆环由体型扩散每帧写 DamageRadius）；Data 缺失才用这个 |
| MaxHits | 命中几次后自己死 | 0=不限；Data 有有效值时以 Data 为准 |
| KnockbackDistance | 命中推人距离 0=不推 | 本次打击自带属性：命中后把对方推出几米，0=只扣血不推。对方没挂击退行为就是纸条没人看，也只扣血 |
| DamageToType | 伤害记在谁头上 空=打谁扣谁 | 比如敌人蹭到塔，血想扣玩家的，就填 Player。空=扣被碰到的那个 |
| TargetType | 目标实体类型 | 打哪类实体，如 Player，空=这条不伤人 |
| DamageRadius | 伤害半径 | -- |
| MaxHits | 命中几次后自己死 0=不限 | -- |
| KnockbackDistance | 命中推人距离 0=不推 | -- |
| DamageToType | 伤害记在谁头上 空=打谁扣谁 | 比如敌人蹭到塔，血想扣玩家的，就填 Player。空=扣被碰到的那个 |

## 使用步骤

1. 在可视化编辑器 Behaviour 标签页找到 Behaviour_Auto_ContactDamage，确认配置数据已生成。
2. 按上表在 Setting 资源里为你的 ObjConfig.Sign 加一条。
3. 需要演示视频时，在本 Doc 目录放 Behaviour_Auto_ContactDamage.mp4，或写 Behaviour_Auto_ContactDamage.url.txt 放一行在线链接。
