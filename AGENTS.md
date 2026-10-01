# AGENTS.md — Behaviour黑盒 AI协作规范（与老行为兼容）

> 开工先读本文件。中文简体，大白话，开头报规则序号001。

## 0. 不确定的事不许擅自写

1. 没把握的点，先停下来问我，贴出选项让我选，不许边猜边写。
2. 通用型的结论，直接写进本规范，后面所有 Behaviour 共用。
3. 单次特例（某一个怪、某一个塔的数值），只改那一条配置，不写进规范。

## 1. 数据统一规范（全局一致，旧参数配置已干掉）

1. 跨行为传数一律走 `EntityAttrRegistry` 注册表：`SetNumber/TryGetNumber、SetBool/TryGetBool、SetText/TryGetText、SetVector/TryGetVector、Health/Move`。
2. `entity.Data` 里的 `FloatData/IntData/BoolData` 不许跨行为读写，只允许行为内部 `AttachBehaviourData<T>` 存自己的 Config + 私有运行时字段（如 `hasTeleported、_innerRequest`）。
3. 谁生产谁创建：`TryGetNumber` 读不到就是 0，直接 `Set` 即创建，不用预配初始值。
4. `Max+参数名`（如 `MaxEnergy`）由生产者顺手登记，界面画条有分母。
5. `Health` 唯一生产者是死亡行为，其余只消费。

## 2. 黑箱原则（老逻辑不动）

1. Behaviour 对外只露 Graph 节点里的格子，不露代码。用户只填：看谁、看哪个数、多少算够、去哪。
2. 行为之间不连线、不互相调用，只靠注册表传话。
3. 老习惯保留：`SourceSign` 一物一条；数值受 `Min/Max` 钳制；传送 `Once` 只跳一次。

## 3. 节点填写规范

1. 一节点一行为，`SourceSign` 必须等于 `ObjConfig.Sign`，对不上红字报错。
2. 左边填变量，右边能用常量就用常量。如 `Energy >= 100` 优于 `Energy >= MaxEnergy`。
3. 四个词全框架通用：`Self=自己 Any=谁都行 Triggerer=撞过来的人 Root=实体根`，不许造新词。
4. 枚举查代码为准：`DataValueType 0=Bool 1=Int 2=Float 3=String 4=Vector3`；`TeleportCompare 0=大于 1=大于等于 2=等于 3=小于等于 4=小于 5=不等`。
5. 改 `Setting/*.asset` 必须同步改 `Graph/Obj_*_*.asset` 里那份拷贝，否则图一保存就覆盖。

## 4. AI 造新积木规范

1. 三件套齐活：`Setting + Data + Behaviour`，带 `MemoDoc` 一句话说明、`CheckContract` 红黄自检、`Clear` 摘监听。少一样不许进图。
2. 只许读写注册表，不许调别的 Behaviour，不许 `PeekData/GetData` 跨行为读 `Data`。
3. 命名复用老词：`Health/MaxHealth、Energy/MaxEnergy`，新词先登记再用。
4. 一行为一事，`Auto_` 每帧跑，`Event_` 被喊才动，超 200 行就拆。

## 5. 排查口诀

- 数对不上先查注册表有没有 `Set`，再查读取用的 key 是否一致，大小写必须一样。

## 6. 反复打磨 + 自我反省（最终目标：拉下来就能在节点里看效果）

1. 行为做完不是结束，以后还会不断优化。每次改完都反省一遍再收工。
2. 自我反省三问，做完一个行为必须跑一遍：
   - 我刚修的这个坑，别的行为有没有同款？如传送改了读注册表，伤害、击退、死亡是不是还读 `Data`？有就列出来问我要不要一起改。
   - 我这次的改法能不能变成通用规则？能就写进本规范，不能就只留在这一个行为里。
   - 用户拉下来能不能直接看效果？节点格子够不够白话？要不要把常量默认值填好（如传送默认 `>=100`）？
3. 反省完先问再干：把“别的行为也有同款问题，要不要一起改”列成选项让我选，我说改才改，不擅自扩散。
4. 方针一致就顺：大方向是用户快速拉取、节点配完直接看效果。凡是让用户多配一步、多看一眼代码的改法，一律打回。

## 7. 插件使用机制（行为拉取制，公用常驻插件）

1. 每一个行为都是要拉取的：新项目默认没有行为，去 Behaviour 界面的下载安装区按需拉，拉哪个看哪个的效果。
2. 拉一个行为，自动带齐五件套：`Behaviour_*.cs` 进 `Assets/LazyPan/Scripts/GamePlay/Behaviour`，`SettingScript` 进 `Config/Setting`，`Data` 进 `Data`，`Setting/*.asset` 进 `Bundles/Configs/Setting`，`NodeView` 进 `Assets/Editor`，同时在 `BehaviourConfig.csv` 登记中文名并自动生成图节点。
3. 行为私有的东西（Setting/Data/Behaviour/占位配置如 BulletFlySetting、OrbitFollowSetting、RingExpandSetting、本行为的 `*NodeView.cs`）一律放在 `Packages/LazyPanPro/Download/Behaviour/对应行为包/` 里，不许进 Core。
4. 所有公用的东西常驻插件：`EntityAttrRegistry`、`DataLabels`、`Entity、Data、Setting` 基类、`Register` 各注册表、`Tool/Label/Config` 公共脚本全在 `Packages/LazyPanPro/Runtime/Core` 里，图框架（全局总览图、实体图窗口、说明书、总览节点类型与配色）全在 `Packages/LazyPanPro/Editor/EntityGraph` 里，新项目拉取检出后就是完整的，不用再拷。
5. 留守 Assets 的只有两样：`BehaviourGraphNodes.Generated.cs`（20 来个实体图资产按 Assembly-CSharp 序列化引用了里面的节点类，搬家就全变 Missing，且生成器持续往这里写），以及还没装进包的新行为草稿。`Download/Template` 下 5 个空文件夹是模板占位，内容进来前不动。
6. F2 就是全局图：包内 F2 入口先走 `GlobalMapHook` 开图，钩子没赋值（极旧项目）才落回文字版。文字版不许再加功能。
7. 卸载行为：已安装的行旁边有“卸载”按钮，删五件套 + 取消登记 + 摘生成节点类；还有实体在用先弹窗确认。重装即恢复，图上残留节点 Missing 一下不怕。
5. 新加公用方法先问归属：行为之间共用的走 `Runtime/Core`，单个行为私有的跟行为包走。拿不准就列选项问，不擅自决定放哪。
6. 下载包准入：`Download/Behaviour` 下每个行为文件夹必须有 `Behaviour_*.cs`（安装器靠它认门，没它不显示不安装），`SettingScript/Data/Doc` 三件套齐了才能进。
