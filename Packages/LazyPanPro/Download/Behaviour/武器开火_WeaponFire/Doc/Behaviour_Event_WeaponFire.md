# Behaviour_Event_WeaponFire WeaponFire

一句话：武器菜谱 — 只管有哪些枪、每把枪什么参数，不认识谁在开火。 一条=一个持有者的军火库，里面多把枪按 WeaponID 区分，当前用哪把由持有者 Data 的 CurrentWeapon 字符串决定。 开火行为只读菜谱+读名单+掐表，不认识枪名，加枪只加行。 配置来源 Setting/WeaponSetting，一个持有者一条。

## Setting 字段

| 字段 | 含义 |
|---|---|
| SourceSign | 挂枪的持有者 SourceSign | 挂开火行为的持有者，必须与 ObjConfig.Sign 一致，如 Obj_Tower_SceneC_Tower |
| DefaultWeaponID | 默认武器ID | 持有者 Data 里没有 CurrentWeapon 时用的枪，对应下表某行的 WeaponID，如 smg01 |
| WeaponID | 武器ID 全局唯一 如smg01 | 武器ID，持有者 Data 的 CurrentWeapon 填它就换这把枪 |
| WeaponName | 武器名 如冲锋枪 | 武器名，只做显示，不参与逻辑 |
| Kind | 武器类型 直射=点射 环绕=围着转 范围=生范围体 | Direct=直射(冲锋枪/狙击枪/霰弹枪)，Orbit=环绕(圆环/环绕球/散射卫星)，Area=范围(圆环扩散/手雷/激光)。切类型下面参数跟着换 |
| SpawnSign | 生成实体 直射/范围填 环绕不填 | 触发时生成的实体 Sign，如子弹 Obj_Bullet_SceneC_Bullet、圆环 Obj_Ring_SceneC_Ring。生成物自己管表现，触发器不管。环绕类型不生成，不用填 |
| SpreadAngle | 散布角总度数 0=无散布 | 多个生成物扇形分开的总角度，如霰弹5发30度。单个填0 |
| UseFireCondition | 开火条件开关 不勾=一直开火 | 勾上才看下面的条件，不勾就是老样子一直打 |
| ConditionValue | 和多少比 如50 | 右边常量，和左边比大小 |
| ParamSign | 参数标签 必填 | 写进生成物 Data 的标签名，如 Damage / BulletSpeed / MaxRadius。必须与生成物行为认的词一致，写错读不到 |
| ValueType | 参数类型 | 参数类型，决定写哪一种 Data |

## 使用步骤

1. 在可视化编辑器 Behaviour 标签页找到 Behaviour_Event_WeaponFire，确认配置数据已生成。
2. 按上表在 Setting 资源里为你的 ObjConfig.Sign 加一条。
3. 需要演示视频时，在本 Doc 目录放 Behaviour_Event_WeaponFire.mp4，或写 Behaviour_Event_WeaponFire.url.txt 放一行在线链接。
