# Behaviour_Auto_Knockback Knockback

一句话：击退 — 被动行为，只管飞，不管谁喊的。 别人（比如接触伤害）调 Behaviour_Auto_Knockback.Request 写方向和力度，行为下一帧自动开飞。 飞全程推 CharacterController，按衰减曲线减速，时间到自动松开 MoveAttr.KnockingBack。 配置来源 Setting/KnockbackSetting，运行时状态只写自己的 KnockbackData。

## Setting 字段

| 字段 | 含义 |
|---|---|
| SourceSign | 会被击退的实体类型 SourceSign | 必须与 ObjConfig.Sign 一致，如 Obj_Player_SceneC_Player。对不上这条配置就不会被这个实体用到 |
| DecayCurve | 减速曲线：x=进度0~1, y=速度倍率 | 前快后慢就拉成前高后低，空=匀速飞完 |

## 使用步骤

1. 在可视化编辑器 Behaviour 标签页找到 Behaviour_Auto_Knockback，确认配置数据已生成。
2. 按上表在 Setting 资源里为你的 ObjConfig.Sign 加一条。
3. 需要演示视频时，在本 Doc 目录放 Behaviour_Auto_Knockback.mp4，或写 Behaviour_Auto_Knockback.url.txt 放一行在线链接。
