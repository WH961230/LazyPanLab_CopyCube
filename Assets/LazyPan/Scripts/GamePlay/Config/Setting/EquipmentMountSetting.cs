using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>装备挂载 — 管理实体与装备的挂载/拆卸 物理装备挂预制体 虚拟装备纯数据。挂载后递增 {槽位}TriggerTick 触发信号 触发什么不可知。</summary>
    [CreateAssetMenu(fileName = "EquipmentMountSetting", menuName = "LazyPan/EquipmentMountSetting")]
    public class EquipmentMountSetting : Setting {
        public List<EquipmentMountSettingData> Datas = new List<EquipmentMountSettingData>();

        public bool TryGet(string sourceSign, out EquipmentMountSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("EquipmentMountSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public class EquipmentMountSettingData {
        [EntitySign]
        [Header("发起挂载的实体类型 SourceSign")]
        [Tooltip("发起挂载的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Player_SceneB_Player")]
        public string SourceSign;

        [Header("初始即挂载")]
        [Tooltip("勾上则行为安装时自动挂载全部配置的装备；不勾则等待外部调用 Mount(槽位) 才挂")]
        public bool InitialMount = true;

        [Header("装备槽位列表")]
        [Tooltip("装备槽位列表，一条=一个槽位。槽位标识同一实体内不可重复")]
        public List<EquipmentMountItem> Mounts = new List<EquipmentMountItem>();
    }

    [Serializable]
    public class EquipmentMountItem {
        [Header("槽位标识")]
        [Tooltip("槽位唯一标识，如 Hand/Back/Skill1。不可含 | 分隔符。运行时产出 Data: {槽位}Mounted / {槽位}TriggerTick / {槽位}DetachTick")]
        public string SlotSign;

        [Header("装备标识 留空为虚拟装备")]
        [Tooltip("物理装备填 Bundles/Prefabs 相对路径(如 Equipment/Sword_01)会实例化挂载；留空=虚拟装备(如技能) 仅写数据不生成物体")]
        public string EquipmentPrefabSign;

        [Header("挂点标签 物理装备用")]
        [Tooltip("挂点标签，实体 Comp 里的 Transform Sign，如 Hand。为空挂到实体根节点。虚拟装备忽略此项")]
        public string MountPointLabel;

        [Header("位置偏移")]
        [Tooltip("挂点局部位置偏移。虚拟装备忽略此项")]
        public Vector3 OffsetPosition;

        [Header("旋转偏移 欧拉角")]
        [Tooltip("挂点局部旋转偏移，欧拉角。虚拟装备忽略此项")]
        public Vector3 OffsetRotation;

        [Header("缩放 默认1,1,1")]
        [Tooltip("挂载缩放，默认 1,1,1。虚拟装备忽略此项")]
        public Vector3 OffsetScale;
    }
}