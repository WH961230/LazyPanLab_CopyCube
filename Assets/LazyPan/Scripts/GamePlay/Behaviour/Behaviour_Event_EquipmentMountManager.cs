using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 装备挂载管理器
    /// 积木 负责实体与装备的挂载/拆卸 装备可为物理物体或虚拟物(技能) 不感知装备自身特性
    /// 装备可拆卸: Mount(槽位)/Detach(槽位) 拆卸销毁物理体并回写数据 可重复挂卸
    /// 触发动作: 挂载完成后递增 {槽位}TriggerTick 触发什么不可知 由其他积木按 WatchSign 自行监听
    /// 独立数据: 仅读写自身实体 Data(MountCount/MountedSlots/{槽位}Mounted/{槽位}TriggerTick/{槽位}DetachTick)
    ///          不引用任何其他行为类型
    /// </summary>
    public class Behaviour_Event_EquipmentMountManager : Behaviour {
        private const string settingPath = "Setting/EquipmentMountSetting";
        public const string MOUNTCOUNT_LABEL = "MountCount";
        public const string MOUNTEDSLOTS_LABEL = "MountedSlots";
        public const string SLOT_MOUNTED_SUFFIX = "Mounted";
        public const string SLOT_TRIGGER_SUFFIX = "TriggerTick";
        public const string SLOT_DETACH_SUFFIX = "DetachTick";
        private const char SLOT_SEPARATOR = '|';

        private EquipmentMountData _mountData;
        private EquipmentMountData.EquipmentMountConfig _config;

        private bool isConfigValid;
        private List<MountedEquipment> mounted = new List<MountedEquipment>();

        private IntData _mountCountData;
        private StringData _mountedSlotsData;

        public Behaviour_Event_EquipmentMountManager(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _mountData = AttachBehaviourData<EquipmentMountData>();

            EquipmentMountSetting setting = Loader.LoadAsset<EquipmentMountSetting>(AssetType.ASSET, settingPath);

            if (setting == null) {
                LogUtil.LogErrorFormat("行为:{0} 未找到配置:{1}", behaviourSign, settingPath);
                return;
            }

            if (!setting.TryGet(entity.ObjConfig.Sign, out EquipmentMountSettingData settingData)) {
                return;
            }

            _config = _mountData.Config;
            CopySetting(settingData);

            if (!ValidateSetting()) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 装备挂载配置无效!", BehaviourSign, entity.ObjConfig.Sign);
                return;
            }

            InitRuntimeData();

            if (_config.InitialMount) {
                foreach (EquipmentMountItem item in _config.Mounts) {
                    Mount(item.SlotSign);
                }
            }

            isConfigValid = true;
        }

        public override void DelayedExecute() { }

        private void CopySetting(EquipmentMountSettingData settingData) {
            _config.InitialMount = settingData.InitialMount;
            _config.Mounts.Clear();
            if (settingData.Mounts == null) return;
            HashSet<string> seenSlots = new HashSet<string>();
            foreach (EquipmentMountItem item in settingData.Mounts) {
                if (!BehaviourSigns.Require(item.SlotSign, BehaviourSign, entity.ObjConfig?.Sign, nameof(EquipmentMountItem.SlotSign))) {
                    continue;
                }

                if (!BehaviourSigns.Require(item.EquipmentPrefabSign, BehaviourSign, entity.ObjConfig?.Sign, nameof(EquipmentMountItem.EquipmentPrefabSign))) {
                    continue;
                }

                if (item.EquipmentPrefabSign != BehaviourSigns.Virtual && !BehaviourSigns.Require(item.MountPointLabel, BehaviourSign, entity.ObjConfig?.Sign, nameof(EquipmentMountItem.MountPointLabel))) {
                    continue;
                }

                if (!IsValidSlotSign(item.SlotSign)) {
                    LogUtil.LogErrorFormat("行为:{0} 实体:{1} 槽位标识非法(空或含分隔符:{2}) 已跳过", BehaviourSign, entity.ObjConfig.Sign, item.SlotSign);
                    continue;
                }

                if (!seenSlots.Add(item.SlotSign)) {
                    LogUtil.LogErrorFormat("行为:{0} 实体:{1} 槽位:{2} 重复配置 仅保留第一条", BehaviourSign, entity.ObjConfig.Sign, item.SlotSign);
                    continue;
                }

                _config.Mounts.Add(new EquipmentMountItem() {
                    SlotSign = item.SlotSign,
                    EquipmentPrefabSign = item.EquipmentPrefabSign,
                    MountPointLabel = item.MountPointLabel,
                    OffsetPosition = item.OffsetPosition,
                    OffsetRotation = item.OffsetRotation,
                    OffsetScale = item.OffsetScale == Vector3.zero ? Vector3.one : item.OffsetScale,
                });
            }
        }

        private bool ValidateSetting() {
            if (_config.Mounts == null || _config.Mounts.Count == 0) {
                LogUtil.LogErrorFormat("行为:{0} 实体:{1} 装备槽位列表为空!", BehaviourSign, entity.ObjConfig.Sign);
                return false;
            }

            return true;
        }

        private bool IsValidSlotSign(string slotSign) {
            return !string.IsNullOrEmpty(slotSign) && !slotSign.Contains(SLOT_SEPARATOR.ToString());
        }

        private void InitRuntimeData() {
            Cond.Instance.TryGetData(entity, MOUNTCOUNT_LABEL, out _mountCountData);
            Cond.Instance.TryGetData(entity, MOUNTEDSLOTS_LABEL, out _mountedSlotsData);
            foreach (EquipmentMountItem item in _config.Mounts) {
                Cond.Instance.TryGetData(entity, GetSlotLabel(item.SlotSign, SLOT_MOUNTED_SUFFIX), out BoolData mountedData);
                Cond.Instance.TryGetData(entity, GetSlotLabel(item.SlotSign, SLOT_TRIGGER_SUFFIX), out IntData triggerData);
                Cond.Instance.TryGetData(entity, GetSlotLabel(item.SlotSign, SLOT_DETACH_SUFFIX), out IntData detachData);
                mountedData.Bool = false;
                triggerData.Int = 0;
                detachData.Int = 0;
            }

            RefreshSummary();
        }

        private string GetSlotLabel(string slotSign, string suffix) {
            return string.Concat(slotSign, suffix);
        }

        /// <summary>
        /// 挂载指定槽位 物理装备实例化挂点 虚拟装备仅写数据
        /// 成功后 {槽位}Mounted=true 且 {槽位}TriggerTick+1 触发什么由监听方决定
        /// </summary>
        public bool Mount(string slotSign) {
            if (!isConfigValid) return false;
            EquipmentMountItem item = FindSlot(slotSign);
            if (item == null) {
                LogUtil.LogErrorFormat("行为:{0} 挂载失败 未配置槽位:{1}", BehaviourSign, slotSign);
                return false;
            }

            if (IsMounted(slotSign)) {
                LogUtil.LogErrorFormat("行为:{0} 挂载失败 槽位:{1} 已挂载", BehaviourSign, slotSign);
                return false;
            }

            GameObject go = null;
            if (item.EquipmentPrefabSign != BehaviourSigns.Virtual) {
                if (!MountPhysical(item, out go)) {
                    return false;
                }
            }

            mounted.Add(new MountedEquipment() {
                SlotSign = item.SlotSign,
                EquipmentPrefabSign = item.EquipmentPrefabSign,
                Go = go,
            });

            Cond.Instance.TryGetData(entity, GetSlotLabel(item.SlotSign, SLOT_MOUNTED_SUFFIX), out BoolData mountedData);
            Cond.Instance.TryGetData(entity, GetSlotLabel(item.SlotSign, SLOT_TRIGGER_SUFFIX), out IntData triggerData);
            mountedData.Bool = true;
            triggerData.Int++;
            RefreshSummary();
            LogUtil.LogFormat($"装备:{item.EquipmentPrefabSign} 挂载成功 槽位:{item.SlotSign} 触发计数:{triggerData.Int}");
            return true;
        }

        /// <summary>
        /// 拆卸指定槽位 物理装备销毁 虚拟装备仅回写数据
        /// 成功后 {槽位}Mounted=false 且 {槽位}DetachTick+1
        /// </summary>
        public bool Detach(string slotSign) {
            if (!isConfigValid) return false;
            for (int i = mounted.Count - 1; i >= 0; i--) {
                if (mounted[i].SlotSign != slotSign) continue;
                MountedEquipment entry = mounted[i];
                if (entry.Go != null) {
                    Object.Destroy(entry.Go);
                }

                mounted.RemoveAt(i);
                Cond.Instance.TryGetData(entity, GetSlotLabel(slotSign, SLOT_MOUNTED_SUFFIX), out BoolData mountedData);
                Cond.Instance.TryGetData(entity, GetSlotLabel(slotSign, SLOT_DETACH_SUFFIX), out IntData detachData);
                mountedData.Bool = false;
                detachData.Int++;
                RefreshSummary();
                LogUtil.LogFormat($"装备:{entry.EquipmentPrefabSign} 拆卸成功 槽位:{slotSign} 拆卸计数:{detachData.Int}");
                return true;
            }

            LogUtil.LogErrorFormat("行为:{0} 拆卸失败 槽位:{1} 未挂载", BehaviourSign, slotSign);
            return false;
        }

        /// <summary>槽位是否已挂载。</summary>
        public bool IsMounted(string slotSign) {
            for (int i = 0; i < mounted.Count; i++) {
                if (mounted[i].SlotSign == slotSign) return true;
            }

            return false;
        }

        /// <summary>按槽位取已挂载的物理装备实例 虚拟装备返回 false。</summary>
        public bool TryGetMounted(string slotSign, out GameObject go) {
            for (int i = 0; i < mounted.Count; i++) {
                if (mounted[i].SlotSign == slotSign && mounted[i].Go != null) {
                    go = mounted[i].Go;
                    return true;
                }
            }

            go = null;
            return false;
        }

        /// <summary>按槽位取配置的装备标识 物理装备为预制体路径 虚拟装备为配置原值。</summary>
        public bool TryGetEquipmentSign(string slotSign, out string equipmentSign) {
            EquipmentMountItem item = FindSlot(slotSign);
            if (item != null) {
                equipmentSign = item.EquipmentPrefabSign;
                return true;
            }

            equipmentSign = null;
            return false;
        }

        private EquipmentMountItem FindSlot(string slotSign) {
            foreach (EquipmentMountItem item in _config.Mounts) {
                if (item.SlotSign == slotSign) return item;
            }

            return null;
        }

        private bool MountPhysical(EquipmentMountItem item, out GameObject go) {
            go = null;
            GameObject prefab = Loader.LoadAsset<GameObject>(AssetType.PREFAB, item.EquipmentPrefabSign);
            if (prefab == null) {
                LogUtil.LogErrorFormat("行为:{0} 挂载失败 未找到装备预制体:{1} 请在 Bundles/Prefabs 下创建", BehaviourSign, item.EquipmentPrefabSign);
                return false;
            }

            Transform parent = ResolveMountPoint(item.MountPointLabel);
            go = Loader.LoadGo(prefab.name, item.EquipmentPrefabSign, parent, true);
            go.transform.localPosition = item.OffsetPosition;
            go.transform.localEulerAngles = item.OffsetRotation;
            go.transform.localScale = item.OffsetScale == Vector3.zero ? Vector3.one : item.OffsetScale;
            return true;
        }

        private Transform ResolveMountPoint(string mountPointLabel) {
            return BehaviourSigns.ResolveMountPoint(entity, BehaviourSign, mountPointLabel);
        }

        private void RefreshSummary() {
            _mountCountData.Int = mounted.Count;
            List<string> slots = new List<string>();
            foreach (MountedEquipment entry in mounted) {
                slots.Add(entry.SlotSign);
            }

            _mountedSlotsData.String = string.Join(SLOT_SEPARATOR.ToString(), slots.ToArray());
        }

        public override void Clear() {
            for (int i = mounted.Count - 1; i >= 0; i--) {
                if (mounted[i].Go != null) {
                    Object.Destroy(mounted[i].Go);
                }
            }

            mounted.Clear();
            if (_mountCountData != null) _mountCountData.Int = 0;
            if (_mountedSlotsData != null) _mountedSlotsData.String = string.Empty;
            DetachBehaviourData<EquipmentMountData>();
            base.Clear();
        }

        private class MountedEquipment {
            public string SlotSign;
            public string EquipmentPrefabSign;
            public GameObject Go;
        }
    }
}