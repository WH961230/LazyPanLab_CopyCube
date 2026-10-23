# Behaviour_Event_UIPickOneOfThree UIPickOneOfThree

一句话：肉鸽三选一 — 内容池，只管有啥，不管长啥样。 UI 壳只认"纸条"(标题/描述/图标)，内容层只管奖池和效果包，中间拿纸条传话，互相看不见对方。 效果包复用老积木语义：目标实体+参数+Set/Add，跟 Death 的 OnDeathParams 一个模子，不写新逻辑。 配置来源 Setting/UIPickOneOfThreeSetting，一个实体一条，里面配一个奖池。

## Setting 字段

| 字段 | 含义 |
|---|---|
| SourceSign | 挂这个行为的实体类型 SourceSign | 挂这个行为的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Player_SceneC_Player |
| PanelPrefabSign | 面板预制体标识 Bundles/Prefabs/UI下 | 三选一面板预制体，Bundles/Prefabs 下相对路径，如 UI/UI_PickOneOfThree。实例化到主界面挂点下。预制体契约:根上挂Comp；3个按钮命名Card0/1/2；每按钮下2个TMP命名CardX_Title/CardX_Desc；根不要加Canvas；Comp里Buttons/TextMeshProUGUIs把引用拖进去 |
| MountSign | 挂点标签 Root=主界面根 | 面板挂到主界面哪个Transform下。Root=主界面根节点，其他填主界面Comp里配置的Transform Sign |
| EnableTestAutoOpen | 测试自动开奖开关 | 打开后进场景自动弹一次，验证面板用。正式接升级调用后关掉 |
| Title | 卡标题 如火球+1 | 卡标题，贴到面板按钮上，UI 不认字只管贴 |
| Description | 卡描述 如伤害+50 | 卡描述，贴到面板上给玩家看 |
| IconName | 卡图标名 可空 | 卡图标名，预留，当前版本可空，以后贴图用了再填 |
| ParamSign | 参数标签 必填 | 目标实体上的 Data 标签名，如 MaxHealth / MovementSpeed。不允许为空 |
| ValueType | 参数类型 | 参数类型，决定读写哪一种 Data |
| Modify | 修改方式 | Set=直接赋值，Add=在原值上累加增量(只对 Int/Float/Vector3 有意义) |

## 使用步骤

1. 在可视化编辑器 Behaviour 标签页找到 Behaviour_Event_UIPickOneOfThree，确认配置数据已生成。
2. 按上表在 Setting 资源里为你的 ObjConfig.Sign 加一条。
3. 需要演示视频时，在本 Doc 目录放 Behaviour_Event_UIPickOneOfThree.mp4，或写 Behaviour_Event_UIPickOneOfThree.url.txt 放一行在线链接。
